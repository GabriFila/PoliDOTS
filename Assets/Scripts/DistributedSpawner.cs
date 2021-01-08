using System.Collections.Generic;
using UnityEngine;

public class DistributedSpawner
{

    private float spawnTimeResolutionS;
    private int totalUnitsToSpawn;
    private int spawnDurationS;
    private Queue<int> unitsPerInterval;
    private float timeLastSpawn = -1;

    public DistributedSpawner(float spawnTimeResolutionS, int totalUnitsToSpawn, int spawnDurationS)
    {
        unitsPerInterval = new Queue<int>();
        this.spawnTimeResolutionS = spawnTimeResolutionS;
        this.totalUnitsToSpawn = totalUnitsToSpawn;
        this.spawnDurationS = spawnDurationS;

        int numIntervals = (int)(spawnDurationS / spawnTimeResolutionS);
        int avgUnitsInInterval = totalUnitsToSpawn / numIntervals;
        int remUnitsToSpawn = totalUnitsToSpawn;

        // compute the weigths in order to have a triangle with area 1 from i=0 to i=numIntervals,
        //    where i=0 will be the start of the delay before the slot and i=numIntervals will be the end of the delay after the slot
        List<float> intervalWeigths = new List<float>();
        List<int> tempUnitsPerInterval = new List<int>();

        for (int i = 0; i < numIntervals / 2; i++)
        {
            intervalWeigths.Add((1f / (numIntervals * numIntervals * 0.25f) * i));
        }
        for (int i = numIntervals / 2; i < numIntervals; i++)
        {
            intervalWeigths.Add((1f / (numIntervals * numIntervals * 0.25f) * (numIntervals - i)));
        }


        foreach (float weight in intervalWeigths)
        {
            int unitsToSpawn = (int)Mathf.Floor(totalUnitsToSpawn * weight);
            if (remUnitsToSpawn > unitsToSpawn)
            {
                tempUnitsPerInterval.Add(unitsToSpawn);
                remUnitsToSpawn -= unitsToSpawn;
            }
            else
            {
                tempUnitsPerInterval.Add(remUnitsToSpawn);
                remUnitsToSpawn = 0;
            }
        }

        if (remUnitsToSpawn > 0)
            for (int i = 0; i < remUnitsToSpawn; i++)
            {
                tempUnitsPerInterval[i % tempUnitsPerInterval.Count]++;
            }

        foreach (int unitPerInt in tempUnitsPerInterval)
        {
            unitsPerInterval.Enqueue(unitPerInt);
        }

    }
    public int GetUnitsToSpawnNow()
    {
        int unitsToSpawn;
        if (timeLastSpawn == -1)
        {
            timeLastSpawn = Time.time;
            return unitsPerInterval.Dequeue();
        }
        else
        {
            unitsToSpawn = 0;
            int passedIntervals = (int)Mathf.Floor(((Time.time - timeLastSpawn) / spawnTimeResolutionS));
            if (passedIntervals > 0)
            {
                timeLastSpawn = Time.time;
                for (int i = 0; i < passedIntervals; i++)
                {
                    if (unitsPerInterval.Count > 0)
                        unitsToSpawn += unitsPerInterval.Dequeue();
                }
            }
            return unitsToSpawn;
        }

    }


}
