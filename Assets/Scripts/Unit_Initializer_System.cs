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
            Debug.Log(count);
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

            //lessons = GenerateSchedule(out lessonStart);
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
                        ecb.AddComponent<Person_Component>(defEntity);
                        ecb.AddComponent<Course_Component>(defEntity);
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
                            myDestination.x += GenerateInt(-6, 7);
                            myDestination.y = 2f;
                            myDestination.z += GenerateInt(-6, 7);

                            sb.Add(new Schedule_Buffer
                            {
                                destination = myDestination,
                                duration = selectedCourse.Lessons[k].Duration
                            });
                        }

                        sb.Add(new Schedule_Buffer
                        {
                            destination = position,
                            //duration = 0
                        });


                        myDestination = GameObject.Find("Aula" + selectedCourse.Lessons[0].Room).GetComponent<Renderer>().bounds.center;
                        myDestination.x += GenerateInt(-6, 7);
                        myDestination.y = 2f;
                        myDestination.z += GenerateInt(-6, 7);
                        Unit_Component uc = new Unit_Component
                        {
                            fromLocation = position,
                            count = 0,
                            toLocation = myDestination,
                            currentBufferIndex = 0,
                            speed = GenerateInt(uic.minSpeed, uic.maxSpeed),
                            minDistanceReached = uic.minDistanceReached,
                            routed = false,
                            //course = currentCourse,
                            //id = entitiesId
                        };

                        //fill in data into components
                        Course_Component courseComponent = new Course_Component
                        {
                            id = selectedCourse.Id,
                            lessonStart = selectedCourse.LessonStart
                        };

                        Person_Component personComponent = new Person_Component
                        {
                            age = GenerateInt(19, 30),
                            sex = GenerateSex()
                        };

                        entitiesId++;

                        ecb.SetComponent(defEntity, uc);
                        ecb.SetComponent(defEntity, courseComponent);
                        ecb.SetComponent(defEntity, personComponent);

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
            case 10:
                return new int[] { 1, 1, 2, 1, 2 };
            case 11:
                return new int[] { 1, 1, 1, 1, 1, 2 };
            case 12:
                return new int[] { 2, 2, 3 };
            case 13:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            case 14:
                return new int[] { 1, 1, 1, 2 };
            case 15:
                return new int[] { 1, 1, 3, 1, 1 };
            case 16:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            case 17:
                return new int[] { 3, 3 };
            case 18:
                return new int[] { 1, 1, 2, 2 };
            case 19:
                return new int[] { 1, 1, 2, 1, 1 };
            case 20:
                return new int[] { 1, 1, 1, 1 };
            case 21:
                return new int[] { 1, 2, 2 };
            case 22:
                return new int[] { 1, 3, 1, 1, 1};
            case 23:
                return new int[] { 1, 1, 1, 1, 1, 1, 1 };
            case 24:
                return new int[] { 1, 1, 1, 1 };
            case 25:
                return new int[] { 1, 1, 1, 1, 2 };
            case 26:
                return new int[] { 1, 2, 2, 1, 1 };
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
                return new int[] { 0, 1, 9, 4 };
            case 1:
                return new int[] { 7, 1, 2, 4, 9, 4, 2 };
            case 2:
                return new int[] { 0, 0, 0, 5, 7 }; 
            case 3:
                return new int[] { 0, 2, 1, 3, 5, 6, 1 };
            case 4:
                return new int[] { 3, 5, 1, 3 };
            case 5:
                return new int[] { 0, 0, 5, 6, 7 };
            case 6:
                return new int[] { 1, 7, 3 };
            case 7:
                return new int[] { 0, 9 };
            case 8:
                return new int[] { 4, 8, 2, 6 };
            case 9:
                return new int[] { 0, 0, 0, 0, 8, 5, 4 };
            case 10:
                return new int[] { 7, 2, 3, 2, 9 };
            case 11:
                return new int[] { 0, 0, 8, 4, 6, 7 };
            case 12:
                return new int[] { 0, 0, 7 };
            case 13:
                return new int[] { 0, 9, 7, 3, 6, 2, 8 };
            case 14:
                return new int[] { 3, 6, 2, 4 };
            case 15:
                return new int[] { 0, 0, 0, 2, 7 };
            case 16:
                return new int[] { 0, 0, 0, 0, 1, 7, 3 };
            case 17:
                return new int[] { 0, 6 };
            case 18:
                return new int[] { 2, 5, 3, 2 };
            case 19:
                return new int[] { 0, 0, 8, 3, 4 }; 
            case 20:
                return new int[] { 0, 1, 9, 4 }; 
            case 21:
                return new int[] { 9, 4, 2 };
            case 22:
                return new int[] { 0, 0, 0, 3, 4 }; 
            case 23:
                return new int[] { 0, 0, 0, 0, 0, 0, 1 }; 
            case 24:
                return new int[] { 0, 0, 4, 3 }; 
            case 25:
                return new int[] { 0, 0, 2, 4, 1 }; 
            case 26:
                return new int[] { 1, 8, 3, 6, 2 }; 
            default:
                throw new System.ArgumentException();
        };
    }
    private List<Lesson> GenerateSchedule(out int lessonStart)
    {
        int numberOfRooms = 10;
        List<Lesson> schedule = new List<Lesson>();
        int numberOfLessons = GenerateInt(1, 8);

        Debug.Log(numberOfLessons);
        int[] rooms = new int[numberOfLessons];
        int[] durations = new int[numberOfLessons];
        int sum;

        lessonStart = GenerateInt(1, numberOfLessons);
        
        //generate 0 rooms for free lessons at the beginning
        for(int i = 0; i < lessonStart-1; i++)
        {
            rooms[i] = 0;
            durations[i] = 0;
        }

        //generate random rooms without two rooms being the same one after the other
        rooms[lessonStart-1] = GenerateInt(1, numberOfRooms);
        for (int i = lessonStart; i < numberOfLessons; i++)
        {
            do
            {
                rooms[i] = GenerateInt(1, numberOfRooms);
            }
            while (rooms[i] != rooms[i - 1]);
        }

        //generate random durations for the lessons considering that the max sum(lessonLenght) = 7
        do
        {
            sum = 0;
            for(int i = lessonStart-1; i < numberOfLessons; i++ )
            {
                durations[i] = GenerateInt(1, 3);
                sum += durations[i];
            }
        }
        while (sum <= (7 - lessonStart-1));


        for(int i = 0; i < numberOfLessons; i++)
        {
            schedule.Add(new Lesson(rooms[i], durations[i]));
        }

        return schedule;
    }
}
