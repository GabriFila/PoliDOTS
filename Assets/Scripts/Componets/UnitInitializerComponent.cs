using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UnitInitializerComponent : IComponentData
{
    public float baseOffset;
    public Entity prefabToSpawn;
    //New
    public float3 currentPosition;
    public float minDistanceReached;
    public uint seed;
}