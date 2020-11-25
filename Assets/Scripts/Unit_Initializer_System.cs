using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Unit_Initializer_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    public float elapsedTime;

    [NativeDisableParallelForRestriction] NativeArray<float3> roomsToVisit;
    [NativeDisableParallelForRestriction] NativeArray<int> roomNumbers;
    [NativeDisableParallelForRestriction] DynamicBuffer<Schedule_Buffer> ub;
    
    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }
    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer();
        int i, initX, num = 0;

        float3 room;

        var roomsToVisit = new NativeArray<float3>(UnitManager.instance.roomsToVisit, Allocator.Temp);
        var roomNumbers = new NativeArray<int>(UnitManager.instance.roomsToVisit, Allocator.Temp);

        elapsedTime += Time.DeltaTime;

        for (i = 0; i < UnitManager.instance.roomsToVisit; i++) {
            num = UnityEngine.Random.Range(0, 10);
            while (roomNumbers.Contains(num))
                num = UnityEngine.Random.Range(0, 10);

            roomNumbers[i] = num;
            room = GameObject.Find("Aula" + roomNumbers[i]).GetComponent<Renderer>().bounds.center;
            room.y = 2f;
            roomsToVisit[i] = room;
        }

        initX = UnityEngine.Random.Range(0, 40);
        
        if (elapsedTime > UnitManager.instance.spawnEvery)
        {
            elapsedTime = 0;
            Entities
                //.WithBurst(synchronousCompilation: true)
                .WithoutBurst()
                .ForEach((Entity e, int entityInQueryIndex, in Unit_Initializer_Component uic, in LocalToWorld ltw) =>
                {
                    for (int t = 0; t < uic.xGridCount; t++)
                    {
                        for (int j = 0; j < uic.zGridCount; j++)
                        {
                            Entity defEntity = ecb.Instantiate(uic.prefabToSpawn);
                            float3 position = new float3(initX, uic.baseOffset, 0) + uic.currentPosition;
                            
                            ecb.SetComponent(defEntity, new Translation { Value = position });
                            ecb.AddComponent<Unit_Component>(defEntity);
                            ecb.AddBuffer<Unit_Buffer>(defEntity);
                            //ecb.AddBuffer<Schedule_Buffer>(entityInQueryIndex, defEntity);

                            var ub = ecb.AddBuffer<Schedule_Buffer>(defEntity);
                            for (int k = 0; k < UnitManager.instance.roomsToVisit; k++) {
                                //ecb.AppendToBuffer(entityInQueryIndex, defEntity, new Schedule_Buffer { destination = roomsToVisit[k] });
                                ub.Add(new Schedule_Buffer { destination = roomsToVisit[k] });
                            }

                            Unit_Component uc = new Unit_Component();
                            uc.fromLocation = position;
                            uc.count = 0;
                            uc.toLocation = roomsToVisit[0];
                            uc.currentBufferIndex = 0;
                            uc.speed = (float)new Unity.Mathematics.Random(uic.seed + (uint)num + (uint)(t * j)).NextDouble(uic.minSpeed, uic.maxSpeed);
                            uc.minDistanceReached = uic.minDistanceReached;

                            uc.flag = false;

                            uc.firstPath = true;

                            ecb.SetComponent(defEntity, uc);
                        }
                    }
                    //ecb.DestroyEntity(entityInQueryIndex, e);
                }).Run();
        }
        bi_ECB.AddJobHandleForProducer(Dependency);

        roomsToVisit.Dispose();
        roomNumbers.Dispose();
    }
}
