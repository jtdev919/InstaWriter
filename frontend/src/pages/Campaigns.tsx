import { useState } from "react";
import { useCampaigns, useCreateCampaign, useDeleteCampaign } from "../hooks/use-campaigns";
import StatusBadge from "../components/ui/StatusBadge";
import { Plus, Trash2 } from "lucide-react";

export default function Campaigns() {
  const { data: campaigns, isLoading } = useCampaigns();
  const createCampaign = useCreateCampaign();
  const deleteCampaign = useDeleteCampaign();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [objective, setObjective] = useState("");

  const handleCreate = () => {
    createCampaign.mutate({ name, objective }, {
      onSuccess: () => { setShowForm(false); setName(""); setObjective(""); },
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Campaigns</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> New Campaign
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Campaign name" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={objective} onChange={(e) => setObjective(e.target.value)} placeholder="Objective" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <button onClick={handleCreate} disabled={!name || createCampaign.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            {createCampaign.isPending ? "Creating..." : "Create"}
          </button>
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Objective</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {campaigns?.map((c) => (
                <tr key={c.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">{c.name}</td>
                  <td className="px-4 py-3 text-gray-500 max-w-xs truncate">{c.objective}</td>
                  <td className="px-4 py-3"><StatusBadge value={c.status} /></td>
                  <td className="px-4 py-3 text-gray-500">{new Date(c.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <button onClick={() => deleteCampaign.mutate(c.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))}
              {campaigns?.length === 0 && <tr><td colSpan={5} className="px-4 py-8 text-center text-gray-400">No campaigns yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
