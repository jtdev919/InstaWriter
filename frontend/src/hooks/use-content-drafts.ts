import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ContentDraft } from "../types";

const KEY = ["content-drafts"];

export function useContentDrafts() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<ContentDraft[]>("/content/drafts") });
}

export function useCreateContentDraft() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<ContentDraft>) => api.post<ContentDraft>("/content/drafts", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateContentDraft() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<ContentDraft> & { id: string }) =>
      api.put<ContentDraft>(`/content/drafts/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useTransitionContentDraft() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api.post<ContentDraft>(`/content/drafts/${id}/transition`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteContentDraft() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/content/drafts/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
