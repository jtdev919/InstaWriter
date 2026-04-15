const API_BASE = import.meta.env.VITE_API_BASE ?? "/api";
const API_KEY = import.meta.env.VITE_API_KEY ?? "";

class ApiError extends Error {
  status: number;
  body: unknown;
  constructor(status: number, body: unknown) {
    super(`API error ${status}`);
    this.status = status;
    this.body = body;
  }
}

function authHeaders(extra?: Record<string, string>): Record<string, string> {
  const h: Record<string, string> = { ...extra };
  if (API_KEY) h["X-Api-Key"] = API_KEY;
  return h;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const opts: RequestInit = {
    method,
    headers: authHeaders({ "Content-Type": "application/json" }),
  };
  if (body !== undefined) opts.body = JSON.stringify(body);

  const res = await fetch(`${API_BASE}${path}`, opts);
  if (!res.ok) {
    const errBody = await res.json().catch(() => null);
    throw new ApiError(res.status, errBody);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const api = {
  get: <T>(path: string) => request<T>("GET", path),
  post: <T>(path: string, body?: unknown) => request<T>("POST", path, body),
  put: <T>(path: string, body: unknown) => request<T>("PUT", path, body),
  del: (path: string) => request<void>("DELETE", path),
  upload: async <T>(path: string, formData: FormData): Promise<T> => {
    const res = await fetch(`${API_BASE}${path}`, {
      method: "POST",
      body: formData,
      headers: authHeaders(),
    });
    if (!res.ok) {
      const errBody = await res.json().catch(() => null);
      throw new ApiError(res.status, errBody);
    }
    return res.json();
  },
};

export { ApiError };
