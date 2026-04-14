import { useState } from "react";
import { useContentPillars, useCreateContentPillar, useDeleteContentPillar } from "../hooks/use-content-pillars";
import { Plus, Trash2 } from "lucide-react";

export default function ContentPillars() {
  const { data: pillars, isLoading } = useContentPillars();
  const createPillar = useCreateContentPillar();
  const deletePillar = useDeleteContentPillar();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [priorityWeight, setPriorityWeight] = useState("1.0");

  const handleCreate = () => {
    createPillar.mutate({ name, description: description || undefined, priorityWeight: parseFloat(priorityWeight) }, {
      onSuccess: () => { setShowForm(false); setName(""); setDescription(""); setPriorityWeight("1.0"); },
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Content Pillars</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> New Pillar
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Pillar name" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Description (optional)" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <div className="flex gap-3 items-center">
            <label className="text-sm text-gray-600">Priority Weight:</label>
            <input type="number" step="0.1" min="0.1" value={priorityWeight} onChange={(e) => setPriorityWeight(e.target.value)} className="w-24 px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            <button onClick={handleCreate} disabled={!name || createPillar.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
              {createPillar.isPending ? "Creating..." : "Create"}
            </button>
          </div>
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {pillars?.map((p) => (
            <div key={p.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-semibold text-gray-900">{p.name}</h3>
                  {p.description && <p className="text-sm text-gray-500 mt-1">{p.description}</p>}
                </div>
                <button onClick={() => deletePillar.mutate(p.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
              </div>
              <div className="mt-3 flex items-center gap-2">
                <div className="flex-1 bg-gray-200 rounded-full h-2">
                  <div className="bg-indigo-500 h-2 rounded-full" style={{ width: `${Math.min(p.priorityWeight * 20, 100)}%` }} />
                </div>
                <span className="text-sm font-medium text-gray-600">{p.priorityWeight}</span>
              </div>
            </div>
          ))}
          {pillars?.length === 0 && <p className="text-gray-400 col-span-full text-center py-8">No pillars yet</p>}
        </div>
      )}
    </div>
  );
}
