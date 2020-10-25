using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class Spawner : MonoBehaviour
{
    [SerializeField] private int entX = 5;
    [SerializeField] private int entZ = 5;
    [SerializeField] private Mesh unitMesh;
    [SerializeField] private Material unitMaterial;
    public NavMeshSurface surface;
    public NavMeshAgent agent;

    private void Start()
    {
        MakeEntity();
        surface.BuildNavMesh();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                agent.SetDestination(hit.point);
            }

        }
    }

    private void MakeEntity()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(NavMeshAgent)
            );

        NativeArray<Entity> entities = new NativeArray<Entity>(entX * entZ, Allocator.Temp);
        entityManager.CreateEntity(archetype, entities);

        GameObject room = GameObject.Find("Aula7");


        float defX = room.transform.position.x;
        float defZ = room.transform.position.z;

        for (int x = 0; x < entX; x++)
            for (int z = 0; z < entZ; z++)
            {
                Entity myEntity = entities[x * entZ + z];


                entityManager.AddComponentData(myEntity, new Translation
                {
                    Value = new float3(defX + 2f * x, 1f, defZ + 2f * z)
                });

                entityManager.AddSharedComponentData(myEntity, new RenderMesh
                {
                    mesh = unitMesh,
                    material = unitMaterial
                });
            }

        entities.Dispose();
    }
}
