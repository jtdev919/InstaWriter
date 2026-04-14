import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { BrandProfile } from "../types";

const KEY = ["brand-profiles"];

export function useBrandProfiles() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<BrandProfile[]>("/brand-profiles") });
}

export function useCreateBrandProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<BrandProfile>) => api.post<BrandProfile>("/brand-profiles", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateBrandProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Partial<BrandProfile> & { id: string }) =>
      api.put<BrandProfile>(`/brand-profiles/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteBrandProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/brand-profiles/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
