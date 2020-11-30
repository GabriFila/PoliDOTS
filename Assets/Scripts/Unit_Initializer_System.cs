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

    public int lessonStart;
    public int maxDayHours;

    protected override void OnCreate()
    {
        maxDayHours = 7;
        lessonStart = 0;

        timeSlot = 0;
        entitiesId = 0;
        numberOfCourses = CourseName.GetValues(typeof(CourseName)).Length;
        courses = new List<Course>();
        List<Lesson> lessons;

        int[] schedule;
        int[] durations;
        latestSlot = 0;

        for (int count = 0; count < numberOfCourses; count++)
        {
            //lessons = new List<Lesson>();
            //schedule = GenerateTimeTable(count);
            //durations = GenerateDuration(count);

            /*
            for (int j = 0; j < schedule.Length; j++)
            {
                if (schedule[j] == 0)
                {
                    lessonStart++;
                    continue;
                }

                lessons.Add(new Lesson(schedule[j], durations[j]));
            }
            */

            //lessons = new List<Lesson>();
            
            lessons = GenerateSchedule(out lessonStart);
            
            if (latestSlot < lessonStart)
                latestSlot = lessonStart;
            courses.Add(new Course(count, CourseName.GetValues(typeof(CourseName)).GetValue(count).ToString(), lessons, lessonStart));
            
            UnityEngine.Debug.Log(courses[count].Name + ", id: " + count + ", number of lessons: " + courses[count].Lessons.Count + ", lesson start at: " + courses[count].LessonStart);
            for(int k=0; k < courses[count].Lessons.Count ; k++)
            {
                UnityEngine.Debug.Log("Lesson " + k + " room: " + courses[count].Lessons[k].Room + ", duration: " + courses[count].Lessons[k].Duration);
            }
            //UnityEngine.Debug.Log(count);
            
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

        //var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

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
                    //List<Course> myCourses = courses;

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

                        availableCourses = new NativeArray<int>(CourseName.GetValues(typeof(CourseName)).Length, Allocator.Temp);
                        int numberAvailableCourses = 0;
                        for (int k = 0; k < courses.Count; k++)
                        {
                            if (courses[k].LessonStart == timeSlot)
                            {
                                availableCourses[numberAvailableCourses] = courses[k].Id;
                                //UnityEngine.Debug.Log(courses[k].Id);
                                numberAvailableCourses++;
                            }
                        }

                        //select randomly a course from the available ones
                        int selectedCourseId = availableCourses[GenerateInt(numberAvailableCourses)];

                        //UnityEngine.Debug.Log("Selected index " + selectedCourseId);

                        Course selectedCourse = courses[selectedCourseId];
                        currentCourse = selectedCourse.Id;

                        //UnityEngine.Debug.Log("Selected course id " + currentCourse);

                        float3 myDestination;

                        //add lessons to Schedule_Buffer

                        UnityEngine.Debug.Log("Selected course # of lessons " + selectedCourse.Lessons.Count);
                       
                        for (int k = 0; k < selectedCourse.Lessons.Count; k++)
                        {
                            UnityEngine.Debug.Log("Selected course room " + selectedCourse.Lessons[k].Room + " ,duration " + selectedCourse.Lessons[k].Duration);

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
                            course = currentCourse,
                            id = entitiesId
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
        //UnityEngine.Debug.Log("Enter in generate schedule");

        int numberOfRooms = 10;
        List<Lesson> schedule = new List<Lesson>();
        List<int> durationsForLessons = new List<int>();

        lessonStart = GenerateInt(1, maxDayHours);
        int maxSlots = maxDayHours - lessonStart;
        int singleDuration;

        while (maxSlots != 0)
        {
            singleDuration = GenerateInt(1, 3); //the number of slots for each lecture is between 1 and 2 (1.5 or 3 hours)
            if (maxSlots - singleDuration < 0)
            {
                durationsForLessons.Add(maxSlots);
                maxSlots = 0;
            }
            else
            {
                durationsForLessons.Add(singleDuration);
                maxSlots -= singleDuration;
            }
        }

        int[] rooms = new int[durationsForLessons.Count];

        for (int i = 0; i < durationsForLessons.Count; i++)
        {
            rooms[i] = GenerateInt(1, numberOfRooms);

            if (i != 0)
                while (rooms[i] == rooms[i - 1])
                    rooms[i] = GenerateInt(1, numberOfRooms);
        }

        for (int i = 0; i < durationsForLessons.Count; i++)
        {
            schedule.Add(new Lesson(rooms[i], durationsForLessons[i]));
        }

        return schedule;
    }
}
