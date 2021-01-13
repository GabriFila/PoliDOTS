using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
public class UnitSystem : SystemBase
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

    BeginInitializationEntityCommandBufferSystem bi_ECB;

    //---------------------- Collision Avoidance ---------------------------

    //public static NativeMultiHashMap<int, CovidPos> cellVsEntityPositions;

    //---------------------- Collision Avoidance ---------------------------

    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        extents = new float3(100, 100, 100);
        allPaths = new Dictionary<string, float3[]>();
        statusOutputs = new List<NativeArray<int>>();
        results = new List<NativeArray<float3>>();
        routedEntities = new List<Entity>();
        queries = new List<NavMeshQuery>();
        jobHandles = new List<JobHandle>();
        keys = new List<string>();


        for (int n = 0; n <= 4000; n++) //limit number equals to Max Entities routed per frame of UnitManager game object
        {
            NativeArray<float3> result = new NativeArray<float3>(1024, Allocator.Persistent);
            NativeArray<int> statusOutput = new NativeArray<int>(2, Allocator.Persistent);
            statusOutputs.Add(statusOutput);
            results.Add(result);
            keys.Add("");
        }
        navMeshWorld = NavMeshWorld.GetDefaultWorld();

    }

    //---------------------- Collision Avoidance ---------------------------

    private static int GetUniqueKeyForPosition(float3 position, int cellSize)
    {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.z / cellSize)));
    }
    //---------------------- Collision Avoidance ---------------------------

    protected override void OnUpdate()
    {
        int totCurrentStudents = GetEntityQuery(typeof(UnitComponent)).CalculateEntityCount();
        UnitManager.Instance.CurrentNumberOfStudents = totCurrentStudents;
        UnitManager.Instance.CurrentNumberOfCovid = GetEntityQuery(typeof(UnitComponent), typeof(HasCovidComponent)).CalculateEntityCount();
        NativeMultiHashMap<int, CovidPos> cellVsEntityPositions = new NativeMultiHashMap<int, CovidPos>(totCurrentStudents, Allocator.Temp);

        int probabilityOfInfectionWithMaskWait = (int)(UnitManager.Instance.ProbabilityOfInfectionWithMaskWait * 100);
        int probabilityOfInfectionWait = (int)(UnitManager.Instance.ProbabilityOfInfectionWait * 100);
        float infectionDistance = UnitManager.Instance.InfectionDistance;

        float deltaTime = Time.DeltaTime;

        var ecb = bi_ECB.CreateCommandBuffer();
        var ecbParallel = bi_ECB.CreateCommandBuffer().AsParallelWriter();
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;
        int i = 0, counter = 0;

        Entities
            //WithNone<WaitComponent>()
            .WithNone<ToDestroyComponent>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref UnitComponent uc, ref DynamicBuffer<UnitBuffer> ub, ref Translation trans, ref PersonComponent pc) =>
            {

                if (i <= UnitManager.Instance.MaxEntitiesRoutedPerFrame)
                {
                    string key = uc.fromLocation.x + "_" + uc.fromLocation.z + "_" + uc.toLocation.x + "_" + uc.toLocation.z;

                    //Cached path
                    if (UnitManager.Instance.UseCache && allPaths.ContainsKey(key) && (!uc.routed || ub.Length == 0))
                    {
                        allPaths.TryGetValue(key, out float3[] cachedPath);
                        for (int h = 0; h < cachedPath.Length; h++)
                        {
                            ub.Add(new UnitBuffer { wayPoints = cachedPath[h] });
                        }
                        uc.routed = true;
                        uc.usingCachedPath = true;
                        EntityManager.AddComponent<UnitRoutedComponent>(e);
                        return;
                    }
                    //Job
                    else if (!uc.routed || ub.Length == 0)
                    {
                        keys[counter] = key;
                        NavMeshQuery currentQuery = new NavMeshQuery(navMeshWorld, Allocator.Persistent, UnitManager.Instance.MaxPathNodePoolSize);
                        SinglePathFindingJob spfj = new SinglePathFindingJob()
                        {
                            query = currentQuery,
                            nml_FromLocation = uc.nml_FromLocation,
                            nml_ToLocation = uc.nml_ToLocation,
                            fromLocation = uc.fromLocation,
                            toLocation = uc.toLocation,
                            extents = extents,
                            maxIteration = UnitManager.Instance.MaxIterations,
                            result = results[counter],
                            statusOutput = statusOutputs[counter],
                            maxPathSize = UnitManager.Instance.MaxPathSize,
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
                if (UnitManager.Instance.UseCache && !allPaths.ContainsKey(keys[j]))
                {
                    float3[] wayPoints = new float3[statusOutputs[j][1]];
                    for (int k = 0; k < statusOutputs[j][1]; k++)
                    {
                        wayPoints[k] = results[j][k];
                    }
                    if (wayPoints.Length > 0)
                    {
                        allPaths.Add(keys[j], wayPoints);
                    }
                }

                UnitComponent uc = EntityManager.GetComponentData<UnitComponent>(routedEntities[j]);
                uc.routed = true;
                EntityManager.SetComponentData(routedEntities[j], uc);
                EntityManager.AddComponent<UnitRoutedComponent>(routedEntities[j]);
            }
            queries[j].Dispose();
            j++;
        }
        routedEntities.Clear();
        jobHandles.Clear();
        queries.Clear();

        //----------- Collision Avoidance Code -----------------


        NativeMultiHashMap<int, CovidPos>.ParallelWriter cellVsEntityPositionsParallelWriter = cellVsEntityPositions.AsParallelWriter();
        Entities
            //.WithNone<WaitComponent>()
            .WithNone<ToDestroyComponent>()
            .WithNone<HasCovidComponent>()
            .WithBurst(synchronousCompilation: true)
            .ForEach((ref UnitComponent uc, ref Translation trans, ref PersonComponent pc) =>
            {
                cellVsEntityPositionsParallelWriter.Add(GetUniqueKeyForPosition(trans.Value, 5), new CovidPos { pos = trans.Value, hasCovid = false });
            }).ScheduleParallel();

        Entities
            //.WithNone<WaitComponent>()
            .WithNone<ToDestroyComponent>()
            .WithAll<HasCovidComponent>()
            .WithBurst(synchronousCompilation: true)
            .ForEach((ref UnitComponent uc, ref Translation trans, ref PersonComponent pc) =>
            {
                cellVsEntityPositionsParallelWriter.Add(GetUniqueKeyForPosition(trans.Value, 5), new CovidPos { pos = trans.Value, hasCovid = true });
            }).ScheduleParallel();


        Entities
            //.WithNone<WaitComponent>()
            .WithNone<ToDestroyComponent>()
            .WithNone<HasCovidComponent>()
            .WithBurst(synchronousCompilation: true)
            .WithReadOnly(cellVsEntityPositions)
            .WithNativeDisableParallelForRestriction(randomArray)
            .ForEach((Entity e, int entityInQueryIndex, int nativeThreadIndex, ref UnitComponent uc, ref Translation trans, ref PersonComponent pc) =>
            {
                var random = randomArray[nativeThreadIndex];
                int key = GetUniqueKeyForPosition(trans.Value, 5);
                NativeMultiHashMapIterator<int> nmhKeyIterator;
                CovidPos otherEntityData;
                float3 otherEntityPos;
                bool otherEntityHasCovid;
                float currentDistance = 1.7f;
                int total = 0;
                float contagionPercentageValue = 0;
                float covidPercentage = 0;
                uc.avoidanceDirection = float3.zero;
                int totalInCell = 0;
                if (cellVsEntityPositions.TryGetFirstValue(key, out otherEntityData, out nmhKeyIterator))
                {
                    otherEntityPos = otherEntityData.pos;
                    otherEntityHasCovid = otherEntityData.hasCovid;

                    do
                    {
                        if (!trans.Value.Equals(otherEntityPos))
                        {
                            totalInCell++;

                            if (otherEntityHasCovid && math.abs(otherEntityPos.x - trans.Value.x) < infectionDistance && math.abs(otherEntityPos.z - trans.Value.z) < infectionDistance)
                            {
                                contagionPercentageValue = random.NextInt(0, 100);

                                if (pc.wearMask)
                                    covidPercentage = probabilityOfInfectionWithMaskWait;
                                else
                                    covidPercentage = probabilityOfInfectionWait;

                                if (contagionPercentageValue <= covidPercentage)
                                {
                                    ecbParallel.AddComponent(entityInQueryIndex, e, new HasCovidComponent { });
                                }
                            }

                            if (currentDistance > math.sqrt(math.lengthsq(trans.Value - otherEntityPos)))
                            {
                                currentDistance = math.sqrt(math.lengthsq(trans.Value - otherEntityPos));
                                float3 distanceFromTo = trans.Value - otherEntityPos;
                                uc.avoidanceDirection = math.normalize(distanceFromTo / currentDistance);
                                total++;
                            }
                        }
                    } while (cellVsEntityPositions.TryGetNextValue(out otherEntityData, ref nmhKeyIterator));
                    if (total > 0)
                    {
                        uc.avoidanceDirection = uc.avoidanceDirection / total;
                    }
                }
                randomArray[nativeThreadIndex] = random;
            }).ScheduleParallel();

        //----------- Collision Avoidance Code -----------------


        //Movement
        Entities
           .WithoutBurst()
           .WithNone<WaitComponent>()
           .WithNone<ToDestroyComponent>()
           .WithAll<UnitRoutedComponent>().ForEach((Entity e, int entityInQueryIndex, ref UnitComponent uc, ref DynamicBuffer<UnitBuffer> ub, ref DynamicBuffer<ScheduleBuffer> sb, ref Translation trans) =>
           {
               UnityEngine.AI.NavMeshHit outResult;
               Translation newTrans = trans;

               if (ub.Length > 0 && uc.routed)
               {
                   uc.waypointDirection = math.normalize(ub[uc.currentBufferIndex].wayPoints - trans.Value);
                   uc.avoidanceDirection.y = 0;
                   uc.waypointDirection.y = 0;
                   uc.waypointDirection = uc.waypointDirection + uc.avoidanceDirection;

                   newTrans.Value = trans.Value + uc.waypointDirection * uc.speed * deltaTime;
                   newTrans.Value.y = 1.791667f;

                   if (!UnityEngine.AI.NavMesh.SamplePosition(newTrans.Value, out outResult, 0.8f, NavMesh.AllAreas))
                   {
                       uc.waypointDirection -= uc.avoidanceDirection / 2;
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
                           ecb.RemoveComponent<UnitRoutedComponent>(e);
                           ecb.AddComponent(e, new WaitComponent
                           {
                               slotsToWait = sb[uc.count - 1].duration,
                               waitEndTime = 0
                           });

                       }
                       else if (uc.count == sb.Length - 1)
                       {
                           ecb.AddComponent<ToDestroyComponent>(e);
                       }
                   }
               }
           }).Run();

        Entities
            //.WithNone<WaitComponent>()
            .WithNone<ToDestroyComponent>()
            .WithAll<HasCovidComponent>()
            .WithoutBurst()
            .ForEach((Entity e, int entityInQueryIndex, ref PersonComponent pc) =>
            {
                ecbParallel.SetSharedComponent(entityInQueryIndex, e, new RenderMesh
                {
                    mesh = UnitManager.Instance.unitMesh,
                    material = UnitManager.Instance.covidMoveMaterial
                });
            }).ScheduleParallel();


        // destory all entities which need to be destroyed to avoid conflict with non parallel ecb buffer
        Entities
       .WithAll<ToDestroyComponent>()
       .WithBurst(synchronousCompilation: true)
       .ForEach((Entity e, int entityInQueryIndex) =>
       {
           ecbParallel.DestroyEntity(entityInQueryIndex, e);
       }).ScheduleParallel();


        cellVsEntityPositions.Dispose();
    }

    protected override void OnDestroy()
    {

        for (int n = 0; n <= UnitManager.Instance.MaxEntitiesRoutedPerFrame; n++)
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
        public DynamicBuffer<UnitBuffer> ub;
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
                        statusOutput[1] = straightPathCount;

                        for (int i = 0; i < straightPathCount; i++)
                        {
                            result[i] = (float3)res[i].position + new float3(0, 0.75f, 0); // elevated point
                            ub.Add(new UnitBuffer { wayPoints = result[i] });
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

public struct UnitRoutedComponent : IComponentData { }

public struct CovidPos
{
    public float3 pos;
    public bool hasCovid;
}
