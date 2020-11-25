using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Unit_Initializer_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    public float elapsedTime;

    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }
    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer().AsParallelWriter();
        int i, initX, roomsNumber, num = 0;

        float3 room;

        NativeArray<float3> roomsToVisit = new NativeArray<float3>(UnitManager.instance.roomsToVisit, Allocator.Temp);
        NativeArray<int> roomNumbers = new NativeArray<int>(UnitManager.instance.roomsToVisit, Allocator.Temp);
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        elapsedTime += Time.DeltaTime;

        for (i = 0; i < UnitManager.instance.roomsToVisit; i++)
        {
            num = UnityEngine.Random.Range(0, 10);
            while (roomNumbers.Contains(num))
                num = UnityEngine.Random.Range(0, 10);

            roomNumbers[i] = num;
            room = GameObject.Find("Aula" + roomNumbers[i]).GetComponent<Renderer>().bounds.center;
            room.y = 2f;
            roomsToVisit[i] = room;
        }

        initX = UnityEngine.Random.Range(0, 40);
        roomsNumber = UnitManager.instance.roomsToVisit;

        if (elapsedTime > UnitManager.instance.spawnEvery)
        {
            elapsedTime = 0;
            Entities
                .WithReadOnly(roomsToVisit)
                .WithBurst(synchronousCompilation: true)
                .ForEach((Entity e, int entityInQueryIndex, in Unit_Initializer_Component uic, in LocalToWorld ltw) =>
                {
                    var random = randomArray[entityInQueryIndex];

                    for (int t = 0; t < uic.xGridCount; t++)
                    {
                        for (int j = 0; j < uic.zGridCount; j++)
                        {
                            Entity defEntity = ecb.Instantiate(entityInQueryIndex, uic.prefabToSpawn);
                            float3 position = new float3(initX, uic.baseOffset, 0) + uic.currentPosition;

                            ecb.SetComponent(entityInQueryIndex, defEntity, new Translation { Value = position });
                            ecb.AddComponent<Unit_Component>(entityInQueryIndex, defEntity);
                            ecb.AddBuffer<Unit_Buffer>(entityInQueryIndex, defEntity);
                            ecb.AddBuffer<Schedule_Buffer>(entityInQueryIndex, defEntity);

                            for (int k = 0; k < roomsNumber; k++)
                            {
                                ecb.AppendToBuffer(entityInQueryIndex, defEntity, new Schedule_Buffer { destination = roomsToVisit[random.NextInt(roomsNumber)] });
                            }

                            Unit_Component uc = new Unit_Component();
                            uc.fromLocation = position;
                            uc.count = 0;
                            uc.toLocation = roomsToVisit[0];
                            uc.currentBufferIndex = 0;
                            uc.speed = (float)new Unity.Mathematics.Random(uic.seed + (uint)num + (uint)(t * j)).NextDouble(uic.minSpeed, uic.maxSpeed);
                            uc.minDistanceReached = uic.minDistanceReached;

                            ecb.SetComponent(entityInQueryIndex, defEntity, uc);
                            randomArray[entityInQueryIndex] = random;
                        }
                    }
                    //ecb.DestroyEntity(entityInQueryIndex, e);
                }).ScheduleParallel();
        }
        bi_ECB.AddJobHandleForProducer(Dependency);

        roomsToVisit.Dispose();
        roomNumbers.Dispose();
    }
}
