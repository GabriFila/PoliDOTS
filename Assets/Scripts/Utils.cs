using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public static class Utils
{
    public static Dictionary<string, string> GetConfigValues()
    {
        Dictionary<string, string> configValues = new Dictionary<string, string>();

        try
        {
            using (StreamReader sr = new StreamReader("./config.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line[0] != '#')
                        configValues.Add(line.Split('=')[0], line.Split('=')[1]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            Application.Quit();
        }
        if (configValues.Count != 16)
        {
            Application.Quit();
            Debug.LogError("Wrong format for Init.txt -> Not enough values");
        }

        return configValues;
    }

    public static int GenerateInt(int v1, int v2)
    {
        return UnityEngine.Random.Range(v1, v2);
    }

    public static int GenerateInt(int v1)
    {
        return GenerateInt(0, v1);
    }

    public static char GenerateSex()
    {
        int sex = GenerateInt(2);
        if (sex == 0)
            return 'M';
        else
            return 'F';
    }

    public static float3 FindDestination(string roomName)
    {
        float3 destination = GameObject.Find(roomName).GetComponent<Renderer>().bounds.center;
        float3 dimension = GameObject.Find(roomName).GetComponent<Renderer>().bounds.size;

        destination.x += Utils.GenerateInt(-(int)dimension.x / 2, (int)dimension.x / 2);
        destination.y = 2f;
        destination.z += Utils.GenerateInt(-(int)dimension.z / 2, (int)dimension.z / 2);

        return destination;
    }

    public static float3 FindExit(string exit)
    {
        float3 destination;
        destination = GameObject.Find(exit).GetComponent<Renderer>().bounds.center;
        destination.x += Utils.GenerateInt(-10, 13);
        destination.z += Utils.GenerateInt(-3, 4);
        destination.y = 2f;

        return destination;
    }
}
