using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class Spawner : MonoBehaviour
{
    //[SerializeField] private int entX = 5;
    //[SerializeField] private int entZ = 5;
    //[SerializeField] private Mesh unitMesh;
    //[SerializeField] private Material unitMaterial;
    [SerializeField] public NavMeshSurface surface;
    [SerializeField] public GameObject playerPrefab;
    private NavMeshAgent myAgent;

    //public NavMeshAgent agent;

    private void Start()
    {

        surface.BuildNavMesh();
        MakeEntity();

        /*GameObject room = GameObject.Find("Aula3");
        Debug.Log(room.transform.position);
        agent.destination = new Vector3(room.transform.position.x, 0, room.transform.position.z);*/
    }


    private void Update()
    {
        /*
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                agent.SetDestination(hit.point);
            }

        }
        */
    }


    private void MakeEntity()
    {

        GameObject room = GameObject.Find("Aula3");

        for (int i = 0; i < 5; i++)
        {

            Vector3 pos = new Vector3(UnityEngine.Random.Range(-10f, 10f), 1.5f, UnityEngine.Random.Range(-1f, 1f));
            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
            var obj = Instantiate(playerPrefab, pos, rot) as GameObject;

            myAgent = obj.GetComponent<NavMeshAgent>();
            myAgent.destination = room.transform.position;
        }

        /*EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(NavMeshAgent)
            );

        NativeArray<Entity> entities = new NativeArray<Entity>(1, Allocator.Temp);
        entityManager.CreateEntity(archetype, entities);

        GameObject room = GameObject.Find("Aula3");

        float defX = room.transform.position.x;
        float defZ = room.transform.position.z;

        Debug.Log(room.transform.position.x);
        Debug.Log(room.transform.position.y);
        Debug.Log(room.transform.position.z);


        for (int i = 0; i < entities.Length; i++) {
                
            Entity myEntity = entities[i];

            entityManager.AddComponentData(myEntity, new Translation
            {
                Value = new float3(UnityEngine.Random.Range(-10f, 10f), 1.5f, UnityEngine.Random.Range(-1f, 1f))
            }) ;

            entityManager.AddSharedComponentData(myEntity, new RenderMesh
            {
                mesh = unitMesh,
                material = unitMaterial
            });

            //agent = new NavMeshAgent();
            //agent.destination = new Vector3(defX, 0, defZ);

            entityManager.AddComponentObject(myEntity, new NavMeshAgent());
           
        }

        entities.Dispose();
        */
    }
}
