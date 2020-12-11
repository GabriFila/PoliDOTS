using Unity.Entities;

public struct PersonComponent : IComponentData
{
    public int age;
    public char sex;
    public bool hasCovid;
    public bool wearMask;
}