using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;

public struct Unit_Component : IComponentData
{
    public float3 toLocation;
    public float3 fromLocation;
    public NavMeshLocation nml_FromLocation;
    public NavMeshLocation nml_ToLocation;
    public bool routed;
    public bool reached;
    public bool usingCachedPath;
    //Movement
    public float3 waypointDirection;
    public float speed;
    public float minDistanceReached;
    public int currentBufferIndex;
    //Collision Avoidance
    public float3 avoidanceDirection;
    public int count;
    public bool flag;

    public Vector3 nearestPoint;
    public float distance;

}
