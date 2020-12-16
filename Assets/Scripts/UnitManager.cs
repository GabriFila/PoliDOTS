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

    public int totNumberOfStudents { get; set; }
    private int totNumberOfCovid;
    public int currentSlotNumber { get; set; }
    private float percentageOfInfected;

    private int boxHeight;
    private int boxWidth;
    private int padding;
    private int boxXPosition;
    private int boxYPosition;
    private int seconds = 30600;

    public void SetNumberOfCovid(int totNumberOfCovid)
    {
        this.totNumberOfCovid = totNumberOfCovid;
    }

    public void UpdateSeconds(int numOfSeconds)
    {
        seconds += numOfSeconds;
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

        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;

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
        if (minutes < 10)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot(hour) : " + hours + " : 0" + minutes);
        else
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot(hour) : " + hours + " : " + minutes);
        boxYPosition -= (boxHeight + padding);
        percentageOfInfected = totNumberOfStudents == 0 ? 0 : (totNumberOfCovid * 100 / totNumberOfStudents);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Percentage of exposed students : " + percentageOfInfected + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed to COVID-19 : " + totNumberOfCovid);
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Students inside POLITO : " + totNumberOfStudents);

        GUI.skin.box.fontSize = boxWidth / 20;
    }

}
