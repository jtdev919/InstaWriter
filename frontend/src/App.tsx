import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import AppShell from "./components/layout/AppShell";
import Dashboard from "./pages/Dashboard";
import ContentIdeas from "./pages/ContentIdeas";
import ContentBriefs from "./pages/ContentBriefs";
import ContentDrafts from "./pages/ContentDrafts";
import ApprovalQueue from "./pages/ApprovalQueue";
import PublishJobs from "./pages/PublishJobs";
import Tasks from "./pages/Tasks";
import CalendarView from "./pages/CalendarView";
import Assets from "./pages/Assets";
import Campaigns from "./pages/Campaigns";
import ContentPillars from "./pages/ContentPillars";
import Analytics from "./pages/Analytics";
import ChannelAccounts from "./pages/ChannelAccounts";
import BrandProfiles from "./pages/BrandProfiles";

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
            <Route path="briefs" element={<ContentBriefs />} />
            <Route path="drafts" element={<ContentDrafts />} />
            <Route path="approvals" element={<ApprovalQueue />} />
            <Route path="publish" element={<PublishJobs />} />
            <Route path="tasks" element={<Tasks />} />
            <Route path="calendar" element={<CalendarView />} />
            <Route path="assets" element={<Assets />} />
            <Route path="campaigns" element={<Campaigns />} />
            <Route path="pillars" element={<ContentPillars />} />
            <Route path="analytics" element={<Analytics />} />
            <Route path="channels" element={<ChannelAccounts />} />
            <Route path="brand-profiles" element={<BrandProfiles />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
