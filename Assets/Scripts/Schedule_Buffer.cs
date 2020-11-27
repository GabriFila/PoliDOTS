using Unity.Entities;
using Unity.Mathematics;

public struct Schedule_Buffer : IBufferElementData
{
    public float3 destination;
    public int duration;
}
