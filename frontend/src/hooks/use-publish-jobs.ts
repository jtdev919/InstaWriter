import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { PublishJob } from "../types";

const KEY = ["publish-jobs"];

export function usePublishJobs() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<PublishJob[]>("/publish/jobs") });
}

export function useCreatePublishJob() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<PublishJob>) => api.post<PublishJob>("/publish/jobs", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useTransitionPublishJob() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api.post<PublishJob>(`/publish/jobs/${id}/transition`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeletePublishJob() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/publish/jobs/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
