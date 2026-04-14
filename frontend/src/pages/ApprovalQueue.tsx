import { useApprovals, useCreateApproval } from "../hooks/use-approvals";
import { useContentDrafts } from "../hooks/use-content-drafts";
import StatusBadge from "../components/ui/StatusBadge";
import { CheckCircle, XCircle } from "lucide-react";

export default function ApprovalQueue() {
  const { data: approvals, isLoading } = useApprovals();
  const { data: drafts } = useContentDrafts();
  const createApproval = useCreateApproval();

  const pendingApprovals = approvals?.filter((a) => a.decision === "Pending") ?? [];

  const handleDecision = (draftId: string, decision: "Approved" | "Rejected") => {
    createApproval.mutate({ contentDraftId: draftId, approver: "admin", decision, comments: `${decision} via dashboard` });
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Approval Queue</h1>

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <>
          <h2 className="text-lg font-semibold text-gray-700 mb-3">Pending ({pendingApprovals.length})</h2>
          {pendingApprovals.length === 0 ? (
            <div className="bg-white rounded-lg shadow p-8 text-center text-gray-400">No pending approvals</div>
          ) : (
            <div className="space-y-3 mb-8">
              {pendingApprovals.map((a) => {
                const draft = drafts?.find((d) => d.id === a.contentDraftId);
                return (
                  <div key={a.id} className="bg-white rounded-lg shadow p-4 flex items-start justify-between">
                    <div className="flex-1">
                      <p className="font-medium text-gray-900">{draft?.contentIdea?.title ?? "Draft"}</p>
                      <p className="text-sm text-gray-500 mt-1 line-clamp-2">{draft?.caption ?? "No caption"}</p>
                      {draft?.complianceScore != null && (
                        <p className="text-xs mt-1">
                          Compliance: <span className={draft.complianceScore >= 0.8 ? "text-green-600" : draft.complianceScore >= 0.5 ? "text-yellow-600" : "text-red-600"}>
                            {(draft.complianceScore * 100).toFixed(0)}%
                          </span>
                        </p>
                      )}
                      {a.comments && <p className="text-xs text-gray-400 mt-1">{a.comments}</p>}
                    </div>
                    <div className="flex gap-2 ml-4">
                      <button onClick={() => handleDecision(a.contentDraftId, "Approved")} className="flex items-center gap-1 px-3 py-1.5 bg-green-50 text-green-700 text-sm rounded-lg hover:bg-green-100">
                        <CheckCircle size={14} /> Approve
                      </button>
                      <button onClick={() => handleDecision(a.contentDraftId, "Rejected")} className="flex items-center gap-1 px-3 py-1.5 bg-red-50 text-red-700 text-sm rounded-lg hover:bg-red-100">
                        <XCircle size={14} /> Reject
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          <h2 className="text-lg font-semibold text-gray-700 mb-3">History</h2>
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3">Draft</th>
                  <th className="px-4 py-3">Approver</th>
                  <th className="px-4 py-3">Decision</th>
                  <th className="px-4 py-3">Comments</th>
                  <th className="px-4 py-3">Date</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {approvals?.filter((a) => a.decision !== "Pending").map((a) => (
                  <tr key={a.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">{drafts?.find((d) => d.id === a.contentDraftId)?.caption?.slice(0, 40) ?? a.contentDraftId.slice(0, 8)}</td>
                    <td className="px-4 py-3 text-gray-500">{a.approver}</td>
                    <td className="px-4 py-3"><StatusBadge value={a.decision} /></td>
                    <td className="px-4 py-3 text-gray-500 max-w-xs truncate">{a.comments ?? "-"}</td>
                    <td className="px-4 py-3 text-gray-500">{new Date(a.timestamp).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
