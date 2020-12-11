using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager instance { get; private set; }
    public int maxPathSize;
    public int maxEntitiesRoutedPerFrame;
    public int maxPathNodePoolSize;
    public int maxIterations;
    public bool useCache;
    public int timeSlotDurationS;
    public float percentageOfInfectionX100;
    public float percentageOfInfectionWithMaskX100;
    public float percentageOfWearingMaskX100;
    public float infectionDistance;
    
    public Material healthyMoveMaterial;
    public Material healthyWaitMaterial;
    public Material covidMoveMaterial;
    public Material covidWaitMaterial;
    public Mesh unitMesh;

    private int totNumberOfStudents;
    private int totNumberOfCovid;
    private int currentSlotNumber;
   
    public void SetNumberOfStudents(int totNumberOfStudents)
    {
        this.totNumberOfStudents = totNumberOfStudents;
    }

    public void SetNumberOfCovid(int totNumberOfCovid)
    {
        this.totNumberOfCovid = totNumberOfCovid;
    }
    public void SetCurrentSlotNumber(int currentSlotNumber)
    {
        this.currentSlotNumber = currentSlotNumber;
    }

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
    public void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 250, 25), "Students inside POLITO : " + totNumberOfStudents);
        GUI.Box(new Rect(10, 35, 250, 25), "Exposed to COVID-19 : " + totNumberOfCovid);
        GUI.Box(new Rect(10, 60, 250, 25), "Current timeslot : " + currentSlotNumber + "/7");
        GUI.Box(new Rect(10, 85, 250, 25), "Risk of infection : " + percentageOfInfectionX100);
        GUI.Box(new Rect(10, 110, 250, 25), "Risk of infection with mask : " + percentageOfInfectionWithMaskX100);

        GUI.skin.box.fontSize = 15;
    }

}
