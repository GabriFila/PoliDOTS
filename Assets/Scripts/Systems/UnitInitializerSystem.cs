using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts;
using Unity.Rendering;

public class UnitInitializerSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem bi_ECB;
    private DynamicBuffer<ScheduleBuffer> sb;
    public float elapsedTime;
    public int timeSlot;
    public List<Course> courses;
    public int currentCourse;
   
    public int latestSlot;
    public int numberOfCourses;
    
    public int numberOfRooms;
    public int lessonStart;
    public int maxDayHours;
    
    NativeArray<int> availableCourses;

    protected override void OnCreate()
    { 
        numberOfRooms = 11;
        maxDayHours = 7; 
        lessonStart = 0;
        timeSlot = 0;
        
        numberOfCourses = CourseName.GetValues(typeof(CourseName)).Length;
        courses = new List<Course>(); 
        latestSlot = 0;

        List<Lesson> lessons;
        for (int count = 0; count < numberOfCourses; count++)
        {   
            lessons = GenerateSchedule(out lessonStart);
            
            if (latestSlot < lessonStart)
                latestSlot = lessonStart;
            courses.Add(new Course(count, CourseName.GetValues(typeof(CourseName)).GetValue(count).ToString(), lessons, lessonStart));
           
            lessonStart = 0;
        }

        bi_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        elapsedTime = 0;
    }

    protected override void OnUpdate()
    {
        var ecb = bi_ECB.CreateCommandBuffer();
        elapsedTime += Time.DeltaTime;

        //if another slot has passed and there are still some courses beginning in a later slot i enter the lambda
        if (elapsedTime > UnitManager.instance.spawnEvery && timeSlot <= latestSlot)
        {
            elapsedTime = 0;
            timeSlot++;

            availableCourses = new NativeArray<int>(CourseName.GetValues(typeof(CourseName)).Length, Allocator.Temp);
            int numberAvailableCourses = 0;
            for (int k = 0; k < courses.Count; k++)
            {
                if (courses[k].LessonStart == timeSlot)
                {
                    availableCourses[numberAvailableCourses] = courses[k].Id;
                    numberAvailableCourses++;
                }
            }

            Entities
                .WithoutBurst()
                .ForEach((Entity e, int entityInQueryIndex, in UnitInitializerComponent uic, in LocalToWorld ltw) =>
                {
                    for (int j = 0; j < uic.numEntitiesToSpawn; j++)
                    {
                        Entity defEntity = ecb.Instantiate(uic.prefabToSpawn);
                        float3 position = new float3(UnityEngine.Random.Range(0, 36), uic.baseOffset, 0) + uic.currentPosition; //value 36 based on the spawner position (-28,0,-47)
                        bool covidValue = false;
                        Material unitMaterial = UnitManager.instance.activeMaterial;

                        ecb.SetComponent(defEntity, new Translation { Value = position });
                        ecb.AddComponent<UnitComponent>(defEntity);
                        ecb.AddComponent<PersonComponent>(defEntity);
                        ecb.AddComponent<CourseComponent>(defEntity);
                        ecb.AddBuffer<UnitBuffer>(defEntity);
                        sb = ecb.AddBuffer<ScheduleBuffer>(defEntity);

                        //select randomly a course from the available ones
                        int selectedCourseId = availableCourses[GenerateInt(numberAvailableCourses)];

                        Course selectedCourse = courses[selectedCourseId];
                        currentCourse = selectedCourse.Id;
                        float3 currentDest;

                        //add lessons to Schedule_Buffer
                        for (int k = 0; k < selectedCourse.Lessons.Count; k++)
                        {
                            currentDest = GameObject.Find("Aula" + selectedCourse.Lessons[k].Room).GetComponent<Renderer>().bounds.center;
                            currentDest.x += GenerateInt(-6, 7); //code to avoid all the entities to go toward to the same x point in room
                            currentDest.z += GenerateInt(-6, 7); //code to avoid all the entities to go toward to the same z point in room
                            currentDest.y = 2f;
                            
                            sb.Add(new ScheduleBuffer
                            {
                                destination = currentDest,
                                duration = selectedCourse.Lessons[k].Duration
                            });
                        }

                        sb.Add(new ScheduleBuffer
                        {
                            destination = position
                        });

                        currentDest = GameObject.Find("Aula" + selectedCourse.Lessons[0].Room).GetComponent<Renderer>().bounds.center;
                        currentDest.x += GenerateInt(-6, 7);
                        currentDest.z += GenerateInt(-6, 7);
                        currentDest.y = 2f;
                        
                        UnitComponent uc = new UnitComponent
                        {
                            fromLocation = position,
                            toLocation = currentDest,
                            speed = GenerateInt(uic.minSpeed, uic.maxSpeed),
                            minDistanceReached = uic.minDistanceReached,
                            count = 0,
                            currentBufferIndex = 0,
                            routed = false
                        };

                        if (timeSlot == 1 && j == 0) //only the first entity in the first slot has covid to simulate what can be the infection
                            covidValue = true;

                        CourseComponent courseComponent = new CourseComponent
                        {
                            id = selectedCourse.Id,
                            lessonStart = selectedCourse.LessonStart
                        };

                        PersonComponent personComponent = new PersonComponent
                        {
                            age = GenerateInt(19, 30),
                            sex = GenerateSex(),
                            hasCovid = covidValue
                        };

                        if (covidValue)
                            unitMaterial = UnitManager.instance.covdMaterial;

                        ecb.AddSharedComponent(e, new RenderMesh
                        {
                            mesh = UnitManager.instance.unitMesh,
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
    private List<Lesson> GenerateSchedule(out int lessonStart)
    {
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
