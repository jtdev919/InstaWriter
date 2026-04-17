import { useState } from "react";
import { useContentDrafts, useCreateContentDraft, useTransitionContentDraft, useDeleteContentDraft, useUpdateContentDraft } from "../hooks/use-content-drafts";
import { useContentIdeas } from "../hooks/use-content-ideas";
import StatusBadge from "../components/ui/StatusBadge";
import { DRAFT_TRANSITIONS, type ContentDraftStatus, type ContentDraft } from "../types";
import { Plus, Trash2, Eye, X, Save, PlusCircle, Minus, Copy, Sparkles } from "lucide-react";

interface SlideContent {
  type: "title" | "content" | "cta-bridge" | "cta";
  category: string;
  headline: string;
  body: string;
  cta?: string;
  subtext?: string;
}

function buildInitialSlides(draft: ContentDraft): SlideContent[] {
  // Check if we have saved carousel JSON
  if (draft.carouselCopyJson) {
    try {
      return JSON.parse(draft.carouselCopyJson);
    } catch { /* fall through to auto-split */ }
  }

  const lines = draft.caption
    .split('\n')
    .map(l => l.trim())
    .filter(l => l.length > 0 && !l.startsWith('#'));

  const title = draft.contentIdea?.title ?? draft.coverText ?? "Swipe to learn more";
  const category = draft.contentIdea?.pillarName?.toUpperCase() ?? "HEALTH & PERFORMANCE";

  const bodyChunks: string[] = [];
  let current = "";
  for (const line of lines) {
    if ((current + " " + line).length > 200 && current.length > 0) {
      bodyChunks.push(current.trim());
      current = line;
    } else {
      current = current ? current + " " + line : line;
    }
  }
  if (current) bodyChunks.push(current.trim());
  while (bodyChunks.length < 5) bodyChunks.push("");

  return [
    { type: "title", category, headline: title, body: "Swipe to learn more" },
    { type: "content", category, headline: bodyChunks[0]?.split('.')[0] ?? "The Story", body: bodyChunks[0] ?? "" },
    { type: "content", category, headline: bodyChunks[1]?.split('.')[0] ?? "Key Insight", body: bodyChunks[1] ?? "" },
    { type: "content", category, headline: bodyChunks[2]?.split('.')[0] ?? "Why It Matters", body: bodyChunks[2] ?? "" },
    { type: "content", category, headline: bodyChunks[3]?.split('.')[0] ?? "Take Action", body: bodyChunks[3] ?? "" },
    { type: "content", category, headline: bodyChunks[4]?.split('.')[0] ?? "The Difference", body: bodyChunks[4] ?? "" },
    { type: "cta-bridge", category, headline: "Follow for more", body: "Biohacking tips, build updates, and health insights." },
    { type: "cta", category, headline: "Link in bio", body: "", cta: "Get Started Free", subtext: (draft.hashtagSet ?? "").slice(0, 80) },
  ];
}

