﻿using System;
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

        long currentUnixTimeMs = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
        long slotDuration = timeSlotDurationMs;

        Entities
            .WithoutBurst()
            .ForEach((Entity e, int entityInQueryIndex, ref WaitComponent wc, ref PersonComponent pc) =>
            {
                if (wc.waitEndTime == 0)
                    wc.waitEndTime = currentUnixTimeMs + wc.slotsToWait * slotDuration;
                else if (currentUnixTimeMs > wc.waitEndTime)
                {
                    ecb.RemoveComponent<WaitComponent>(entityInQueryIndex, e);
                    // TODO when enabling burst the following line throws a one-time error, which doesn't seem to affect the execution

                    Material unitMaterial;

                    if (pc.hasCovid)
                        unitMaterial = UnitManager.instance.covdMaterial;
                    else
                        unitMaterial = UnitManager.instance.activeMaterial;
                    
                    ecb.SetSharedComponent(entityInQueryIndex, e, new RenderMesh
                    {
                        mesh = UnitManager.instance.unitMesh,
                        material = unitMaterial
                    });
                }
            }).ScheduleParallel();
    }
}