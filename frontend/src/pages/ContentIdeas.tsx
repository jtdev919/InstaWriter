import { useState } from "react";
import { useContentIdeas, useCreateContentIdea, useDeleteContentIdea, useTransitionContentIdea } from "../hooks/use-content-ideas";
import StatusBadge from "../components/ui/StatusBadge";
import { IDEA_TRANSITIONS, type ContentIdeaStatus, type ContentRiskLevel } from "../types";
import { Plus, Trash2 } from "lucide-react";

export default function ContentIdeas() {
  const { data: ideas, isLoading } = useContentIdeas();
  const createIdea = useCreateContentIdea();
  const deleteIdea = useDeleteContentIdea();
  const transition = useTransitionContentIdea();
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState("");
  const [summary, setSummary] = useState("");
  const [riskLevel, setRiskLevel] = useState<ContentRiskLevel>("Low");
  const [pillarName, setPillarName] = useState("");

  const handleCreate = () => {
    createIdea.mutate({ title, summary: summary || undefined, riskLevel, pillarName: pillarName || undefined }, {
      onSuccess: () => { setShowForm(false); setTitle(""); setSummary(""); setPillarName(""); },
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Content Ideas</h1>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700"
        >
          <Plus size={16} /> New Idea
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Idea title"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
          <textarea
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            placeholder="Summary (optional)"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            rows={2}
          />
          <div className="flex gap-3">
            <select
              value={riskLevel}
              onChange={(e) => setRiskLevel(e.target.value as ContentRiskLevel)}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="Low">Low Risk</option>
              <option value="Medium">Medium Risk</option>
              <option value="High">High Risk</option>
            </select>
            <input
              value={pillarName}
              onChange={(e) => setPillarName(e.target.value)}
              placeholder="Pillar name (optional)"
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
            <button
              onClick={handleCreate}
              disabled={!title || createIdea.isPending}
              className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
            >
              {createIdea.isPending ? "Creating..." : "Create"}
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
                <th className="px-4 py-3">Title</th>
                <th className="px-4 py-3">Pillar</th>
                <th className="px-4 py-3">Risk</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {ideas?.map((idea) => {
                const allowed = IDEA_TRANSITIONS[idea.status as ContentIdeaStatus] ?? [];
                return (
                  <tr key={idea.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-900">{idea.title}</td>
                    <td className="px-4 py-3 text-gray-500">{idea.pillarName ?? "-"}</td>
                    <td className="px-4 py-3"><StatusBadge value={idea.riskLevel} /></td>
                    <td className="px-4 py-3"><StatusBadge value={idea.status} /></td>
                    <td className="px-4 py-3 text-gray-500">{new Date(idea.createdAt).toLocaleDateString()}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {allowed.map((s) => (
                          <button
                            key={s}
                            onClick={() => transition.mutate({ id: idea.id, status: s })}
                            className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 rounded"
                          >
                            {s}
                          </button>
                        ))}
                        <button
                          onClick={() => deleteIdea.mutate(idea.id)}
                          className="p-1 text-red-400 hover:text-red-600"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {ideas?.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No ideas yet</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
