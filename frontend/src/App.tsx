import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import AppShell from "./components/layout/AppShell";
import Dashboard from "./pages/Dashboard";
import ContentIdeas from "./pages/ContentIdeas";
import Tasks from "./pages/Tasks";
import Stub from "./pages/Stub";

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<AppShell />}>
            <Route index element={<Dashboard />} />
            <Route path="ideas" element={<ContentIdeas />} />
            <Route path="briefs" element={<Stub name="Content Briefs" />} />
            <Route path="drafts" element={<Stub name="Content Drafts" />} />
            <Route path="approvals" element={<Stub name="Approval Queue" />} />
            <Route path="publish" element={<Stub name="Publish Jobs" />} />
            <Route path="tasks" element={<Tasks />} />
            <Route path="calendar" element={<Stub name="Calendar" />} />
            <Route path="assets" element={<Stub name="Assets" />} />
            <Route path="campaigns" element={<Stub name="Campaigns" />} />
            <Route path="pillars" element={<Stub name="Content Pillars" />} />
            <Route path="analytics" element={<Stub name="Analytics" />} />
            <Route path="channels" element={<Stub name="Channel Accounts" />} />
            <Route path="brand-profiles" element={<Stub name="Brand Profiles" />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
