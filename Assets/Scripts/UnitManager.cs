using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager instance { get; private set; }
    public int maxPathSize;
    public int maxEntitiesRoutedPerFrame;
    public int maxPathNodePoolSize;
    public int maxIterations;
    public bool useCache;
    public float spawnEvery;
    public int roomsToVisit;
    public int numberOfRooms;
    public Material waitMaterial;
    public Material activeMaterial;
    public Mesh unitMesh;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
