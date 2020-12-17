﻿using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class WaitSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;

    protected override void OnCreate()
    {
        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer().AsParallelWriter();

        float elapsedTime = (float)Time.ElapsedTime;
        int timeSlotDurationS = UnitManager.Instance.timeSlotDurationS;
        Entities
            .WithoutBurst()
            .ForEach((Entity e, int entityInQueryIndex, ref WaitComponent wc, ref PersonComponent pc) =>
            {
                if (wc.waitEndTime == 0)
                {
                    // stop waiting when the next slot starts
                    wc.waitEndTime = timeSlotDurationS * ((UnitManager.Instance.currentSlotNumber - 1) + wc.slotsToWait);
                }
                else if (elapsedTime > wc.waitEndTime)
                {
                    ecb.RemoveComponent<WaitComponent>(entityInQueryIndex, e);

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
