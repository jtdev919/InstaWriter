import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { Approval } from "../types";

const KEY = ["approvals"];

export function useApprovals() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<Approval[]>("/approvals") });
}

export function useCreateApproval() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<Approval>) => api.post<Approval>("/approvals", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEY });
      qc.invalidateQueries({ queryKey: ["content-drafts"] });
    },
  });
}
