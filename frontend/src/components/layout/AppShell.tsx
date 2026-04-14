import { Outlet } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api/client";
import Sidebar from "./Sidebar";
import { Bell } from "lucide-react";
import { useState } from "react";

interface NotificationItem {
  id: string;
  recipient: string;
  channel: string;
  subject: string;
  body: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  isRead: boolean;
  createdAt: string;
}

export default function AppShell() {
  const [showNotifs, setShowNotifs] = useState(false);
  const qc = useQueryClient();

  const { data: unreadCount } = useQuery({
    queryKey: ["notifications", "unread-count"],
    queryFn: () => api.get<{ count: number }>("/notifications/unread-count"),
    refetchInterval: 30_000,
  });

  const { data: notifications } = useQuery({
    queryKey: ["notifications"],
    queryFn: () => api.get<NotificationItem[]>("/notifications?unreadOnly=true"),
    enabled: showNotifs,
  });

  const markRead = useMutation({
    mutationFn: (id: string) => api.post(`/notifications/${id}/read`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["notifications", "unread-count"] });
    },
  });

  const markAllRead = useMutation({
    mutationFn: () => api.post("/notifications/read-all"),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["notifications", "unread-count"] });
    },
  });

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />
      <div className="flex-1 flex flex-col">
        <header className="bg-white border-b border-gray-200 px-6 py-3 flex justify-end items-center relative">
          <button
            onClick={() => setShowNotifs(!showNotifs)}
            className="relative p-2 text-gray-500 hover:text-gray-700 rounded-lg hover:bg-gray-100"
          >
            <Bell size={20} />
            {(unreadCount?.count ?? 0) > 0 && (
              <span className="absolute -top-0.5 -right-0.5 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                {unreadCount!.count > 9 ? "9+" : unreadCount!.count}
              </span>
            )}
          </button>

          {showNotifs && (
            <div className="absolute right-6 top-14 w-96 bg-white rounded-lg shadow-lg border border-gray-200 z-50 max-h-96 overflow-y-auto">
              <div className="flex justify-between items-center px-4 py-2 border-b">
                <span className="font-semibold text-sm text-gray-700">Notifications</span>
                {(unreadCount?.count ?? 0) > 0 && (
                  <button onClick={() => markAllRead.mutate()} className="text-xs text-indigo-600 hover:text-indigo-800">
                    Mark all read
                  </button>
                )}
              </div>
              {notifications?.length ? (
                notifications.map((n) => (
                  <div
                    key={n.id}
                    onClick={() => { markRead.mutate(n.id); }}
                    className="px-4 py-3 border-b border-gray-100 hover:bg-gray-50 cursor-pointer"
                  >
                    <p className="text-sm font-medium text-gray-900">{n.subject}</p>
                    <p className="text-xs text-gray-500 mt-0.5 line-clamp-2">{n.body}</p>
                    <p className="text-xs text-gray-400 mt-1">{new Date(n.createdAt).toLocaleString()}</p>
                  </div>
                ))
              ) : (
                <div className="px-4 py-6 text-center text-sm text-gray-400">No unread notifications</div>
              )}
            </div>
          )}
        </header>
        <main className="flex-1 p-6 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
