import { useState } from "react";
import { useContentBriefs, useCreateContentBrief, useDeleteContentBrief } from "../hooks/use-content-briefs";
import { useContentIdeas } from "../hooks/use-content-ideas";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import StatusBadge from "../components/ui/StatusBadge";
import type { ContentFormat } from "../types";
import { Plus, Trash2, Image } from "lucide-react";

interface RenderResult { briefId: string; slideCount: number; assetIds: string[]; message: string; }

export default function ContentBriefs() {
  const { data: briefs, isLoading } = useContentBriefs();
  const { data: ideas } = useContentIdeas();
  const qc = useQueryClient();
  const createBrief = useCreateContentBrief();
  const deleteBrief = useDeleteContentBrief();
  const renderCarousel = useMutation({
    mutationFn: (id: string) => api.post<RenderResult>(`/content/briefs/${id}/render-carousel`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
  const [showForm, setShowForm] = useState(false);
  const [contentIdeaId, setContentIdeaId] = useState("");
  const [targetFormat, setTargetFormat] = useState<ContentFormat>("StaticImage");
  const [objective, setObjective] = useState("");
  const [keyMessage, setKeyMessage] = useState("");
  const [requiresOriginalMedia, setRequiresOriginalMedia] = useState(false);
  const [renderResult, setRenderResult] = useState<RenderResult | null>(null);

  const handleCreate = () => {
    createBrief.mutate({ contentIdeaId, targetFormat, objective, keyMessage, requiresOriginalMedia }, {
      onSuccess: () => { setShowForm(false); setObjective(""); setKeyMessage(""); },
    });
  };

  const handleRender = (id: string) => {
    setRenderResult(null);
    renderCarousel.mutate(id, {
      onSuccess: (data) => setRenderResult(data),
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Content Briefs</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> New Brief
        </button>
      </div>

      {renderResult && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
          <p className="text-sm text-green-800 font-medium">{renderResult.message}</p>
          <p className="text-xs text-green-600 mt-1">{renderResult.assetIds.length} slides saved to Assets.</p>
        </div>
      )}

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <select value={contentIdeaId} onChange={(e) => setContentIdeaId(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
            <option value="">Select an idea...</option>
            {ideas?.map((i) => <option key={i.id} value={i.id}>{i.title}</option>)}
          </select>
          <div className="flex gap-3">
            <select value={targetFormat} onChange={(e) => setTargetFormat(e.target.value as ContentFormat)} className="px-3 py-2 border border-gray-300 rounded-lg text-sm">
              <option value="StaticImage">Static Image</option>
              <option value="Carousel">Carousel</option>
              <option value="Reel">Reel</option>
              <option value="Video">Video</option>
              <option value="Story">Story</option>
            </select>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={requiresOriginalMedia} onChange={(e) => setRequiresOriginalMedia(e.target.checked)} />
              Requires original media
            </label>
          </div>
          <input value={objective} onChange={(e) => setObjective(e.target.value)} placeholder="Objective" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={keyMessage} onChange={(e) => setKeyMessage(e.target.value)} placeholder="Key message" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <button onClick={handleCreate} disabled={!contentIdeaId || !objective || !keyMessage || createBrief.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            {createBrief.isPending ? "Creating..." : "Create"}
          </button>
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">Idea</th>
                <th className="px-4 py-3">Format</th>
                <th className="px-4 py-3">Objective</th>
                <th className="px-4 py-3">Key Message</th>
                <th className="px-4 py-3">Media</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {briefs?.map((b) => (
                <tr key={b.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500">{ideas?.find((i) => i.id === b.contentIdeaId)?.title ?? b.contentIdeaId.slice(0, 8)}</td>
                  <td className="px-4 py-3"><StatusBadge value={b.targetFormat} /></td>
                  <td className="px-4 py-3 max-w-xs truncate">{b.objective}</td>
                  <td className="px-4 py-3 max-w-xs truncate">{b.keyMessage}</td>
                  <td className="px-4 py-3">{b.requiresOriginalMedia ? "Original" : "Library"}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      {b.targetFormat === "Carousel" && (
                        <button
                          onClick={() => handleRender(b.id)}
                          disabled={renderCarousel.isPending}
                          className="flex items-center gap-1 px-2 py-1 text-xs bg-purple-100 text-purple-700 hover:bg-purple-200 rounded disabled:opacity-50"
                        >
                          <Image size={12} /> {renderCarousel.isPending ? "Rendering..." : "Render"}
                        </button>
                      )}
                      <button onClick={() => deleteBrief.mutate(b.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {briefs?.length === 0 && <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No briefs yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
