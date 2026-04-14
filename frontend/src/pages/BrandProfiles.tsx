import { useState } from "react";
import { useBrandProfiles, useCreateBrandProfile, useDeleteBrandProfile } from "../hooks/use-brand-profiles";
import { Plus, Trash2 } from "lucide-react";

export default function BrandProfiles() {
  const { data: profiles, isLoading } = useBrandProfiles();
  const createProfile = useCreateBrandProfile();
  const deleteProfile = useDeleteBrandProfile();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [voiceGuide, setVoiceGuide] = useState("");
  const [toneGuide, setToneGuide] = useState("");
  const [ctaStyle, setCtaStyle] = useState("");
  const [disclaimerRules, setDisclaimerRules] = useState("");
  const [defaultHashtagSets, setDefaultHashtagSets] = useState("");

  const handleCreate = () => {
    createProfile.mutate({ name, voiceGuide, toneGuide, ctaStyle, disclaimerRules, defaultHashtagSets }, {
      onSuccess: () => { setShowForm(false); setName(""); setVoiceGuide(""); setToneGuide(""); setCtaStyle(""); setDisclaimerRules(""); setDefaultHashtagSets(""); },
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Brand Profiles</h1>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
          <Plus size={16} /> New Profile
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-3">
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Brand name" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <textarea value={voiceGuide} onChange={(e) => setVoiceGuide(e.target.value)} placeholder="Voice guide" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" rows={2} />
          <textarea value={toneGuide} onChange={(e) => setToneGuide(e.target.value)} placeholder="Tone guide" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" rows={2} />
          <input value={ctaStyle} onChange={(e) => setCtaStyle(e.target.value)} placeholder="CTA style" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={disclaimerRules} onChange={(e) => setDisclaimerRules(e.target.value)} placeholder="Disclaimer rules" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <input value={defaultHashtagSets} onChange={(e) => setDefaultHashtagSets(e.target.value)} placeholder="Default hashtags (e.g. #wellness #health)" className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          <button onClick={handleCreate} disabled={!name || createProfile.isPending} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            {createProfile.isPending ? "Creating..." : "Create"}
          </button>
        </div>
      )}

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="space-y-4">
          {profiles?.map((p) => (
            <div key={p.id} className="bg-white rounded-lg shadow p-5">
              <div className="flex justify-between items-start mb-3">
                <h3 className="text-lg font-semibold text-gray-900">{p.name}</h3>
                <button onClick={() => deleteProfile.mutate(p.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                <div><span className="font-medium text-gray-600">Voice:</span> <span className="text-gray-500">{p.voiceGuide || "-"}</span></div>
                <div><span className="font-medium text-gray-600">Tone:</span> <span className="text-gray-500">{p.toneGuide || "-"}</span></div>
                <div><span className="font-medium text-gray-600">CTA Style:</span> <span className="text-gray-500">{p.ctaStyle || "-"}</span></div>
                <div><span className="font-medium text-gray-600">Disclaimers:</span> <span className="text-gray-500">{p.disclaimerRules || "-"}</span></div>
              </div>
              {p.defaultHashtagSets && <p className="text-sm text-indigo-500 mt-2">{p.defaultHashtagSets}</p>}
            </div>
          ))}
          {profiles?.length === 0 && <p className="text-gray-400 text-center py-8">No brand profiles yet</p>}
        </div>
      )}
    </div>
  );
}
