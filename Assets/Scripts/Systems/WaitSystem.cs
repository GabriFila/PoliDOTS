using Unity.Entities;
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
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        float elapsedTime = UnitManager.Instance.TimeSinceFirstSlot;
        int timeSlotDurationS = UnitManager.Instance.TimeSlotDurationS;
        float delayVariance = UnitManager.Instance.MaxDelayS;
        float halfDelayVariance = delayVariance / 2;

        Entities
            .WithoutBurst()
            .WithNativeDisableParallelForRestriction(randomArray)
            .ForEach((Entity e, int entityInQueryIndex, int nativeThreadIndex, ref WaitComponent wc, ref PersonComponent pc) =>
            {
                var random = randomArray[nativeThreadIndex];

                if (wc.waitEndTime == 0)
                {
                    // stop waiting when the next slot starts
                    wc.waitEndTime = timeSlotDurationS * (UnitManager.Instance.GetCloseTimeSlot() + wc.slotsToWait) + (random.NextFloat(delayVariance) - halfDelayVariance);
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
                randomArray[nativeThreadIndex] = random;
            }).ScheduleParallel();
    }
}
