using System.Collections.Generic;
using Unity.Mathematics;

namespace Assets.Scripts
{
    public struct Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Lesson> Lessons { get; set; }
        public int LessonStart { get; set; }
        public Course(int id, string name, List<Lesson> lessons, int lessonStart)
        {
            Id = id;
            Name = name;
            Lessons = lessons;
            LessonStart = lessonStart;
        }
    }

    public struct Lesson
    {
        public int Room { get; set; }
        public int Duration { get; set; }
        public Lesson(int room, int duration)
        {
            Room = room;
            Duration = duration;
        }
    }

    public enum CourseName
    {
        Computer,
        Informatica,
        Nuclear,
        Automotive,
        Data_Science,
        Civil,
        Civile,
        Building,
        Edile,
        Electronic,
        Elettronica,
        Mechanical,
        Meccanica,
        Mechatronic,
        Physics,
        Fisica,
        Ict,
        Biomedical,
        Biomedica,
        Aereospaziale,
        Materials,
        Cinema,
        Gestionale,
        Management,
        Ambientale,
        Nanotechnologies,
        Nanotecnologie
    }
}