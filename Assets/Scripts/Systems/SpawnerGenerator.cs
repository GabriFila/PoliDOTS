using System.Collections.Generic;
using UnityEngine;

public static class SpawnerGenerator
{

    public static List<DistributedSpawner> GenerateSpawners(int totUnits, List<float> slotWeights, int maxDelayS)
    {
        List<DistributedSpawner> timeSlotSpawners = new List<DistributedSpawner>();
        foreach (int ups in computeUnitsToSpawnOnEachSlot(totUnits, slotWeights))
        {
            timeSlotSpawners.Add(new DistributedSpawner(0.1f, ups, maxDelayS * 2));
        }
        return timeSlotSpawners;
    }

    private static List<int> computeUnitsToSpawnOnEachSlot(int totUnits, List<float> slotWeights)
    {
        List<int> unitsPerSlot = new List<int>();
        int remUnits = totUnits;
        // fill the list with the best approximation respect to the given weigths
        foreach (float weigth in slotWeights)
        {
            int unitsToAdd = (int)Mathf.Floor(weigth * totUnits);
            if (remUnits > unitsToAdd)
            {
                unitsPerSlot.Add(unitsToAdd);
                remUnits -= unitsToAdd;
            }
            else
            {
                unitsPerSlot.Add(remUnits);
                remUnits = 0;
            }
        }
        // the approximation could have left still some units to assign, distrbuite them equally
        if (remUnits > 0)
            for (int i = 0; i < remUnits; i++)
            {
                unitsPerSlot[i % unitsPerSlot.Count]++;
            }

        return unitsPerSlot;
    }
}
