using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    public int MaxPathSize { get; private set; }
    public int MaxEntitiesRoutedPerFrame { get; private set; }
    public int MaxPathNodePoolSize { get; private set; }
    public int MaxIterations { get; private set; }
    public int TotNumberOfStudents { get; set; }
    public int TotNumberOfCovid { get; set; }
    public int CurrentSlotNumber { get; set; }

    public int TimeSlotDurationS { get; private set; }
    public float ProbabilityOfInfection { get; private set; }
    public float ProbabilityOfInfectionWithMask { get; private set; }
    public float ProbabilityOfWearingMask { get; private set; }
    public float InfectionDistance { get; private set; }
    public float DelayPercentageTimeSlot { get; private set; }
    public bool UseCache { get; private set; }
    public int NumEntitiesToSpawn { get; private set; }
    private int MaxSlotsInSingleDay { get; set; }

    public int Speed { get; private set; } = 25;

    //configs not hardcoded
    public Material healthyMoveMaterial;
    public Material healthyWaitMaterial;
    public Material covidMoveMaterial;
    public Material covidWaitMaterial;
    public Mesh unitMesh;

    private int startTimeSeconds;
    private float percentageOfInfected;
    private int boxHeight;
    private int boxWidth;
    private int padding;
    private int boxXPosition;
    private int boxYPosition;
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
        Dictionary<string, string> configValues = GetConfigValues();

        string tempStartTime = configValues["START_TIME_REAL_LIFE"];

        int startTimeH = int.Parse(tempStartTime.Split(':')[0]);
        int startTimeM = int.Parse(tempStartTime.Split(':')[1]);
        startTimeSeconds = (startTimeH * 60 + startTimeM) * 60;

        MaxSlotsInSingleDay = int.Parse(configValues["MAX_SLOTS_IN_SINGLE_DAY"]);
        NumEntitiesToSpawn = int.Parse(configValues["NUM_ENTITIES_TO_SPAWN"]);
        TimeSlotDurationS = int.Parse(configValues["SLOT_DURATION_REAL_LIFE_MINUTES"]);
        ProbabilityOfInfection = float.Parse(configValues["PROBABILITY_OF_INFECTION"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfInfectionWithMask = float.Parse(configValues["PROBABILITY_OF_INFECTION_WITH_MASK"], CultureInfo.InvariantCulture.NumberFormat);
        ProbabilityOfWearingMask = float.Parse(configValues["PROBABILITY_OF_WEARING_MASK"], CultureInfo.InvariantCulture.NumberFormat);
        InfectionDistance = float.Parse(configValues["INFECTION_DISTANCE"], CultureInfo.InvariantCulture.NumberFormat);
        DelayPercentageTimeSlot = float.Parse(configValues["DELAY_PERCENTAGE_TIMESLOT"], CultureInfo.InvariantCulture.NumberFormat);
        UseCache = configValues["USE_CACHE"] == "true";
    }

    public static Dictionary<string, string> GetConfigValues()
    {
        Dictionary<string, string> configValues = new Dictionary<string, string>();

        try
        {
            using (StreamReader sr = new StreamReader("./config.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    configValues.Add(line.Split('=')[0], line.Split('=')[1]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        if (configValues.Count != 10)
            Debug.LogError("Wrong format for Init.txt -> Not enough values");

        return configValues;
    }

    private void Update()
    {
        secondsInRealLife = (int)(Time.time * 60) + startTimeSeconds;
    }

    public void OnGUI()
    {
        padding = 2;
        boxWidth = Screen.width / 4;
        boxHeight = Screen.height / 20;

        int hours = secondsInRealLife / 3600;
        int minutes = (secondsInRealLife % 3600) / 60;

        boxXPosition = 5;
        boxYPosition = Screen.height - 40;
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Probability of students with a mask : " + ProbabilityOfWearingMask * 100 + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection with mask : " + ProbabilityOfInfectionWithMask * 100 + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Risk of infection : " + ProbabilityOfInfection * 100 + "%");
        boxYPosition -= (boxHeight + padding);
        if (CurrentSlotNumber <= 7)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot : " + CurrentSlotNumber + "/" + MaxSlotsInSingleDay);
        else
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot : end of the day ");
        boxYPosition -= (boxHeight + padding);
        if (minutes < 10)
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot(hour) : " + hours + " : 0" + minutes);
        else
            GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Current timeslot(hour) : " + hours + " : " + minutes);
        boxYPosition -= (boxHeight + padding);
        percentageOfInfected = TotNumberOfStudents == 0 ? 0 : (TotNumberOfCovid * 100 / TotNumberOfStudents);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Percentage of exposed students : " + percentageOfInfected + "%");
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Exposed to COVID-19 : " + TotNumberOfCovid);
        boxYPosition -= (boxHeight + padding);
        GUI.Box(new Rect(boxXPosition, boxYPosition, boxWidth, boxHeight), "Students inside POLITO : " + TotNumberOfStudents);

        GUI.skin.box.fontSize = boxWidth / 20;
    }

}
