using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    public int maxPathSize;
    public int maxEntitiesRoutedPerFrame;
    public int maxPathNodePoolSize;
    public int maxIterations;
    public bool useCache;
    public int timeSlotDurationS;
    public float probabilityOfInfection;
    public float probabilityOfInfectionWithMask;
    public float probabilityOfWearingMask;
    public float infectionDistance;
    
    public Material healthyMoveMaterial;
    public Material healthyWaitMaterial;
    public Material covidMoveMaterial;
    public Material covidWaitMaterial;
    public Mesh unitMesh;

    private int totNumberOfStudents;
    private int totNumberOfCovid;
    private int currentSlotNumber;
    private float percentageOfInfected;

    private int boxHeight;
    private int boxWidth;
    private int padding;
    private int boxXPosition;
    private int boxYPosition;

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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void OnGUI()
    {
        padding = 2;
        boxWidth = Screen.width / 4;
        boxHeight = Screen.height / 20;

        boxXPosition = 5;
        boxYPosition = Screen.height - 40;
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Percentage of students with a mask : " + probabilityOfWearingMask * 100 + "%");
        boxYPosition -= (boxHeight + padding); 
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection with mask : " + probabilityOfInfectionWithMask * 100 + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection : " + probabilityOfInfection * 100 + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot : " + currentSlotNumber + "/7");
        boxYPosition -= (boxHeight + padding);
        percentageOfInfected = (totNumberOfCovid * 100 / totNumberOfStudents);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Percentage of exposed students : " + percentageOfInfected + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed to COVID-19 : " + totNumberOfCovid);
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Students inside POLITO : " + totNumberOfStudents);

        GUI.skin.box.fontSize = boxHeight / 2;
    }

}
