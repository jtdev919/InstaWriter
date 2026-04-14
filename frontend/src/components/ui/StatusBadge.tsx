const colors: Record<string, string> = {
  // Content Ideas
  Captured: "bg-blue-100 text-blue-800",
  Planned: "bg-indigo-100 text-indigo-800",
  InProgress: "bg-yellow-100 text-yellow-800",
  Published: "bg-green-100 text-green-800",
  Archived: "bg-gray-100 text-gray-800",
  Rejected: "bg-red-100 text-red-800",
  // Drafts
  Draft: "bg-gray-100 text-gray-800",
  AwaitingReview: "bg-orange-100 text-orange-800",
  Approved: "bg-green-100 text-green-800",
  // Jobs
  Pending: "bg-yellow-100 text-yellow-800",
  Scheduled: "bg-blue-100 text-blue-800",
  Publishing: "bg-purple-100 text-purple-800",
  Failed: "bg-red-100 text-red-800",
  Cancelled: "bg-gray-100 text-gray-800",
  // Tasks
  Completed: "bg-green-100 text-green-800",
  Overdue: "bg-red-100 text-red-800",
  // Assets
  Uploaded: "bg-blue-100 text-blue-800",
  Processing: "bg-yellow-100 text-yellow-800",
  Ready: "bg-green-100 text-green-800",
  // Auth
  Connected: "bg-green-100 text-green-800",
  Expired: "bg-red-100 text-red-800",
  Revoked: "bg-gray-100 text-gray-800",
  // Campaign
  Active: "bg-green-100 text-green-800",
  Paused: "bg-yellow-100 text-yellow-800",
  // Priority
  Low: "bg-gray-100 text-gray-600",
  Medium: "bg-yellow-100 text-yellow-800",
  High: "bg-orange-100 text-orange-800",
  Urgent: "bg-red-100 text-red-800",
};

export default function StatusBadge({ value }: { value: string }) {
  const cls = colors[value] ?? "bg-gray-100 text-gray-800";
  return (
    <span className={`inline-block px-2 py-0.5 text-xs font-medium rounded-full ${cls}`}>
      {value}
    </span>
  );
}
