import { useState } from "react";
import { useContentDrafts, useCreateContentDraft, useTransitionContentDraft, useDeleteContentDraft, useRenderCarousel, useCarouselAssets } from "../hooks/use-content-drafts";
import { useContentIdeas } from "../hooks/use-content-ideas";
import StatusBadge from "../components/ui/StatusBadge";
import { DRAFT_TRANSITIONS, type ContentDraftStatus } from "../types";
import { Plus, Trash2, Image, Eye, X } from "lucide-react";

export default function ContentDrafts() {
  const { data: drafts, isLoading } = useContentDrafts();
  const { data: ideas } = useContentIdeas();
  const createDraft = useCreateContentDraft();
  const transition = useTransitionContentDraft();
  const deleteDraft = useDeleteContentDraft();
  const renderCarousel = useRenderCarousel();
  const [showForm, setShowForm] = useState(false);
  const [contentIdeaId, setContentIdeaId] = useState("");
  const [caption, setCaption] = useState("");
  const [previewDraftId, setPreviewDraftId] = useState<string | null>(null);
  const { data: carouselAssets } = useCarouselAssets(previewDraftId);

  const handleCreate = () => {
    createDraft.mutate({ contentIdeaId, caption }, {
      onSuccess: () => { setShowForm(false); setCaption(""); },
    });
  };

  const handleRenderCarousel = (draftId: string) => {
    renderCarousel.mutate(draftId, {
      onSuccess: () => setPreviewDraftId(draftId),
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Content Drafts</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> New Draft
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <select value={contentIdeaId} onChange={(e) => setContentIdeaId(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
            <option value="">Select an idea...</option>
            {ideas?.map((i) => <option key={i.id} value={i.id}>{i.title}</option>)}
          </select>
          <textarea value={caption} onChange={(e) => setCaption(e.target.value)} placeholder="Caption" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" rows={3} />
          <button onClick={handleCreate} disabled={!contentIdeaId || !caption || createDraft.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            {createDraft.isPending ? "Creating..." : "Create"}
          </button>
        </div>
      )}

      {/* Carousel Preview Panel */}
      {previewDraftId && (
        <div className="bg-white p-4 rounded-lg shadow mb-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold text-gray-900">Carousel Preview</h2>
            <button onClick={() => setPreviewDraftId(null)} className="p-1 text-gray-400 hover:text-gray-600"><X size={18} /></button>
          </div>
          {carouselAssets && carouselAssets.length > 0 ? (
            <div className="flex gap-3 overflow-x-auto pb-3">
              {carouselAssets.map((asset, i) => (
                <div key={asset.id} className="flex-shrink-0">
                  <img
                    src={asset.blobUri ?? ""}
                    alt={`Slide ${i + 1}`}
                    className="w-48 h-48 object-cover rounded-lg border border-gray-200 shadow-sm"
                  />
                  <p className="text-xs text-gray-500 text-center mt-1">Slide {i + 1}</p>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-400 text-sm">Loading slides...</p>
          )}
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">Idea</th>
                <th className="px-4 py-3">Caption</th>
                <th className="px-4 py-3">Version</th>
                <th className="px-4 py-3">Compliance</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {drafts?.map((d) => {
                const allowed = DRAFT_TRANSITIONS[d.status as ContentDraftStatus] ?? [];
                return (
                  <tr key={d.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-500">{d.contentIdea?.title ?? ideas?.find((i) => i.id === d.contentIdeaId)?.title ?? d.contentIdeaId.slice(0, 8)}</td>
                    <td className="px-4 py-3 max-w-md truncate">{d.caption}</td>
                    <td className="px-4 py-3 text-gray-500">v{d.versionNo}</td>
                    <td className="px-4 py-3">
                      {d.complianceScore != null ? (
                        <span className={`text-xs font-medium ${d.complianceScore >= 0.8 ? "text-green-600" : d.complianceScore >= 0.5 ? "text-yellow-600" : "text-red-600"}`}>
                          {(d.complianceScore * 100).toFixed(0)}%
                        </span>
                      ) : <span className="text-gray-400">-</span>}
                    </td>
                    <td className="px-4 py-3"><StatusBadge value={d.status} /></td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => handleRenderCarousel(d.id)}
                          disabled={renderCarousel.isPending}
                          className="flex items-center gap-1 px-2 py-1 text-xs bg-purple-100 text-purple-700 hover:bg-purple-200 rounded disabled:opacity-50"
                          title="Generate carousel slides"
                        >
                          <Image size={12} />
                          {renderCarousel.isPending && renderCarousel.variables === d.id ? "Rendering..." : "Carousel"}
                        </button>
                        <button
                          onClick={() => setPreviewDraftId(previewDraftId === d.id ? null : d.id)}
                          className="flex items-center gap-1 px-2 py-1 text-xs bg-blue-100 text-blue-700 hover:bg-blue-200 rounded"
                          title="Preview carousel"
                        >
                          <Eye size={12} /> Preview
                        </button>
                        {allowed.map((s) => (
                          <button key={s} onClick={() => transition.mutate({ id: d.id, status: s })} className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 rounded">{s}</button>
                        ))}
                        <button onClick={() => deleteDraft.mutate(d.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {drafts?.length === 0 && <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No drafts yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
