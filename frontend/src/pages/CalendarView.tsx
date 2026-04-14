import { useQuery } from "@tanstack/react-query";
import { api } from "../api/client";
import type { CalendarEvent, TaskItem } from "../types";
import { format, startOfMonth, endOfMonth, eachDayOfInterval, isSameDay, isToday } from "date-fns";
import { useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";

export default function CalendarView() {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const { data: events } = useQuery({ queryKey: ["calendar-events"], queryFn: () => api.get<CalendarEvent[]>("/calendar-events") });
  const { data: tasks } = useQuery({ queryKey: ["tasks"], queryFn: () => api.get<TaskItem[]>("/tasks") });

  const start = startOfMonth(currentMonth);
  const end = endOfMonth(currentMonth);
  const days = eachDayOfInterval({ start, end });

  // Pad start to Monday
  const startDay = start.getDay();
  const padBefore = (startDay === 0 ? 6 : startDay - 1);

  const prevMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1));
  const nextMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1));

  const getEventsForDay = (day: Date) => events?.filter((e) => isSameDay(new Date(e.startDateTime), day)) ?? [];
  const getTasksForDay = (day: Date) => tasks?.filter((t) => t.dueDate && isSameDay(new Date(t.dueDate), day)) ?? [];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Calendar</h1>
        <div className="flex items-center gap-3">
          <button onClick={prevMonth} className="p-1 hover:bg-gray-200 rounded"><ChevronLeft size={20} /></button>
          <span className="font-semibold text-gray-700">{format(currentMonth, "MMMM yyyy")}</span>
          <button onClick={nextMonth} className="p-1 hover:bg-gray-200 rounded"><ChevronRight size={20} /></button>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="grid grid-cols-7 text-center text-xs font-semibold text-gray-500 border-b">
          {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((d) => (
            <div key={d} className="py-2">{d}</div>
          ))}
        </div>
        <div className="grid grid-cols-7">
          {Array.from({ length: padBefore }).map((_, i) => (
            <div key={`pad-${i}`} className="h-24 border-b border-r border-gray-100" />
          ))}
          {days.map((day) => {
            const dayEvents = getEventsForDay(day);
            const dayTasks = getTasksForDay(day);
            return (
              <div key={day.toISOString()} className={`h-24 border-b border-r border-gray-100 p-1 ${isToday(day) ? "bg-indigo-50" : ""}`}>
                <span className={`text-xs font-medium ${isToday(day) ? "text-indigo-600" : "text-gray-500"}`}>{format(day, "d")}</span>
                {dayEvents.map((e) => (
                  <div key={e.id} className="text-xs bg-blue-100 text-blue-700 rounded px-1 mt-0.5 truncate">
                    {format(new Date(e.startDateTime), "HH:mm")}
                  </div>
                ))}
                {dayTasks.map((t) => (
                  <div key={t.id} className={`text-xs rounded px-1 mt-0.5 truncate ${t.status === "Overdue" ? "bg-red-100 text-red-700" : "bg-yellow-100 text-yellow-700"}`}>
                    {t.taskType}
                  </div>
                ))}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