function SlidePreview({ slide, slideNum, selected, onClick }: {
  slide: SlideContent;
  slideNum: number;
  selected: boolean;
  onClick: () => void;
}) {
  const ring = selected ? "ring-2 ring-purple-500 ring-offset-2" : "";
  const base = `w-40 h-40 flex-shrink-0 rounded-lg flex flex-col justify-center p-4 relative overflow-hidden text-white cursor-pointer hover:opacity-90 ${ring}`;

  if (slide.type === "title") return (
    <div className={`${base} bg-gradient-to-br from-[#3B0764] to-[#6B21A8] items-center text-center`} onClick={onClick}>
      <p className="text-[7px] font-bold tracking-widest text-[#9333EA] uppercase mb-1">{slide.category}</p>
      <p className="text-[10px] font-extrabold uppercase leading-tight">{slide.headline}</p>
      <p className="text-[6px] text-white/50 mt-1">{slide.body}</p>
      <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-gradient-to-r from-[#9333EA] to-[#06B6D4]" />
    </div>
  );

  if (slide.type === "cta-bridge") return (
    <div className={`${base} bg-gradient-to-br from-[#3B0764] to-[#6B21A8] items-center text-center`} onClick={onClick}>
      <p className="text-[9px] font-extrabold uppercase leading-tight mb-1">{slide.headline}</p>
      <p className="text-[6px] text-white/60">{slide.body}</p>
      <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-gradient-to-r from-[#9333EA] to-[#06B6D4]" />
    </div>
  );

  if (slide.type === "cta") return (
    <div className={`${base} items-center text-center`} style={{ background: "linear-gradient(180deg, #3B0764 0%, #6B21A8 50%, #3B0764 100%)" }} onClick={onClick}>
      <p className="text-[9px] font-extrabold uppercase leading-tight mb-2">{slide.headline}</p>
      <div className="px-2 py-1 bg-gradient-to-r from-[#06B6D4] to-[#22D3EE] text-[#1a0a2e] text-[7px] font-bold rounded-full">{slide.cta}</div>
      <p className="text-[6px] text-white/50 mt-1">{slide.subtext}</p>
      <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-gradient-to-r from-[#9333EA] to-[#06B6D4]" />
    </div>
  );

  return (
    <div className={`${base} bg-[#1a0a2e]`} onClick={onClick}>
      <span className="absolute top-1 right-2 text-xl font-extrabold text-[#9333EA]/10">{slideNum}</span>
      <p className="text-[6px] font-bold tracking-widest text-[#9333EA] uppercase mb-1">{slide.category}</p>
      <p className="text-[8px] font-extrabold uppercase leading-tight mb-1">{slide.headline}</p>
      <p className="text-[6px] text-white/70 leading-relaxed line-clamp-5">{slide.body}</p>
      <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-gradient-to-r from-[#9333EA] to-[#06B6D4]" />
    </div>
  );
}

function CarouselEditor({ draft, onClose }: { draft: ContentDraft; onClose: () => void }) {
  const [slides, setSlides] = useState<SlideContent[]>(() => buildInitialSlides(draft));
  const [selectedIdx, setSelectedIdx] = useState(0);
  const [aiDirection, setAiDirection] = useState("");
  const [aiLoading, setAiLoading] = useState(false);
  const updateDraft = useUpdateContentDraft();

  const selected = slides[selectedIdx];

  const updateSlide = (field: keyof SlideContent, value: string) => {
    setSlides(prev => prev.map((s, i) => i === selectedIdx ? { ...s, [field]: value } : s));
  };

  const deleteSlide = () => {
    if (slides.length <= 2) return;
    setSlides(prev => prev.filter((_, i) => i !== selectedIdx));
    setSelectedIdx(Math.min(selectedIdx, slides.length - 2));
  };

  const addSlide = () => {
    if (slides.length >= 10) return;
    const newSlide: SlideContent = {
      type: "content",
      category: selected.category,
      headline: "New Slide",
      body: "Add your content here.",
    };
    const insertAt = selectedIdx + 1;
    setSlides(prev => [...prev.slice(0, insertAt), newSlide, ...prev.slice(insertAt)]);
    setSelectedIdx(insertAt);
  };

  const cloneSlide = () => {
    if (slides.length >= 10) return;
    const cloned: SlideContent = { ...selected };
    const insertAt = selectedIdx + 1;
    setSlides(prev => [...prev.slice(0, insertAt), cloned, ...prev.slice(insertAt)]);
    setSelectedIdx(insertAt);
  };

  const aiRewrite = async () => {
    setAiLoading(true);
    try {
      const res = await fetch(
        `${import.meta.env.VITE_API_BASE ?? "/api"}/content/slides/rewrite`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            ...(import.meta.env.VITE_API_KEY ? { "X-Api-Key": import.meta.env.VITE_API_KEY } : {}),
          },
          body: JSON.stringify({
            headline: selected.headline,
            body: selected.body,
            direction: aiDirection || null,
          }),
        }
      );
      if (res.ok) {
        const data = await res.json();
        setSlides(prev =>
          prev.map((s, i) =>
            i === selectedIdx ? { ...s, headline: data.headline, body: data.body } : s
          )
        );
        setAiDirection("");
      }
    } finally {
      setAiLoading(false);
    }
  };

  const handleSave = () => {
    updateDraft.mutate({
      id: draft.id,
      carouselCopyJson: JSON.stringify(slides),
    });
  };

  return (
    <div className="bg-white p-4 rounded-lg shadow mb-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Carousel Editor</h2>
        <div className="flex items-center gap-2">
          <button
            onClick={handleSave}
            disabled={updateDraft.isPending}
            className="flex items-center gap-1 px-3 py-1.5 text-xs bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
          >
            <Save size={12} /> {updateDraft.isPending ? "Saving..." : "Save Slides"}
          </button>
          <button onClick={onClose} className="p-1 text-gray-400 hover:text-gray-600"><X size={18} /></button>
        </div>
      </div>

      {/* Slide strip */}
      <div className="flex gap-2 overflow-x-auto pb-3 mb-4">
        {slides.map((slide, i) => (
          <div key={i} className="text-center flex-shrink-0">
            <SlidePreview slide={slide} slideNum={i + 1} selected={i === selectedIdx} onClick={() => setSelectedIdx(i)} />
            <p className="text-[10px] text-gray-500 mt-1">Slide {i + 1}</p>
          </div>
        ))}
      </div>

      {/* Editor for selected slide */}
      <div className="border border-gray-200 rounded-lg p-4 bg-gray-50">
        <div className="flex items-center gap-3 mb-3">
          <span className="text-xs font-semibold text-gray-500 uppercase">Editing Slide {selectedIdx + 1} of {slides.length}</span>
          <span className="text-xs text-purple-600 bg-purple-50 px-2 py-0.5 rounded">{selected.type}</span>
          <div className="flex items-center gap-1 ml-auto">
            <button
              onClick={addSlide}
              disabled={slides.length >= 10}
              className="flex items-center gap-1 px-2 py-1 text-xs bg-green-100 text-green-700 hover:bg-green-200 rounded disabled:opacity-30"
              title="Add blank slide after this one"
            >
              <PlusCircle size={12} /> Add
            </button>
            <button
              onClick={cloneSlide}
              disabled={slides.length >= 10}
              className="flex items-center gap-1 px-2 py-1 text-xs bg-blue-100 text-blue-700 hover:bg-blue-200 rounded disabled:opacity-30"
              title="Clone this slide"
            >
              <Copy size={12} /> Clone
            </button>
            <button
              onClick={deleteSlide}
              disabled={slides.length <= 2}
              className="flex items-center gap-1 px-2 py-1 text-xs bg-red-100 text-red-700 hover:bg-red-200 rounded disabled:opacity-30"
              title="Delete this slide"
            >
              <Minus size={12} /> Delete
            </button>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-xs font-medium text-gray-600 block mb-1">Category / Label</label>
            <input
              value={selected.category}
              onChange={(e) => updateSlide("category", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-gray-600 block mb-1">Headline</label>
            <input
              value={selected.headline}
              onChange={(e) => updateSlide("headline", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div className="col-span-2">
            <label className="text-xs font-medium text-gray-600 block mb-1">
              {selected.type === "title" ? "Subtext" : selected.type === "cta" ? "Button Text" : "Body"}
            </label>
            <textarea
              value={selected.type === "cta" ? (selected.cta ?? "") : selected.body}
              onChange={(e) => updateSlide(selected.type === "cta" ? "cta" : "body", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
              rows={3}
            />
          </div>
          {selected.type === "cta" && (
            <div className="col-span-2">
              <label className="text-xs font-medium text-gray-600 block mb-1">Subtext (below button)</label>
              <input
                value={selected.subtext ?? ""}
                onChange={(e) => updateSlide("subtext", e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
              />
            </div>
          )}
        </div>

        {/* AI Rewrite */}
        <div className="mt-4 pt-3 border-t border-gray-200">
          <div className="flex items-center gap-2">
            <Sparkles size={14} className="text-purple-500" />
            <span className="text-xs font-semibold text-gray-600">AI Rewrite</span>
          </div>
          <div className="flex gap-2 mt-2">
            <input
              value={aiDirection}
              onChange={(e) => setAiDirection(e.target.value)}
              placeholder="Optional direction, e.g. 'make it more personal' or 'add urgency'"
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
            <button
              onClick={aiRewrite}
              disabled={aiLoading}
              className="flex items-center gap-1 px-3 py-2 text-xs bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 whitespace-nowrap"
            >
              <Sparkles size={12} /> {aiLoading ? "Rewriting..." : "Rewrite with AI"}
            </button>
          </div>
          <p className="text-[10px] text-gray-400 mt-1">AI will rewrite the headline and body of this slide. Add direction to guide the tone.</p>
        </div>
      </div>

      {/* Quick Links */}
      <div className="mt-4 pt-3 border-t border-gray-200">
        <div className="flex items-center gap-4">
          <span className="text-xs font-semibold text-gray-600">Quick Links:</span>
          <a
            href="https://docs.google.com/forms/d/e/1FAIpQLSdRhAStyQpK484QXvk2icOUqBqIVyzZFIQG8o_4thZX6WlnoA/viewform"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs text-indigo-600 hover:text-indigo-800 underline"
          >
            Beta Signup Form
          </a>
          <a
            href="https://healthcoach.teknicalsolutionz.com"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs text-indigo-600 hover:text-indigo-800 underline"
          >
            Landing Page
          </a>
        </div>
      </div>

      <p className="text-xs text-gray-400 mt-3">
        Click a slide to edit it. Changes preview live. Click "Save Slides" to store your edits.
      </p>
    </div>
  );
}

export default function ContentDrafts() {
  const { data: drafts, isLoading } = useContentDrafts();
  const { data: ideas } = useContentIdeas();
  const createDraft = useCreateContentDraft();
  const transition = useTransitionContentDraft();
  const deleteDraft = useDeleteContentDraft();
  const [showForm, setShowForm] = useState(false);
  const [contentIdeaId, setContentIdeaId] = useState("");
  const [caption, setCaption] = useState("");
  const [editorDraftId, setEditorDraftId] = useState<string | null>(null);

  const editorDraft = drafts?.find(d => d.id === editorDraftId);

  const handleCreate = () => {
    createDraft.mutate({ contentIdeaId, caption }, {
      onSuccess: () => { setShowForm(false); setCaption(""); },
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

      {/* Carousel Editor Panel */}
      {editorDraft && (
        <CarouselEditor draft={editorDraft} onClose={() => setEditorDraftId(null)} />
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
                          onClick={() => setEditorDraftId(editorDraftId === d.id ? null : d.id)}
                          className="flex items-center gap-1 px-2 py-1 text-xs bg-purple-100 text-purple-700 hover:bg-purple-200 rounded"
                          title="Edit carousel slides"
                        >
                          <Eye size={12} /> Carousel
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
