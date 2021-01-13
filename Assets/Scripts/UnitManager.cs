using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    public int MaxPathSize { get; private set; }
    public int MaxEntitiesRoutedPerFrame { get; private set; }
    public int MaxPathNodePoolSize { get; private set; }
    public int MaxIterations { get; private set; }
    public int CurrentNumberOfCovid { get; set; }
    public int CurrentNumberOfStudents { get; set; }
    public int NumberOfStudentsUpToNow { get; set; }
    // initial value of current slot number is -1 because in the program is 0-based and the GUI it will be 0
    public int CurrentSlotNumber { get; private set; } = -1;
    public int TimeSlotDurationS { get; private set; }
    public float ProbabilityOfInfection { get; private set; }
    public float ProbabilityOfInfectionWithMask { get; private set; }
    public float ProbabilityOfWearingMask { get; private set; }
    public float InfectionDistance { get; private set; }
    public float ProbabilityOfInfectionWait { get; private set; }
    public float ProbabilityOfInfectionWithMaskWait { get; private set; }
    public float InfectionDistanceWait { get; private set; }
    public int MaxDelayS { get; private set; }
    public bool UseCache { get; private set; }
    public int TotalStudentsAcrossDay { get; private set; }
    public int InitialDelayTillFirstSlotS { get; private set; }
    public float TimeSinceFirstSlot { get; private set; }

    public int Speed { get; private set; } = 25;

    //configs not hardcoded
    public Material healthyMoveMaterial;
    public Material healthyWaitMaterial;
    public Material covidMoveMaterial;
    public Material covidWaitMaterial;
    public Mesh unitMesh;

    private float maxDelayPercentageTimeSlot;
    private int slotsInDay;
    private int firstSlotStartOffsetVis;
    private float percentageOfInfected;
    private float percentageOfCurrentInfected;
    private int totalNumberOfStudents;
    private int totalNumberOfCovid;
    private int secondsInRealLife;

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

        InitValues();
    }

    private void InitValues()
    {
        //fixed values
        MaxPathSize = 1024;
        MaxEntitiesRoutedPerFrame = 4000;
        MaxPathNodePoolSize = 1024;
        MaxIterations = 1024;
        //values read from config file
        Dictionary<string, string> configValues = Utils.GetConfigValues();

        string tempStartTime = configValues["START_TIME_REAL_LIFE"];

        int startTimeH = int.Parse(tempStartTime.Split(':')[0]);
        int startTimeM = int.Parse(tempStartTime.Split(':')[1]);
        firstSlotStartOffsetVis = (startTimeH * 60 + startTimeM) * 60;

        slotsInDay = int.Parse(configValues["SLOTS_IN_DAY"]);
        TotalStudentsAcrossDay = int.Parse(configValues["TOTAL_STUDENTS_ACROSS_DAY"]);
        TimeSlotDurationS = int.Parse(configValues["SLOT_DURATION_REAL_LIFE_MINUTES"]);
        Time.timeScale = float.Parse(configValues["TIME_SCALE"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfInfection = float.Parse(configValues["PROBABILITY_OF_INFECTION"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfInfectionWithMask = float.Parse(configValues["PROBABILITY_OF_INFECTION_WITH_MASK"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfWearingMask = float.Parse(configValues["PROBABILITY_OF_WEARING_MASK"], CultureInfo.InvariantCulture.NumberFormat);
        InfectionDistance = float.Parse(configValues["INFECTION_DISTANCE"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfInfectionWait = float.Parse(configValues["PROBABILITY_OF_INFECTION_WAIT"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfInfectionWithMaskWait = float.Parse(configValues["PROBABILITY_OF_INFECTION_WITH_MASK_WAIT"], CultureInfo.InvariantCulture.NumberFormat);
        InfectionDistanceWait = float.Parse(configValues["INFECTION_DISTANCE_WAIT"], CultureInfo.InvariantCulture.NumberFormat);
        maxDelayPercentageTimeSlot = float.Parse(configValues["MAX_DELAY_PERCENTAGE_TIMESLOT"], CultureInfo.InvariantCulture.NumberFormat);
        MaxDelayS = (int)(TimeSlotDurationS * maxDelayPercentageTimeSlot);
        UseCache = configValues["USE_CACHE"] == "true";
        // set the initial delay till first slot start to 2 times the maximum possible delay of a student in a sim
        InitialDelayTillFirstSlotS = (int)(maxDelayPercentageTimeSlot * TimeSlotDurationS * 1.5);
        Debug.Log("Unit manager config parsed");
    }


    private void Update()
    {
        // handle GUI time and current slot for entire sim (GUI and program)
        TimeSinceFirstSlot = Time.time - InitialDelayTillFirstSlotS;
        if (TimeSinceFirstSlot > 0)
        {
            CurrentSlotNumber = (int)TimeSinceFirstSlot / TimeSlotDurationS;
        }
        secondsInRealLife = (int)((TimeSinceFirstSlot) * 60) + firstSlotStartOffsetVis;

    }

    public void OnGUI()
    {
        int padding;
        int boxXPosition;
        int boxHeight;
        int boxWidth;
        int boxYPosition;

        boxXPosition = 5;
        padding = 2;
        boxHeight = Screen.height / 20;
        boxWidth = Screen.width / 4;
        GUI.skin.box.fontSize = boxWidth / 20;
        GUI.skin.box.alignment = TextAnchor.MiddleLeft;
        boxYPosition = 20;

        // Time and slot info


        int hours = secondsInRealLife / 3600;
        int minutes = (secondsInRealLife % 3600) / 60;

        if (minutes < 10)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Time: " + hours + ":0" + minutes);
        else
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Time: " + hours + ":" + minutes);
        boxYPosition += (boxHeight + padding);
        if (CurrentSlotNumber == -1)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot: " + "before lecture start");
        else if (CurrentSlotNumber < slotsInDay)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot: " + (CurrentSlotNumber + 1) + "/" + slotsInDay);
        else
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot: end of the day ");




        // Config info
        boxYPosition += (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Probability of students with a mask: " + ProbabilityOfWearingMask * 100 + "%");
        boxYPosition += (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection with mask: " + ProbabilityOfInfectionWithMask * 100 + "%");
        boxYPosition += (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection: " + ProbabilityOfInfection * 100 + "%");


        // General info
        boxYPosition = Screen.height - 40;

        percentageOfCurrentInfected = CurrentNumberOfStudents == 0 ? 0 : (CurrentNumberOfCovid * 100 / CurrentNumberOfStudents);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed students inside now: " + percentageOfCurrentInfected + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed to Covid-19 inside now: " + CurrentNumberOfCovid);
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Students in PoliTO inside now: " + CurrentNumberOfStudents);

        boxYPosition -= (boxHeight + padding);
        boxYPosition -= (boxHeight + padding);
        totalNumberOfStudents = NumberOfStudentsUpToNow;
        //totalNumberOfCovid = CurrentNumberOfCovid + TotNumberOfCovidExit;
        percentageOfInfected = totalNumberOfStudents == 0 ? 0 : (totalNumberOfCovid * 100 / totalNumberOfStudents);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed students up to now: " + percentageOfInfected + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed to Covid-19 up to now: " + totalNumberOfCovid);
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Students in PoliTO up to now: " + totalNumberOfStudents);

        if (CurrentNumberOfStudents == 0 && CurrentSlotNumber >= slotsInDay)
        {
            bool quitGame = GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height - 200, 200, 20), "Day ended, click to quit");
            if (quitGame)
                Application.Quit();
        }

    }


    public int GetCloseTimeSlot()
    {
        // if currentTime is in delay range before the nextTimeSlot return the next time slot
        if (TimeSinceFirstSlot > (CurrentSlotNumber + 1) * TimeSlotDurationS - MaxDelayS)
        {
            return CurrentSlotNumber + 1;
        }
        return CurrentSlotNumber;
    }


}
