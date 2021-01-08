using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public static class ScheduleUtils
{
    public static List<Lecture> GenerateSchedule(int slotsInDay, int numberOfRooms, out int lectureStartTimeSlot)
    {
        List<Lecture> schedule = new List<Lecture>();
        List<int> durationsForLectures = new List<int>();

        // a lecture can start in the last slot of a day
        lectureStartTimeSlot = Utils.GenerateInt(0, slotsInDay);
        int lectureEndTimeSlot = Utils.GenerateInt(lectureStartTimeSlot + 1, slotsInDay + 1);
        int maxScheduleSlotsDuration = lectureEndTimeSlot - lectureStartTimeSlot; // even the last lecture can last 1 slot
        int singleDuration;

        while (maxScheduleSlotsDuration != 0)
        {
            singleDuration = Utils.GenerateInt(1, 3); //the number of slots for each lecture is between 1 and 2 (1.5 or 3 hours)
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
            rooms[i] = Utils.GenerateInt(1, numberOfRooms);

            if (i != 0)
                while (rooms[i] == rooms[i - 1])
                    rooms[i] = Utils.GenerateInt(1, numberOfRooms);
        }

        for (int i = 0; i < durationsForLectures.Count; i++)
        {
            schedule.Add(new Lecture(rooms[i], durationsForLectures[i]));
        }

        return schedule;
    }
}
