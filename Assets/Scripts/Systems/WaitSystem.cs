using System;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class WaitSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    public float elapsedTime;

    long timeSlotDurationMs = 1000 * 1;
    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer().AsParallelWriter();

        long currentUnixTimeS = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        long slotDuration = timeSlotDurationMs;

        Entities
            .WithoutBurst()
            .ForEach((Entity e, int entityInQueryIndex, ref WaitComponent wc, ref PersonComponent pc) =>
            {
                if (wc.waitEndTime == 0)
                    wc.waitEndTime = currentUnixTimeS + wc.slotsToWait * UnitManager.Instance.timeSlotDurationS;
                else if (currentUnixTimeS > wc.waitEndTime)
                {
                    ecb.RemoveComponent<WaitComponent>(entityInQueryIndex, e);
                    // TODO when enabling burst the following line throws a one-time error, which doesn't seem to affect the execution

                    Material unitMaterial;

                    if (pc.hasCovid)
                        unitMaterial = UnitManager.Instance.covidMoveMaterial;
                    else
                        unitMaterial = UnitManager.Instance.healthyMoveMaterial;

                    ecb.SetSharedComponent(entityInQueryIndex, e, new RenderMesh
                    {
                        mesh = UnitManager.Instance.unitMesh,
                        material = unitMaterial
                    });
                }
            }).ScheduleParallel();
    }
}
