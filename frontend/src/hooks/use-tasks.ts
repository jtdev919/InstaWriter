import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { TaskItem } from "../types";

const KEY = ["tasks"];

export function useTasks() {
  return useQuery({ queryKey: KEY, queryFn: () => api.get<TaskItem[]>("/tasks") });
}

export function useCreateTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<TaskItem>) => api.post<TaskItem>("/tasks", data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useTransitionTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api.post<TaskItem>(`/tasks/${id}/transition`, { status }),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useCompleteTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post<TaskItem>(`/tasks/${id}/complete`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del(`/tasks/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
