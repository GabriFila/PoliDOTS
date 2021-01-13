using Assets.Scripts;
using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class UnitInitializerSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    private float elapsedTime;
    // the close time slot ("intorno" in italian math) is the slot which is about to start or last started, based if at the moment the sim is in the delay range before the neightTimeSLot of after it
    private int prevCloseTimeSlot;
    private int finalTimeSlot;
    List<DistributedSpawner> timeSlotSpawners;

    private List<Course> courses;
    List<int> availableCourseIDsInCurrentSlot = new List<int>();
    List<List<int>> availableCourseIDsPerSlot;

    private bool hasCovidAlreadySpawned = false;


    protected override void OnCreate()
    {
        Dictionary<string, string> configValues = Utils.GetConfigValues();
        int slotsInDay = int.Parse(configValues["SLOTS_IN_DAY"]);
        int totStudents = int.Parse(configValues["TOTAL_STUDENTS_ACROSS_DAY"]);
        int timeSlotDurationS = int.Parse(configValues["SLOT_DURATION_REAL_LIFE_MINUTES"]);
        float maxDelayPercentageTimeSlot = float.Parse(configValues["MAX_DELAY_PERCENTAGE_TIMESLOT"], CultureInfo.InvariantCulture.NumberFormat);
        int maxDelayS = (int)(timeSlotDurationS * maxDelayPercentageTimeSlot);
        int numberOfCourses = int.Parse(configValues["NUMBER_OF_COURSES"]);
        List<float> slotWeights = ParseSlotWeights(configValues["SPAWN_DISTRIBUTION"], slotsInDay);

        Debug.Log("Unit initializer config parsed");

        int numberOfRooms = 30;

        courses = new List<Course>();
        finalTimeSlot = 0;
        List<Lecture> lectures;
        for (int i = 0; i < numberOfCourses; i++)
        {
            lectures = ScheduleUtils.GenerateSchedule(slotsInDay, numberOfRooms, out int lectureStartTimeSlot);
            if (finalTimeSlot < lectureStartTimeSlot)
                finalTimeSlot = lectureStartTimeSlot;
            courses.Add(new Course(i, $"Course-{i}", lectures, lectureStartTimeSlot));
        }

        availableCourseIDsPerSlot = new List<List<int>>();
        for (int slotIdx = 0; slotIdx < slotsInDay; slotIdx++)
        {
            availableCourseIDsPerSlot.Add(new List<int>());
            for (int courseIdx = 0; courseIdx < courses.Count; courseIdx++)
            {
                if (courses[courseIdx].LectureStart == slotIdx + 1)
                {
                    availableCourseIDsPerSlot[slotIdx].Add(courses[courseIdx].Id);
                }
            }
        }

        timeSlotSpawners = SpawnerGenerator.GenerateSpawners(totStudents, slotWeights, maxDelayS);
        prevCloseTimeSlot = -1;

        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer();
        elapsedTime += Time.DeltaTime;


        int unitsToSpawn = 0;
        int currentTimeSlot = UnitManager.Instance.CurrentSlotNumber;

        // need to wait one second after each generation because at the moment the distribution of the units
        // among the delay before/after the start of a time slot ahs a resolution of one sec
        if (elapsedTime > 0.05 && currentTimeSlot < finalTimeSlot)
        {
            elapsedTime = 0;

            int slotDurationS = UnitManager.Instance.TimeSlotDurationS;
            int maxDelayS = UnitManager.Instance.MaxDelayS;

            int currentCloseTimeSlot = UnitManager.Instance.GetCloseTimeSlot();
            // when the sim enters the delay range for the before the next time slot
            if (currentCloseTimeSlot >= 0)
                unitsToSpawn = timeSlotSpawners[currentCloseTimeSlot].GetUnitsToSpawnNow();
            if (currentCloseTimeSlot > prevCloseTimeSlot)
            {
                // update prevNeighbour to avoid reenter next frame
                prevCloseTimeSlot = currentCloseTimeSlot;
                // compute courses for next slots
                availableCourseIDsInCurrentSlot = availableCourseIDsPerSlot[currentCloseTimeSlot];
            }

        }

        //spawn units when sim starts and every if another slot has passed and there are still some courses beginning in a later slot i enter the lambda
        if (unitsToSpawn > 0 && availableCourseIDsInCurrentSlot.Count > 0)
        {
            Entities
                .WithoutBurst()
                .ForEach((Entity e, int entityInQueryIndex, in UnitInitializerComponent uic, in LocalToWorld ltw) =>
                {
                    for (int j = 0; j < unitsToSpawn; j++)
                    {
                        Entity defEntity = ecb.Instantiate(uic.prefabToSpawn);
                        float3 position = new float3(UnityEngine.Random.Range(0, 36), uic.baseOffset, 0) + uic.currentPosition; //value 36 based on the spawner position (-28,0,-47)
                        bool hasCovid = false;
                        bool wearMask = false;
                        Material unitMaterial = UnitManager.Instance.healthyMoveMaterial;

                        ecb.SetComponent(defEntity, new Translation { Value = position });
                        ecb.AddComponent<UnitComponent>(defEntity);
                        ecb.AddComponent<PersonComponent>(defEntity);
                        ecb.AddComponent<CourseComponent>(defEntity);
                        ecb.AddBuffer<UnitBuffer>(defEntity);
                        DynamicBuffer<ScheduleBuffer> sb = ecb.AddBuffer<ScheduleBuffer>(defEntity);

                        //select randomly a course from the available ones
                        int selectedCourseId = availableCourseIDsInCurrentSlot[Utils.GenerateInt(availableCourseIDsInCurrentSlot.Count)];

                        Course selectedCourse = courses[selectedCourseId];
                        float3 currentDest;
                        float3 firstDest = 0;

                        //add lectures to Schedule_Buffer
                        for (int k = 0; k < selectedCourse.Lectures.Count; k++)
                        {
                            currentDest = Utils.FindDestination("Aula" + selectedCourse.Lectures[k].Room);
                            if (k == 0)
                                firstDest = currentDest;
                            sb.Add(new ScheduleBuffer
                            {
                                destination = currentDest,
                                duration = selectedCourse.Lectures[k].Duration
                            });
                        }

                        currentDest = Utils.FindExit("UscitaCastelidardo");
                        sb.Add(new ScheduleBuffer
                        {
                            destination = currentDest,
                            duration = -1
                        });
                        UnitComponent uc = new UnitComponent
                        {
                            fromLocation = position,
                            toLocation = firstDest,
                            speed = UnitManager.Instance.Speed,
                            minDistanceReached = uic.minDistanceReached,
                            count = 0,
                            currentBufferIndex = 0,
                            routed = false
                        };

                        if (!hasCovidAlreadySpawned) //only the first entity in the first slot has covid to simulate what can be the infection
                        {
                            hasCovidAlreadySpawned = true;
                            hasCovid = true;
                        }

                        CourseComponent courseComponent = new CourseComponent
                        {
                            id = selectedCourse.Id,
                            lectureStart = selectedCourse.LectureStart
                        };

                        if (UnityEngine.Random.Range(0, 100) <= (UnitManager.Instance.ProbabilityOfWearingMask * 100))
                            wearMask = true;

                        PersonComponent personComponent = new PersonComponent
                        {
                            age = Utils.GenerateInt(19, 30),
                            sex = Utils.GenerateSex(),
                            hasCovid = hasCovid,
                            wearMask = wearMask
                        };

                        if (hasCovid)
                        {
                            unitMaterial = UnitManager.Instance.covidMoveMaterial;
                        }

                        ecb.AddSharedComponent(e, new RenderMesh
                        {
                            mesh = UnitManager.Instance.unitMesh,
                            material = unitMaterial
                        });

                        ecb.SetComponent(defEntity, uc);
                        ecb.SetComponent(defEntity, courseComponent);
                        ecb.SetComponent(defEntity, personComponent);
                    }
                }).Run();
        }
        bi_ECB.AddJobHandleForProducer(Dependency);

    }

    private static List<float> ParseSlotWeights(string strArray, int slotsInDay)
    {
        string[] slotWeigthsStrs = strArray.Replace("[", "").Replace("]", "").Split(',');

        if (slotWeigthsStrs.Length != slotsInDay)
        {
            Debug.LogError("Slots weight length is not the same as slots in day");
            Application.Quit();
        }

        List<float> slotWeigths = new List<float>();
        float sumWeigths = 0;
        foreach (string str in slotWeigthsStrs)
        {
            float newWeigth = float.Parse(str, CultureInfo.InvariantCulture.NumberFormat);
            slotWeigths.Add(newWeigth);
            sumWeigths += newWeigth;
        }
        if (sumWeigths != 1)
        {
            Application.Quit();
            Debug.LogError("Slots weight total is not 1");
        }
        return slotWeigths;
    }

}
