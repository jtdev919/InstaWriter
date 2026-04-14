import { usePublishJobs, useTransitionPublishJob, useDeletePublishJob } from "../hooks/use-publish-jobs";
import StatusBadge from "../components/ui/StatusBadge";
import { JOB_TRANSITIONS, type PublishJobStatus } from "../types";
import { Trash2 } from "lucide-react";

export default function PublishJobs() {
  const { data: jobs, isLoading } = usePublishJobs();
  const transition = useTransitionPublishJob();
  const deleteJob = useDeletePublishJob();

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Publish Jobs</h1>

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">Draft</th>
                <th className="px-4 py-3">Planned Date</th>
                <th className="px-4 py-3">Mode</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Media ID</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {jobs?.map((j) => {
                const allowed = JOB_TRANSITIONS[j.status as PublishJobStatus] ?? [];
                return (
                  <tr key={j.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-500">{j.contentDraft?.caption?.slice(0, 40) ?? j.contentDraftId.slice(0, 8)}</td>
                    <td className="px-4 py-3 text-gray-500">{j.plannedPublishDate ? new Date(j.plannedPublishDate).toLocaleDateString() : "-"}</td>
                    <td className="px-4 py-3"><StatusBadge value={j.publishMode} /></td>
                    <td className="px-4 py-3"><StatusBadge value={j.status} /></td>
                    <td className="px-4 py-3 text-gray-400 text-xs font-mono">{j.externalMediaId ?? "-"}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {allowed.map((s) => (
                          <button key={s} onClick={() => transition.mutate({ id: j.id, status: s })} className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 rounded">{s}</button>
                        ))}
                        <button onClick={() => deleteJob.mutate(j.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {jobs?.length === 0 && <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No publish jobs yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
