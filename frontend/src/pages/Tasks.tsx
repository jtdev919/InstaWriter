import { useState } from "react";
import { useTasks, useCreateTask, useTransitionTask, useCompleteTask, useDeleteTask } from "../hooks/use-tasks";
import StatusBadge from "../components/ui/StatusBadge";
import { TASK_TRANSITIONS, type TaskItemStatus, type TaskPriority } from "../types";
import { Plus, Trash2, CheckCircle } from "lucide-react";
import { formatDistanceToNow, isPast } from "date-fns";

export default function Tasks() {
  const { data: tasks, isLoading } = useTasks();
  const createTask = useCreateTask();
  const transitionTask = useTransitionTask();
  const completeTask = useCompleteTask();
  const deleteTask = useDeleteTask();
  const [showForm, setShowForm] = useState(false);
  const [taskType, setTaskType] = useState("");
  const [description, setDescription] = useState("");
  const [owner, setOwner] = useState("");
  const [priority, setPriority] = useState<TaskPriority>("Medium");
  const [dueDate, setDueDate] = useState("");

  const handleCreate = () => {
    createTask.mutate({
      taskType, description: description || undefined, owner,
      priority, dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
    }, {
      onSuccess: () => { setShowForm(false); setTaskType(""); setDescription(""); setOwner(""); setDueDate(""); },
    });
  };

  const canComplete = (status: TaskItemStatus) => status === "InProgress" || status === "Overdue";

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Tasks</h1>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700"
        >
          <Plus size={16} /> New Task
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <div className="flex gap-3">
            <input
              value={taskType}
              onChange={(e) => setTaskType(e.target.value)}
              placeholder="Task type (e.g. RecordReel)"
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
            <input
              value={owner}
              onChange={(e) => setOwner(e.target.value)}
              placeholder="Owner"
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Description (optional)"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            rows={2}
          />
          <div className="flex gap-3">
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value as TaskPriority)}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Urgent">Urgent</option>
            </select>
            <input
              type="datetime-local"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
            <button
              onClick={handleCreate}
              disabled={!taskType || createTask.isPending}
              className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
            >
              {createTask.isPending ? "Creating..." : "Create"}
            </button>
          </div>
        </div>
      )}

      {isLoading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Description</th>
                <th className="px-4 py-3">Owner</th>
                <th className="px-4 py-3">Priority</th>
                <th className="px-4 py-3">Due</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {tasks?.map((task) => {
                const allowed = TASK_TRANSITIONS[task.status as TaskItemStatus] ?? [];
                const overdue = task.dueDate && isPast(new Date(task.dueDate)) && task.status !== "Completed" && task.status !== "Cancelled";
                return (
                  <tr key={task.id} className={`hover:bg-gray-50 ${overdue ? "bg-red-50" : ""}`}>
                    <td className="px-4 py-3 font-medium text-gray-900">{task.taskType}</td>
                    <td className="px-4 py-3 text-gray-500 max-w-xs truncate">{task.description ?? "-"}</td>
                    <td className="px-4 py-3 text-gray-500">{task.owner || "-"}</td>
                    <td className="px-4 py-3"><StatusBadge value={task.priority} /></td>
                    <td className="px-4 py-3 text-gray-500">
                      {task.dueDate
                        ? formatDistanceToNow(new Date(task.dueDate), { addSuffix: true })
                        : "-"}
                    </td>
                    <td className="px-4 py-3"><StatusBadge value={task.status} /></td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {canComplete(task.status as TaskItemStatus) && (
                          <button
                            onClick={() => completeTask.mutate(task.id)}
                            className="p-1 text-green-500 hover:text-green-700"
                            title="Complete"
                          >
                            <CheckCircle size={16} />
                          </button>
                        )}
                        {allowed.filter(s => s !== "Completed").map((s) => (
                          <button
                            key={s}
                            onClick={() => transitionTask.mutate({ id: task.id, status: s })}
                            className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 rounded"
                          >
                            {s}
                          </button>
                        ))}
                        <button
                          onClick={() => deleteTask.mutate(task.id)}
                          className="p-1 text-red-400 hover:text-red-600"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {tasks?.length === 0 && (
                <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-400">No tasks yet</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
