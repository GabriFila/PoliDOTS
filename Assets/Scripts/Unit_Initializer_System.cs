using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts;

public class Unit_Initializer_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    private DynamicBuffer<Schedule_Buffer> sb;
    public float elapsedTime;
    public int timeSlot;
    public List<Course> courses;
    public int numberOfCourses;
    //used to keep track of the last slot entities can be spawned
    public int latestSlot;
    public int currentCourse;
    public int entitiesId;

    protected override void OnCreate()
    {
        timeSlot = 0;
        entitiesId = 0;
        numberOfCourses = CourseName.GetValues(typeof(CourseName)).Length;
        courses = new List<Course>();
        List<Lesson> lessons;

        int[] schedule;
        int[] durations;
        int lessonStart = 0;
        latestSlot = 0;

        for (int count = 0; count < numberOfCourses; count++)
        {
            lessons = new List<Lesson>();
            schedule = GenerateTimeTable(count);
            durations = GenerateDuration(count);

            for (int j = 0; j < schedule.Length; j++)
            {
                if (schedule[j] == 0)
                {
                    lessonStart++;
                    continue;
                }

                lessons.Add(new Lesson(schedule[j], durations[j]));
            }

            //UnityEngine.Debug.Log("Course " + count + " starts in the slot " + lessonStart);

            if (latestSlot < lessonStart)
                latestSlot = lessonStart;
            courses.Add(new Course(count, CourseName.GetValues(typeof(CourseName)).GetValue(count).ToString(), lessons, lessonStart));
            
            //UnityEngine.Debug.Log(courses[count].Name + " " + courses[count].Id + " " + courses[count].Lessons.Count + " " + courses[count].LessonStart);
            
            lessonStart = 0;
        }

        //UnityEngine.Debug.Log("Latest slot is " + latestSlot);

        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer();

        //courses that are available for the current timeslot in which the entity is created
        NativeArray<int> availableCourses;

        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        elapsedTime += Time.DeltaTime;

        //if another slot has passed and there are still some courses beginning in a later slot i enter the lambda
        if (elapsedTime > UnitManager.instance.spawnEvery && timeSlot <= latestSlot)
        {
            elapsedTime = 0;
            timeSlot++;
            Entities
                .WithoutBurst()
                .ForEach((Entity e, int entityInQueryIndex, in Unit_Initializer_Component uic, in LocalToWorld ltw) =>
                {
                    //var random = randomArray[entityInQueryIndex];

                    for (int j = 0; j < uic.numEntitiesToSpawn; j++)
                    {
                        Entity defEntity = ecb.Instantiate(uic.prefabToSpawn);
                        float3 position = new float3(UnityEngine.Random.Range(0, 40), uic.baseOffset, 0) + uic.currentPosition;

                        ecb.SetComponent(defEntity, new Translation { Value = position });
                        ecb.AddComponent<Unit_Component>(defEntity);
                        ecb.AddBuffer<Unit_Buffer>(defEntity);
                        sb = ecb.AddBuffer<Schedule_Buffer>(defEntity);

                        //find courses that begin at current timeslot => cicle over non-managed courses and find the ones with lessonStart = timeSlot
                        availableCourses = new NativeArray<int>(numberOfCourses, Allocator.Temp);
                        int numberAvailableCourses = 0;
                        for (int k = 0; k < courses.Count; k++)
                        {
                            if (courses[k].LessonStart == timeSlot-1)
                            {
                                availableCourses[numberAvailableCourses] = courses[k].Id;
                                //UnityEngine.Debug.Log(courses[k].Id);
                                numberAvailableCourses++;
                            }
                        }

                        //UnityEngine.Debug.Log(numberAvailableCourses);

                        //select randomly a course from the available ones
                        int selectedCourseId = availableCourses[GenerateInt(numberAvailableCourses)];
                        Course selectedCourse = courses[selectedCourseId];
                        currentCourse = selectedCourse.Id;

                        float3 myDestination;

                        //add lessons to Schedule_Buffer
                       
                        for (int k = 0; k < selectedCourse.Lessons.Count; k++)
                        {
                            myDestination = GameObject.Find("Aula" + selectedCourse.Lessons[k].Room).GetComponent<Renderer>().bounds.center;
                            myDestination.y = 2f;
                            sb.Add(new Schedule_Buffer
                            {
                                destination = myDestination,
                                duration = selectedCourse.Lessons[k].Duration
                            });
                        }
                        myDestination = GameObject.Find("Aula" + selectedCourse.Lessons[0].Room).GetComponent<Renderer>().bounds.center;
                        myDestination.y = 2f;
                        Unit_Component uc = new Unit_Component
                        {
                            fromLocation = position,
                            count = 0,
                            toLocation = myDestination,
                            currentBufferIndex = 0,
                            speed = (float)new Unity.Mathematics.Random(uic.seed).NextDouble(uic.minSpeed, uic.maxSpeed),
                            minDistanceReached = uic.minDistanceReached,
                            routed = false,
                            course = currentCourse,
                            id = entitiesId
                        };

                        entitiesId++;

                        ecb.SetComponent(defEntity, uc);
                        availableCourses.Dispose();
                    }
                }).Run();
        }
        bi_ECB.AddJobHandleForProducer(Dependency);

    }

    protected override void OnDestroy()
    {
        //courses.Dispose();
    }
    private int GenerateInt(int i)
    {
        return UnityEngine.Random.Range(0, i);
    }
    private int[] GenerateDuration(int courseID)
    {
        switch (courseID)
        {
            case 0:
                return new int[] { 1, 1, 2, 1 };
            case 1:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            case 2:
                return new int[] { 1, 1, 1, 2, 2 };
            case 3:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            case 4:
                return new int[] { 2, 2, 1, 2 };
            case 5:
                return new int[] { 1, 1, 1, 1, 2 };
            case 6:
                return new int[] { 1, 2, 3 };
            case 7:
                return new int[] { 1, 1 };
            case 8:
                return new int[] { 1, 2, 2, 1 };
            case 9:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            default:
                throw new System.ArgumentException();
        };
    }

    private int[] GenerateTimeTable(int courseID)
    {
        //generate random number between 0 and MAX_ROOM_NUMBER
        //0 -> no lesson 
        //x -> roomNumber
        switch (courseID)
        {
            case 0:
                return new int[] { 0, 1, 9, 4 }; //2
            case 1:
                return new int[] { 7, 1, 2, 4, 9, 4, 2 }; //1
            case 2:
                return new int[] { 0, 0, 0, 5, 7 }; //4
            case 3:
                return new int[] { 0, 2, 1, 3, 5, 6, 1 }; //2
            case 4:
                return new int[] { 3, 5, 1, 3 }; //1
            case 5:
                return new int[] { 0, 0, 5, 6, 7 }; //3
            case 6:
                return new int[] { 1, 7, 3 }; //1
            case 7:
                return new int[] { 0, 9 }; //2
            case 8:
                return new int[] { 4, 8, 2, 6 }; //1
            case 9:
                return new int[] { 0, 0, 0, 0, 8, 5, 4 }; //5
            default:
                throw new System.ArgumentException();
        };
    }

}
