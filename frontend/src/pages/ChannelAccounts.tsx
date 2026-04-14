import { useState } from "react";
import { useChannels, useCreateChannel, useDeleteChannel } from "../hooks/use-channels";
import StatusBadge from "../components/ui/StatusBadge";
import { Plus, Trash2 } from "lucide-react";

export default function ChannelAccounts() {
  const { data: channels, isLoading } = useChannels();
  const createChannel = useCreateChannel();
  const deleteChannel = useDeleteChannel();
  const [showForm, setShowForm] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [externalAccountId, setExternalAccountId] = useState("");

  const handleCreate = () => {
    createChannel.mutate({ accountName, externalAccountId: externalAccountId || undefined, platformType: "Instagram" }, {
      onSuccess: () => { setShowForm(false); setAccountName(""); setExternalAccountId(""); },
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Channel Accounts</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> Add Channel
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <input value={accountName} onChange={(e) => setAccountName(e.target.value)} placeholder="Account name (e.g. @mybrand)" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={externalAccountId} onChange={(e) => setExternalAccountId(e.target.value)} placeholder="Instagram User ID (optional)" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <button onClick={handleCreate} disabled={!accountName || createChannel.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            {createChannel.isPending ? "Creating..." : "Create"}
          </button>
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {channels?.map((ch) => (
            <div key={ch.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-semibold text-gray-900">{ch.accountName}</h3>
                  <p className="text-sm text-gray-500 mt-1">Platform: {ch.platformType}</p>
                  {ch.externalAccountId && <p className="text-xs text-gray-400 mt-1 font-mono">ID: {ch.externalAccountId}</p>}
                </div>
                <button onClick={() => deleteChannel.mutate(ch.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
              </div>
              <div className="mt-3 flex items-center gap-3">
                <StatusBadge value={ch.authStatus} />
                {ch.tokenExpiry && <span className="text-xs text-gray-400">Expires: {new Date(ch.tokenExpiry).toLocaleDateString()}</span>}
                {ch.isActive ? <span className="text-xs text-green-600">Active</span> : <span className="text-xs text-gray-400">Inactive</span>}
              </div>
            </div>
          ))}
          {channels?.length === 0 && <p className="text-gray-400 col-span-full text-center py-8">No channels connected</p>}
        </div>
      )}
    </div>
  );
}
