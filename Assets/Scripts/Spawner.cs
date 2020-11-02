using System.Linq;
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
    [SerializeField] public int totUnits;

    //public NavMeshAgent agent;

    private void Start()
    {
        MakePlayers();
        //MakeEntity();
    }

    private void MakePlayers()
    {
        var rand = new System.Random();
        GameObject[] rooms = new GameObject[] {
            GameObject.Find("Aula2"),
            GameObject.Find("Aula3"),
            GameObject.Find("Aula4"),
            GameObject.Find("Aula5"),
            GameObject.Find("Aula6"),
            GameObject.Find("Aula7"),
            GameObject.Find("AulaMagna"),
        };
        Vector3[] targets = rooms.Select(room => room.transform.position).ToArray();


        for (int i = 0; i < totUnits; i++)
        {

            Vector3 pos = new Vector3(UnityEngine.Random.Range(-20f, 5f), 1.5f, UnityEngine.Random.Range(-20f, 20f));
            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
            var obj = Instantiate(playerPrefab, pos, rot) as GameObject;

            NavMeshAgent navMeshAgent = obj.GetComponent<NavMeshAgent>();
            navMeshAgent.destination = targets[rand.Next(targets.Length)];
        }
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
