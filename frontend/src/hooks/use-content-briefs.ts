import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ContentBrief } from "../types";

const KEY = ["content-briefs"];

export function useContentBriefs() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<ContentBrief[]>("/content/briefs") });
}

export function useCreateContentBrief() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<ContentBrief>) => api.post<ContentBrief>("/content/briefs", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteContentBrief() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/content/briefs/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
