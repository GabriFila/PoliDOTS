using System.Collections.Generic;

namespace Assets.Scripts
{
    public struct Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Lecture> Lectures { get; set; }
        public int LectureStart { get; set; }
        public Course(int id, string name, List<Lecture> lessons, int lessonStart)
        {
            Id = id;
            Name = name;
            Lectures = lessons;
            LectureStart = lessonStart;
        }
    }

    public struct Lecture
    {
        public int Room { get; set; }
        public int Duration { get; set; }
        public Lecture(int room, int duration)
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
