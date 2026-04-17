import { useQuery } from "@tanstack/react-query";
import { api } from "../api/client";
import type { CalendarEvent, TaskItem } from "../types";
import { format, startOfMonth, endOfMonth, eachDayOfInterval, isSameDay, isToday } from "date-fns";
import { useState } from "react";
import { ChevronLeft, ChevronRight, X, Clock, CheckCircle2 } from "lucide-react";

export default function CalendarView() {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [selectedDay, setSelectedDay] = useState<Date | null>(null);
  const { data: events } = useQuery({ queryKey: ["calendar-events"], queryFn: () => api.get<CalendarEvent[]>("/calendar-events") });
  const { data: tasks } = useQuery({ queryKey: ["tasks"], queryFn: () => api.get<TaskItem[]>("/tasks") });

  const start = startOfMonth(currentMonth);
  const end = endOfMonth(currentMonth);
  const days = eachDayOfInterval({ start, end });

  const startDay = start.getDay();
  const padBefore = (startDay === 0 ? 6 : startDay - 1);

  const prevMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1));
  const nextMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1));

  const getEventsForDay = (day: Date) => events?.filter((e) => isSameDay(new Date(e.startDateTime), day)) ?? [];
  const getTasksForDay = (day: Date) => tasks?.filter((t) => t.dueDate && isSameDay(new Date(t.dueDate), day)) ?? [];

  const selectedEvents = selectedDay ? getEventsForDay(selectedDay) : [];
  const selectedTasks = selectedDay ? getTasksForDay(selectedDay) : [];

  const getTaskForEvent = (event: CalendarEvent) => tasks?.find(t => t.id === event.taskItemId);

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

      <div className="grid grid-cols-3 gap-6">
        {/* Calendar grid */}
        <div className="col-span-2 bg-white rounded-lg shadow">
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
              const hasItems = dayEvents.length > 0 || dayTasks.length > 0;
              const isSelected = selectedDay && isSameDay(day, selectedDay);
              return (
                <div
                  key={day.toISOString()}
                  onClick={() => setSelectedDay(day)}
                  className={`h-24 border-b border-r border-gray-100 p-1 cursor-pointer hover:bg-gray-50 ${isToday(day) ? "bg-indigo-50" : ""} ${isSelected ? "ring-2 ring-indigo-400 ring-inset" : ""}`}
                >
                  <div className="flex items-center justify-between">
                    <span className={`text-xs font-medium ${isToday(day) ? "text-indigo-600" : "text-gray-500"}`}>{format(day, "d")}</span>
                    {hasItems && <span className="w-1.5 h-1.5 rounded-full bg-purple-500" />}
                  </div>
                  {dayEvents.slice(0, 2).map((e) => {
                    const task = getTaskForEvent(e);
                    const label = task?.description?.split(":")[0] ?? format(new Date(e.startDateTime), "HH:mm");
                    return (
                      <div key={e.id} className="text-[10px] bg-purple-100 text-purple-700 rounded px-1 mt-0.5 truncate">
                        {format(new Date(e.startDateTime), "h:mma")} {label}
                      </div>
                    );
                  })}
                  {dayEvents.length > 2 && (
                    <div className="text-[10px] text-gray-400 px-1">+{dayEvents.length - 2} more</div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* Day detail panel */}
        <div className="bg-white rounded-lg shadow p-4">
          {selectedDay ? (
            <>
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">{format(selectedDay, "EEEE, MMM d")}</h2>
                <button onClick={() => setSelectedDay(null)} className="p-1 text-gray-400 hover:text-gray-600"><X size={16} /></button>
              </div>

              {selectedEvents.length === 0 && selectedTasks.length === 0 ? (
                <p className="text-sm text-gray-400">No events scheduled</p>
              ) : (
                <div className="space-y-3">
                  {selectedEvents.map((e) => {
                    const task = getTaskForEvent(e);
                    const parts = task?.description?.split(": ") ?? [];
                    const title = parts[0] ?? "Event";
                    const desc = parts.slice(1).join(": ") ?? "";
                    const isCompleted = task?.status === "Completed";
                    return (
                      <div key={e.id} className={`border rounded-lg p-3 ${isCompleted ? "border-green-200 bg-green-50" : "border-purple-200 bg-purple-50"}`}>
                        <div className="flex items-center gap-2 mb-1">
                          {isCompleted ? (
                            <CheckCircle2 size={14} className="text-green-600" />
                          ) : (
                            <Clock size={14} className="text-purple-600" />
                          )}
                          <span className={`text-sm font-semibold ${isCompleted ? "text-green-700 line-through" : "text-purple-700"}`}>{title}</span>
                        </div>
                        <div className="text-xs text-gray-600 mb-1">
                          {format(new Date(e.startDateTime), "h:mm a")} - {format(new Date(e.endDateTime), "h:mm a")}
                          {e.reminderProfile && <span className="ml-2 text-gray-400">({e.reminderProfile})</span>}
                        </div>
                        {desc && <p className="text-xs text-gray-500 mt-1">{desc}</p>}
                        {task && (
                          <div className="flex items-center gap-2 mt-2">
                            <span className={`text-[10px] px-1.5 py-0.5 rounded ${
                              task.priority === "High" || task.priority === "Urgent" ? "bg-red-100 text-red-700" :
                              task.priority === "Medium" ? "bg-yellow-100 text-yellow-700" :
                              "bg-gray-100 text-gray-600"
                            }`}>{task.priority}</span>
                            <span className={`text-[10px] px-1.5 py-0.5 rounded ${
                              task.status === "Completed" ? "bg-green-100 text-green-700" :
                              task.status === "Overdue" ? "bg-red-100 text-red-700" :
                              "bg-blue-100 text-blue-700"
                            }`}>{task.status}</span>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </>
          ) : (
            <div className="text-center text-gray-400 py-8">
              <p className="text-sm">Click a day to see details</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
