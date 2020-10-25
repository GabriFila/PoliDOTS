using Unity.Entities;
//using UnityEngine.AI;
using Unity.Transforms;

public class NavMeshSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation navMesh) => { }
        );
    }
}
