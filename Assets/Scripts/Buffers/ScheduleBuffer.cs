using Unity.Entities;
using Unity.Mathematics;

public struct ScheduleBuffer : IBufferElementData
{
    public float3 destination;
    public int duration;
}
