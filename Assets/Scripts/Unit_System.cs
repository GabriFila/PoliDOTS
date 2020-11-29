using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Experimental.AI;
using System.Collections.Generic;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.AI;
using System.Diagnostics;
using UnityEngine;

public class Unit_System : SystemBase
{
    private NavMeshQuery query;
    private float3 extents;
    private Dictionary<string, float3[]> allPaths;
    private List<Entity> routedEntities;
    private List<NativeArray<int>> statusOutputs;
    private List<NativeArray<float3>> results;
    private List<NavMeshQuery> queries;
    private NavMeshWorld navMeshWorld;
    private List<JobHandle> jobHandles;
    private List<string> keys;

    private int totCycles;
    private float totTime;
    private float previousTime;
    private int FPS;
    private int totFPS;


    BeginInitializationEntityCommandBufferSystem bi_ECB;

    //----------- Collision Avoidance initialization code start -----------------
    public static NativeMultiHashMap<int, float3> cellVsEntityPositions;
    public static int totalCollisions;
    //----------- Collision Avoidance initialization code end -------------

    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        extents = new float3(50, 50, 50);
        allPaths = new Dictionary<string, float3[]>();
        statusOutputs = new List<NativeArray<int>>();
        results = new List<NativeArray<float3>>();
        routedEntities = new List<Entity>();
        queries = new List<NavMeshQuery>();
        jobHandles = new List<JobHandle>();
        keys = new List<string>();

        totFPS = 0;
        totCycles = 0;
        totTime = 0;
        previousTime = 0;

        for (int n = 0; n <= 1000; n++)
        {
            NativeArray<float3> result = new NativeArray<float3>(1024, Allocator.Persistent);
            NativeArray<int> statusOutput = new NativeArray<int>(3, Allocator.Persistent);
            statusOutputs.Add(statusOutput);
            results.Add(result);
            keys.Add("");
        }
        navMeshWorld = NavMeshWorld.GetDefaultWorld();

