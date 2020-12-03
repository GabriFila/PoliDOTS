using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UnitInitializerComponent : IComponentData
{
    public int numEntitiesToSpawn;
    public float baseOffset;
    public Entity prefabToSpawn;
    //New
    public float3 currentPosition;
    public int minSpeed;
    public int maxSpeed;
    public float minDistanceReached;
    public uint seed;
}