import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { Asset } from "../types";
import StatusBadge from "../components/ui/StatusBadge";
import { Upload, Trash2 } from "lucide-react";

export default function Assets() {
  const { data: assets, isLoading } = useQuery({ queryKey: ["assets"], queryFn: () => api.get<Asset[]>("/assets") });
  const qc = useQueryClient();
  const uploadMut = useMutation({
    mutationFn: (formData: FormData) => api.upload<Asset>("/assets/upload", formData),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => api.del(`/assets/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
  const fileRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0];
    if (!file) return;
    setUploading(true);
    const fd = new FormData();
    fd.append("file", file);
    uploadMut.mutate(fd, { onSettled: () => { setUploading(false); if (fileRef.current) fileRef.current.value = ""; } });
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Assets</h1>
        <div className="flex items-center gap-2">
          <input ref={fileRef} type="file" className="text-sm" />
          <button onClick={handleUpload} disabled={uploading} className="flex items-center gap-1 px-3 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50">
            <Upload size={16} /> {uploading ? "Uploading..." : "Upload"}
          </button>
        </div>
      </div>

      {isLoading ? <p className="text-gray-500">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-3">File</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Size</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {assets?.map((a) => (
                <tr key={a.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">{a.fileName}</td>
                  <td className="px-4 py-3"><StatusBadge value={a.assetType} /></td>
                  <td className="px-4 py-3 text-gray-500">{formatSize(a.fileSizeBytes)}</td>
                  <td className="px-4 py-3"><StatusBadge value={a.status} /></td>
                  <td className="px-4 py-3 text-gray-500">{new Date(a.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <button onClick={() => deleteMut.mutate(a.id)} className="p-1 text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))}
              {assets?.length === 0 && <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No assets yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
