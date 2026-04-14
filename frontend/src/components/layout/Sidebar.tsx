import { NavLink } from "react-router-dom";
import {
  LayoutDashboard, Lightbulb, FileText, PenTool, CheckSquare,
  Send, ListTodo, Calendar, Image, Target, Layers, BarChart3,
  Radio, Palette
} from "lucide-react";

const groups = [
  {
    label: "Overview",
    items: [
      { to: "/", icon: LayoutDashboard, label: "Dashboard" },
    ],
  },
  {
    label: "Content",
    items: [
      { to: "/ideas", icon: Lightbulb, label: "Ideas" },
      { to: "/briefs", icon: FileText, label: "Briefs" },
      { to: "/drafts", icon: PenTool, label: "Drafts" },
      { to: "/approvals", icon: CheckSquare, label: "Approvals" },
      { to: "/publish", icon: Send, label: "Publish Jobs" },
    ],
  },
  {
    label: "Operations",
    items: [
      { to: "/tasks", icon: ListTodo, label: "Tasks" },
      { to: "/calendar", icon: Calendar, label: "Calendar" },
      { to: "/assets", icon: Image, label: "Assets" },
    ],
  },
  {
    label: "Strategy",
    items: [
      { to: "/campaigns", icon: Target, label: "Campaigns" },
      { to: "/pillars", icon: Layers, label: "Pillars" },
      { to: "/analytics", icon: BarChart3, label: "Analytics" },
    ],
  },
  {
    label: "Settings",
    items: [
      { to: "/channels", icon: Radio, label: "Channels" },
      { to: "/brand-profiles", icon: Palette, label: "Brand" },
    ],
  },
];

export default function Sidebar() {
  return (
    <aside className="w-56 bg-gray-900 text-gray-300 flex flex-col min-h-screen">
      <div className="px-4 py-5 text-white font-bold text-lg tracking-tight">
        InstaWriter
      </div>
      <nav className="flex-1 px-2 space-y-4 overflow-y-auto">
        {groups.map((g) => (
          <div key={g.label}>
            <p className="px-3 text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
              {g.label}
            </p>
            {g.items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  `flex items-center gap-2 px-3 py-1.5 rounded text-sm ${
                    isActive
                      ? "bg-gray-800 text-white"
                      : "hover:bg-gray-800 hover:text-white"
                  }`
                }
              >
                <item.icon size={16} />
                {item.label}
              </NavLink>
            ))}
          </div>
        ))}
      </nav>
    </aside>
  );
}
