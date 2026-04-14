import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ContentPillar } from "../types";

const KEY = ["content-pillars"];

export function useContentPillars() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<ContentPillar[]>("/content-pillars") });
}

export function useCreateContentPillar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<ContentPillar>) => api.post<ContentPillar>("/content-pillars", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateContentPillar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<ContentPillar> & { id: string }) =>
      api.put<ContentPillar>(`/content-pillars/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteContentPillar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/content-pillars/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
