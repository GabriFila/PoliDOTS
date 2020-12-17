using Unity.Entities;

public struct WaitComponent : IComponentData
{
    // time is in unix format and needs to be that way to make it DOTS compliant, since the class Time is not suitable as a component field
    public float waitEndTime;
    public int slotsToWait;
}
