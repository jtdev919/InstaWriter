import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ChannelAccount } from "../types";

const KEY = ["channels"];

export function useChannels() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<ChannelAccount[]>("/channels") });
}

export function useCreateChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<ChannelAccount>) => api.post<ChannelAccount>("/channels", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateChannelToken() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, accessToken, tokenExpiry }: { id: string; accessToken: string; tokenExpiry?: string }) =>
      api.put<unknown>(`/channels/${id}/token`, { accessToken, tokenExpiry }),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/channels/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
