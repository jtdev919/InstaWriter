import { useContentIdeas } from "../hooks/use-content-ideas";
import { useTasks } from "../hooks/use-tasks";

export default function Dashboard() {
  const { data: ideas } = useContentIdeas();
  const { data: tasks } = useTasks();

  const stats = [
    { label: "Total Ideas", value: ideas?.length ?? 0, color: "bg-blue-500" },
    { label: "In Progress", value: ideas?.filter((i) => i.status === "InProgress").length ?? 0, color: "bg-yellow-500" },
    { label: "Published", value: ideas?.filter((i) => i.status === "Published").length ?? 0, color: "bg-green-500" },
    { label: "Pending Tasks", value: tasks?.filter((t) => t.status === "Pending").length ?? 0, color: "bg-orange-500" },
    { label: "Overdue Tasks", value: tasks?.filter((t) => t.status === "Overdue").length ?? 0, color: "bg-red-500" },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dashboard</h1>
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-8">
        {stats.map((s) => (
          <div key={s.label} className="bg-white rounded-lg shadow p-4">
            <div className={`w-2 h-2 rounded-full ${s.color} mb-2`} />
            <p className="text-2xl font-bold text-gray-900">{s.value}</p>
            <p className="text-sm text-gray-500">{s.label}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-4">
          <h2 className="font-semibold text-gray-900 mb-3">Recent Ideas</h2>
          {ideas?.slice(0, 5).map((idea) => (
            <div key={idea.id} className="flex justify-between items-center py-2 border-b border-gray-100 last:border-0">
              <span className="text-sm text-gray-700">{idea.title}</span>
              <span className={`text-xs px-2 py-0.5 rounded-full ${
                idea.status === "Published" ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-600"
              }`}>{idea.status}</span>
            </div>
          ))}
          {(!ideas || ideas.length === 0) && <p className="text-sm text-gray-400">No ideas yet</p>}
        </div>

        <div className="bg-white rounded-lg shadow p-4">
          <h2 className="font-semibold text-gray-900 mb-3">Active Tasks</h2>
          {tasks?.filter((t) => t.status !== "Completed" && t.status !== "Cancelled").slice(0, 5).map((task) => (
            <div key={task.id} className="flex justify-between items-center py-2 border-b border-gray-100 last:border-0">
              <span className="text-sm text-gray-700">{task.taskType}: {task.description?.slice(0, 40)}</span>
              <span className={`text-xs px-2 py-0.5 rounded-full ${
                task.status === "Overdue" ? "bg-red-100 text-red-800" : "bg-yellow-100 text-yellow-800"
              }`}>{task.status}</span>
            </div>
          ))}
          {(!tasks || tasks.filter((t) => t.status !== "Completed" && t.status !== "Cancelled").length === 0) && (
            <p className="text-sm text-gray-400">No active tasks</p>
          )}
        </div>
      </div>
    </div>
  );
}
