using Assets.Scripts;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

using System.IO;
using System;

public class UnitInitializerSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    public float elapsedTime;
    public int timeSlot;
    public List<Course> courses;
    public int currentCourse;

    public int lastSlot;
    public int numberOfCourses;

    public int numberOfRooms;
    public int maxSlotsInSingleDay;
    public int courseComponent0 = 0;
    public int totCourses = 0;

    NativeArray<int> availableCoursesIds;

    protected override void OnCreate()
    {
        maxSlotsInSingleDay = int.Parse(UnitManager.GetConfigValues()["MAX_SLOTS_IN_SINGLE_DAY"]);
        numberOfRooms = 30;
        timeSlot = 0;

        numberOfCourses = CourseName.GetValues(typeof(CourseName)).Length;
        courses = new List<Course>();
        lastSlot = 0;
        List<Lecture> lectures;
        for (int count = 0; count < numberOfCourses; count++)
        {
            int lectureStart;
            lectures = GenerateSchedule(out lectureStart);

            int totDuration = 0;
            if (lastSlot < lectureStart)
                lastSlot = lectureStart;
            courses.Add(new Course(count, CourseName.GetValues(typeof(CourseName)).GetValue(count).ToString(), lectures, lectureStart));

            for (int k = 0; k < courses[count].Lectures.Count; k++)
                totDuration += courses[count].Lectures[k].Duration;

            int startL = lectureStart;
            for (int k = 0; k < courses[count].Lectures.Count; k++)
            {
                startL += courses[count].Lectures[k].Duration;
            }

        }

        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }

    private int GetMaxSlotsInSingleDay()
    {
        int slots = 0;
        try
        {
            using (StreamReader sr = new StreamReader("../PoliDOTS/Assets/Init/Init.txt"))
            {
                string line = sr.ReadLine();
                slots = int.Parse(line.Split('=')[1]);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            maxSlotsInSingleDay = 8;
        }
        return slots;
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer();
        elapsedTime += Time.DeltaTime;


        //spawn units when sim starts and every if another slot has passed and there are still some courses beginning in a later slot i enter the lambda
        if (timeSlot == 0 || (elapsedTime > UnitManager.Instance.TimeSlotDurationS && timeSlot <= lastSlot))
        {
            elapsedTime = 0;
            timeSlot++;

            UnitManager.Instance.CurrentSlotNumber = timeSlot;

            availableCoursesIds = new NativeArray<int>(CourseName.GetValues(typeof(CourseName)).Length, Allocator.Temp);
            int numberAvailableCourses = 0;
            for (int k = 0; k < courses.Count; k++)
            {
                if (courses[k].LectureStart == timeSlot)
                {
                    availableCoursesIds[numberAvailableCourses] = courses[k].Id;
                    numberAvailableCourses++;
                }
            }

            totCourses += numberAvailableCourses;

            if (numberAvailableCourses != 0)
            {

                Entities
                    .WithoutBurst()
                    .ForEach((Entity e, int entityInQueryIndex, in UnitInitializerComponent uic, in LocalToWorld ltw) =>
                    {
                        for (int j = 0; j < UnitManager.Instance.NumEntitiesToSpawn; j++)
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
                            int selectedCourseId = availableCoursesIds[GenerateInt(numberAvailableCourses)];

                            Course selectedCourse = courses[selectedCourseId];
                            currentCourse = selectedCourse.Id;
                            float3 currentDest;
                            float3 firstDest = 0;

                            //add lectures to Schedule_Buffer
                            for (int k = 0; k < selectedCourse.Lectures.Count; k++)
                            {
                                currentDest = FindDestination("Aula" + selectedCourse.Lectures[k].Room);
                                if (k == 0)
                                    firstDest = currentDest;
                                sb.Add(new ScheduleBuffer
                                {
                                    destination = currentDest,
                                    duration = selectedCourse.Lectures[k].Duration
                                });
                            }

                            currentDest = FindExit("UscitaCastelidardo");
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

                            if (timeSlot == 1 && j == 0) //only the first entity in the first slot has covid to simulate what can be the infection
                                hasCovid = true;

                            CourseComponent courseComponent = new CourseComponent
                            {
                                id = selectedCourse.Id,
                                lectureStart = selectedCourse.LectureStart
                            };

                            if (selectedCourse.Id == 0)
                                courseComponent0++;

                            if (UnityEngine.Random.Range(0, 100) <= (UnitManager.Instance.ProbabilityOfWearingMask * 100))
                                wearMask = true;

                            PersonComponent personComponent = new PersonComponent
                            {
                                age = GenerateInt(19, 30),
                                sex = GenerateSex(),
                                hasCovid = hasCovid,
                                wearMask = wearMask
                            };

                            if (hasCovid)
                                unitMaterial = UnitManager.Instance.covidMoveMaterial;

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
        }
        bi_ECB.AddJobHandleForProducer(Dependency);

    }

    private int GenerateInt(int v1, int v2)
    {
        return UnityEngine.Random.Range(v1, v2);
    }
    private int GenerateInt(int v1)
    {
        return GenerateInt(0, v1);
    }
    private char GenerateSex()
    {
        int sex = GenerateInt(2);
        if (sex == 0)
            return 'M';
        else
            return 'F';
    }
    private List<Lecture> GenerateSchedule(out int lectureStart)
    {
        List<Lecture> schedule = new List<Lecture>();
        List<int> durationsForLectures = new List<int>();

        // a lecture can start in the last slot of a day
        lectureStart = GenerateInt(1, maxSlotsInSingleDay + 1);
        int lectureEnd = GenerateInt(lectureStart + 1, maxSlotsInSingleDay + 2);
        int maxScheduleSlotsDuration = lectureEnd - lectureStart; // even the last lecture can last 1 slot
        int singleDuration;

        while (maxScheduleSlotsDuration != 0)
        {
            singleDuration = GenerateInt(1, 3); //the number of slots for each lecture is between 1 and 2 (1.5 or 3 hours)
            if (maxScheduleSlotsDuration - singleDuration < 0)
            {
                durationsForLectures.Add(maxScheduleSlotsDuration);
                break;
            }
            else
            {
                durationsForLectures.Add(singleDuration);
                maxScheduleSlotsDuration -= singleDuration;
            }
        }

        int[] rooms = new int[durationsForLectures.Count];

        for (int i = 0; i < durationsForLectures.Count; i++)
        {
            rooms[i] = GenerateInt(1, numberOfRooms);

            if (i != 0)
                while (rooms[i] == rooms[i - 1])
                    rooms[i] = GenerateInt(1, numberOfRooms);
        }

        for (int i = 0; i < durationsForLectures.Count; i++)
        {
            schedule.Add(new Lecture(rooms[i], durationsForLectures[i]));
        }

        return schedule;
    }
    private float3 FindDestination(string roomName)
    {
        float3 destination = GameObject.Find(roomName).GetComponent<Renderer>().bounds.center;
        float3 dimension = GameObject.Find(roomName).GetComponent<Renderer>().bounds.size;

        destination.x += GenerateInt(-(int)dimension.x / 2, (int)dimension.x / 2);
        destination.y = 2f;
        destination.z += GenerateInt(-(int)dimension.z / 2, (int)dimension.z / 2);

        return destination;
    }
    private float3 FindExit(string exit)
    {
        float3 destination;
        destination = GameObject.Find(exit).GetComponent<Renderer>().bounds.center;
        destination.x += GenerateInt(-10, 13);
        destination.z += GenerateInt(-3, 4);
        destination.y = 2f;

        return destination;
    }
}