        //----------- Collision Avoidance OnCreate code start -----------------
        totalCollisions = 0;
        cellVsEntityPositions = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
        //----------- Collision Avoidance OnCreate code end -------------
    }

    //----------- Collision Avoidance function definition code start -----------------
    public static int GetUniqueKeyForPosition(float3 position, int cellSize)
    {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.z / cellSize)));
    }
    //----------- Collision Avoidance function definition code end -------------

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        totCycles++;

        FPS = (int)(1f / deltaTime);
        totFPS += FPS;
        UnityEngine.Debug.Log(totFPS / totCycles);
       

        var ecb = bi_ECB.CreateCommandBuffer();
        int i = 0, counter = 0;

        Entities.
            WithBurst(synchronousCompilation: true).
            WithStructuralChanges().
            ForEach((Entity e, ref Unit_Component uc, ref DynamicBuffer<Unit_Buffer> ub) =>
            {
                if (i <= UnitManager.instance.maxEntitiesRoutedPerFrame)
                {
                    string key = uc.fromLocation.x + "_" + uc.fromLocation.z + "_" + uc.toLocation.x + "_" + uc.toLocation.z;
                    
                    //Cached path
                    if (UnitManager.instance.useCache && allPaths.ContainsKey(key) && (!uc.routed || ub.Length == 0))
                    {
                        allPaths.TryGetValue(key, out float3[] cachedPath);
                        for (int h = 0; h < cachedPath.Length; h++)
                        {
                            ub.Add(new Unit_Buffer { wayPoints = cachedPath[h] });
                        }
                        uc.routed = true;
                        uc.usingCachedPath = true;
                        EntityManager.AddComponent<Unit_Routed>(e);
                        return;
                    }
                    //Job
                    else if (!uc.routed || ub.Length == 0)
                    {
                        keys[counter] = key;

                        NavMeshQuery currentQuery = new NavMeshQuery(navMeshWorld, Allocator.Persistent, UnitManager.instance.maxPathNodePoolSize);
                        SinglePathFindingJob spfj = new SinglePathFindingJob()
                        {
                            query = currentQuery,
                            nml_FromLocation = uc.nml_FromLocation,
                            nml_ToLocation = uc.nml_ToLocation,
                            fromLocation = uc.fromLocation,
                            toLocation = uc.toLocation,
                            extents = extents,
                            maxIteration = UnitManager.instance.maxIterations,
                            result = results[counter],
                            statusOutput = statusOutputs[counter],
                            maxPathSize = UnitManager.instance.maxPathSize,
                            ub = ub
                        };
                        routedEntities.Add(e);
                        queries.Add(currentQuery);
                        jobHandles.Add(spfj.Schedule());
                        counter++;
                    }
                    i++;
                }
                else
                {
                    return;
                }
            }).Run();

        //Waiting for the completion of jobs
        int n = 0;
        NativeArray<JobHandle> jhs = new NativeArray<JobHandle>(jobHandles.Count, Allocator.Temp);
        foreach (JobHandle jh in jobHandles)
        {
            jhs[n] = jh;
            n++;
        }
        JobHandle.CompleteAll(jhs);
        jhs.Dispose();

        int j = 0;
        foreach (JobHandle jh in jobHandles)
        {
            if (statusOutputs[j][0] == 1)
            {
                if (UnitManager.instance.useCache && !allPaths.ContainsKey(keys[j]))
                {
                    float3[] wayPoints = new float3[statusOutputs[j][2]];
                    for (int k = 0; k < statusOutputs[j][2]; k++)
                    {
                        wayPoints[k] = results[j][k];
                    }
                    if (wayPoints.Length > 0)
                    {
                        allPaths.Add(keys[j], wayPoints);
                    }
                }

                Unit_Component uc = EntityManager.GetComponentData<Unit_Component>(routedEntities[j]);
                uc.routed = true;
                EntityManager.SetComponentData<Unit_Component>(routedEntities[j], uc);
                EntityManager.AddComponent<Unit_Routed>(routedEntities[j]);
            }
            queries[j].Dispose();
            j++;
        }
        routedEntities.Clear();
        jobHandles.Clear();
        queries.Clear();

        //----------- Collision Avoidance Code -----------------

        EntityQuery eq = GetEntityQuery(typeof(Unit_Component));

        cellVsEntityPositions.Clear();
        if (eq.CalculateEntityCount() > cellVsEntityPositions.Capacity)
        {
            cellVsEntityPositions.Capacity = eq.CalculateEntityCount();
        }

        NativeMultiHashMap<int, float3>.ParallelWriter cellVsEntityPositionsParallel = cellVsEntityPositions.AsParallelWriter();
        Entities
            .WithBurst(synchronousCompilation: true)
            .ForEach((ref Unit_Component uc, ref Translation trans) =>
            {
                cellVsEntityPositionsParallel.Add(GetUniqueKeyForPosition(trans.Value, 15), trans.Value);
            }).ScheduleParallel();


        NativeMultiHashMap<int, float3> cellVsEntityPositionsForJob = cellVsEntityPositions;
        Entities
            .WithBurst(synchronousCompilation: true)
            .WithReadOnly(cellVsEntityPositionsForJob)
            .ForEach((ref Unit_Component uc, ref Translation trans) =>
            {
                int key = GetUniqueKeyForPosition(trans.Value, 15);
                NativeMultiHashMapIterator<int> nmhKeyIterator;
                float3 currentLocationToCheck;
                float currentDistance = 0.3f;
                int total = 0;
                uc.avoidanceDirection = float3.zero;
                if (cellVsEntityPositionsForJob.TryGetFirstValue(key, out currentLocationToCheck, out nmhKeyIterator))
                {
                    do
                    {
                        if (!trans.Value.Equals(currentLocationToCheck))
                        {
                            if (currentDistance > math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck)))
                            {
                                currentDistance = math.sqrt(math.lengthsq(trans.Value - currentLocationToCheck));
                                float3 distanceFromTo = trans.Value - currentLocationToCheck;
                                uc.avoidanceDirection = math.normalize(distanceFromTo / currentDistance);
                                total++;
                            }
                        }
                    } while (cellVsEntityPositionsForJob.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
                    if (total > 0)
                    {
                        uc.avoidanceDirection = uc.avoidanceDirection / total;
                    }
                }
            }).ScheduleParallel();

        //----------- Collision Avoidance Code -----------------


        //Movement
        Entities
           //.WithStructuralChanges()
           .WithoutBurst()
           .WithAll<Unit_Routed>().ForEach((Entity e, int entityInQueryIndex, ref Unit_Component uc, ref DynamicBuffer<Unit_Buffer> ub, ref DynamicBuffer<Schedule_Buffer> sb, ref Translation trans) =>
           {
               UnityEngine.AI.NavMeshHit outResult;
               Translation newTrans = trans;

               if (ub.Length > 0 && uc.routed)
               {
                   uc.waypointDirection = math.normalize(ub[uc.currentBufferIndex].wayPoints - trans.Value);
                   uc.avoidanceDirection.y = 0;
                   uc.waypointDirection = uc.waypointDirection + uc.avoidanceDirection;
                   uc.waypointDirection.y = 0;

                   newTrans.Value.y = 1.083333f;

                   if (!UnityEngine.AI.NavMesh.SamplePosition(newTrans.Value, out outResult, 0.01f, NavMesh.AllAreas))
                   {
                       UnityEngine.AI.NavMesh.SamplePosition(newTrans.Value, out outResult, 1f, NavMesh.AllAreas);
                       trans.Value.x = outResult.position.x;
                       trans.Value.z = outResult.position.z;
                   }

                   newTrans.Value = trans.Value + uc.waypointDirection * uc.speed * deltaTime;
                   newTrans.Value.y = 1.083333f;

                   if (!UnityEngine.AI.NavMesh.SamplePosition(newTrans.Value, out outResult, 0.00001f, NavMesh.AllAreas))
                   {
                       uc.waypointDirection -= uc.avoidanceDirection;
                   }

                   trans.Value += uc.waypointDirection * uc.speed * deltaTime;
                   float3 finalWayPoint = uc.toLocation;
                   finalWayPoint.y = ub[uc.currentBufferIndex].wayPoints.y;

                   if (math.distance(trans.Value, ub[uc.currentBufferIndex].wayPoints) <= uc.minDistanceReached)
                   {

                       if (uc.currentBufferIndex < ub.Length - 1 && !ub[uc.currentBufferIndex].wayPoints.Equals(finalWayPoint))
                       {
                           uc.currentBufferIndex = uc.currentBufferIndex + 1;
                       }

                       else if (uc.count < sb.Length - 1)
                       {
                           uc.count += 1;
                           uc.fromLocation = uc.toLocation;
                           uc.toLocation = sb[uc.count].destination;
                           uc.routed = false;
                           uc.usingCachedPath = false;
                           uc.currentBufferIndex = 0;
                           ub.Clear();
                           ecb.RemoveComponent<Unit_Routed>(e);
                       }
                       else if (uc.count == sb.Length - 1)
                       {
                           ecb.DestroyEntity(e);
                       }
                   }

               }
           }).Run();
    }

    protected override void OnDestroy()
    {
        cellVsEntityPositions.Dispose();

        for (int n = 0; n <= UnitManager.instance.maxEntitiesRoutedPerFrame; n++)
        {
            statusOutputs[n].Dispose();
            results[n].Dispose();
        }
    }

    [BurstCompile]
    private struct SinglePathFindingJob : IJob
    {
        PathQueryStatus status;
        PathQueryStatus returningStatus;
        public NavMeshQuery query;
        public NavMeshLocation nml_FromLocation;
        public NavMeshLocation nml_ToLocation;
        public float3 fromLocation;
        public float3 toLocation;
        public float3 extents;
        public int maxIteration;
        public DynamicBuffer<Unit_Buffer> ub;
        public NativeArray<float3> result;
        public NativeArray<int> statusOutput;
        public int maxPathSize;

        public void Execute()
        {
            nml_FromLocation = query.MapLocation(fromLocation, extents, 0);
            nml_ToLocation = query.MapLocation(toLocation, extents, 0);
            if (query.IsValid(nml_FromLocation) && query.IsValid(nml_ToLocation))
            {
                status = query.BeginFindPath(nml_FromLocation, nml_ToLocation, -1);
                if (status == PathQueryStatus.InProgress)
                {
                    status = query.UpdateFindPath(maxIteration, out int iterationPerformed);
                }
                if (status == PathQueryStatus.Success)
                {
                    status = query.EndFindPath(out int polygonSize);
                    NativeArray<NavMeshLocation> res = new NativeArray<NavMeshLocation>(polygonSize, Allocator.Temp);
                    NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                    NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                    NativeArray<PolygonId> polys = new NativeArray<PolygonId>(polygonSize, Allocator.Temp);
                    int straightPathCount = 0;
                    query.GetPathResult(polys);
                    returningStatus = PathUtils.FindStraightPath(
                        query,
                        fromLocation,
                        toLocation,
                        polys,
                        polygonSize,
                        ref res,
                        ref straightPathFlag,
                        ref vertexSide,
                        ref straightPathCount,
                        maxPathSize
                        );
                    if (returningStatus == PathQueryStatus.Success)
                    {
                        statusOutput[0] = 1;
                        statusOutput[1] = 1;
                        statusOutput[2] = straightPathCount;

                        if (straightPathCount == 0)
                            UnityEngine.Debug.Log("Something wrong");

                        for (int i = 0; i < straightPathCount; i++)
                        {
                            result[i] = (float3)res[i].position + new float3(0, 0.75f, 0); // elevated point
                            ub.Add(new Unit_Buffer { wayPoints = result[i] });
                        }
                    }

                    res.Dispose();
                    straightPathFlag.Dispose();
                    polys.Dispose();
                    vertexSide.Dispose();
                }
            }
        }
    }
}

public struct Unit_Routed : IComponentData { }

public struct Unit_Cached : IComponentData { }