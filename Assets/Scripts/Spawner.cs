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
        var rand = new System.Random();
        GameObject room3 = GameObject.Find("Aula3");
        GameObject room7 = GameObject.Find("Aula7");
        GameObject room8 = GameObject.Find("Aula8");
        GameObject room4 = GameObject.Find("Aula4");
        GameObject room5 = GameObject.Find("Aula5");
        GameObject room6 = GameObject.Find("Aula6");
        GameObject roomM = GameObject.Find("AulaMagna");
        Vector3[] targets = { room3.transform.position, room7.transform.position, room5.transform.position, room6.transform.position, roomM.transform.position, room8.transform.position, room4.transform.position };
        for (int i = 0; i < totUnits; i++)
        {

            Vector3 pos = new Vector3(UnityEngine.Random.Range(-10f, 10f), 1.5f, UnityEngine.Random.Range(-1f, 1f));
            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
            var obj = Instantiate(playerPrefab, pos, rot) as GameObject;

            myAgent = obj.GetComponent<NavMeshAgent>();
            //myAgent.destination = room3.transform.position;
            myAgent.destination = targets[rand.Next(targets.Length)];
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
