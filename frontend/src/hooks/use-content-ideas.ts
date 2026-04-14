import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ContentIdea } from "../types";

const KEY = ["content-ideas"];

export function useContentIdeas() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<ContentIdea[]>("/content/ideas") });
}

export function useContentIdea(id: string) {
  return useQuery({ queryKey: [...KEY, id], queryFn: () => api.get<ContentIdea>(`/content/ideas/${id}`) });
}

export function useCreateContentIdea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<ContentIdea>) => api.post<ContentIdea>("/content/ideas", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateContentIdea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<ContentIdea> & { id: string }) =>
      api.put<ContentIdea>(`/content/ideas/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteContentIdea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/content/ideas/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useTransitionContentIdea() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api.post<ContentIdea>(`/content/ideas/${id}/transition`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
