import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { Campaign } from "../types";

const KEY = ["campaigns"];

export function useCampaigns() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<Campaign[]>("/campaigns") });
}

export function useCreateCampaign() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<Campaign>) => api.post<Campaign>("/campaigns", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateCampaign() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<Campaign> & { id: string }) =>
      api.put<Campaign>(`/campaigns/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteCampaign() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/campaigns/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
