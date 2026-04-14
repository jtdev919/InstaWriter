import { useQuery } from "@tanstack/react-query";
import { api } from "../api/client";
import type { PostScore, PerformanceCluster, PillarPerformance, PostRecommendation } from "../types";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

// Add types to match the API
interface CTAInsight { ctaPattern: string; postCount: number; avgEngagementRate: number; avgSaves: number; avgShares: number; }

export default function Analytics() {
  const { data: topPosts } = useQuery({ queryKey: ["analytics", "top"], queryFn: () => api.get<PostScore[]>("/analytics/posts/top?count=10") });
  const { data: clusters } = useQuery({ queryKey: ["analytics", "clusters"], queryFn: () => api.get<PerformanceCluster[]>("/analytics/clusters") });
  const { data: ctaInsights } = useQuery({ queryKey: ["analytics", "cta"], queryFn: () => api.get<CTAInsight[]>("/analytics/cta-insights") });
  const { data: pillarPerf } = useQuery({ queryKey: ["analytics", "pillars"], queryFn: () => api.get<PillarPerformance[]>("/analytics/pillars/performance") });
  const { data: recommendations } = useQuery({ queryKey: ["analytics", "recs"], queryFn: () => api.get<PostRecommendation[]>("/analytics/recommendations") });

  return (
    <div className="space-y-8">
      <h1 className="text-2xl font-bold text-gray-900">Analytics</h1>

      {/* Recommendations */}
      <section>
        <h2 className="text-lg font-semibold text-gray-700 mb-3">Recommendations</h2>
        {recommendations?.length ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {recommendations.map((r, i) => (
              <div key={i} className="bg-white rounded-lg shadow p-4">
                <div className="flex justify-between items-start">
                  <span className="font-medium text-gray-900">{r.pillarName}</span>
                  <span className="text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded-full">{r.suggestedFormat}</span>
                </div>
                <p className="text-sm text-gray-500 mt-2">{r.rationale}</p>
                <p className="text-xs text-gray-400 mt-1">Confidence: {(r.confidenceScore * 100).toFixed(0)}%</p>
              </div>
            ))}
          </div>
        ) : <p className="text-gray-400">No recommendations yet — publish some content first.</p>}
      </section>

      {/* Top Posts */}
      <section>
        <h2 className="text-lg font-semibold text-gray-700 mb-3">Top Posts</h2>
        {topPosts?.length ? (
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3">Pillar</th>
                  <th className="px-4 py-3">Format</th>
                  <th className="px-4 py-3">Reach</th>
                  <th className="px-4 py-3">Engagements</th>
                  <th className="px-4 py-3">Rate</th>
                  <th className="px-4 py-3">Score</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {topPosts.map((p) => (
                  <tr key={p.publishJobId} className="hover:bg-gray-50">
                    <td className="px-4 py-3">{p.pillarName ?? "-"}</td>
                    <td className="px-4 py-3">{p.targetFormat ?? "-"}</td>
                    <td className="px-4 py-3">{p.reach.toLocaleString()}</td>
                    <td className="px-4 py-3">{p.totalEngagements.toLocaleString()}</td>
                    <td className="px-4 py-3">{(p.engagementRate * 100).toFixed(1)}%</td>
                    <td className="px-4 py-3 font-medium">{p.engagementScore.toFixed(0)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : <p className="text-gray-400">No published posts with insights yet.</p>}
      </section>

      {/* Performance Clusters */}
      {clusters && clusters.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold text-gray-700 mb-3">Performance Clusters</h2>
          <div className="bg-white rounded-lg shadow p-4" style={{ height: 300 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={clusters}>
                <XAxis dataKey="groupKey" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Bar dataKey="avgEngagementScore" fill="#6366f1" name="Avg Score" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
      )}

      {/* CTA Insights */}
      {ctaInsights && ctaInsights.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold text-gray-700 mb-3">CTA Insights</h2>
          <div className="bg-white rounded-lg shadow p-4" style={{ height: 300 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={ctaInsights}>
                <XAxis dataKey="ctaPattern" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Bar dataKey="avgEngagementRate" fill="#10b981" name="Avg Engagement Rate" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
      )}

      {/* Pillar Performance */}
      {pillarPerf && pillarPerf.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold text-gray-700 mb-3">Pillar Performance</h2>
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3">Pillar</th>
                  <th className="px-4 py-3">Posts</th>
                  <th className="px-4 py-3">Avg Score</th>
                  <th className="px-4 py-3">Current Weight</th>
                  <th className="px-4 py-3">Recommended</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {pillarPerf.map((p) => (
                  <tr key={p.pillarName} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{p.pillarName}</td>
                    <td className="px-4 py-3">{p.postCount}</td>
                    <td className="px-4 py-3">{p.avgEngagementScore.toFixed(1)}</td>
                    <td className="px-4 py-3">{p.currentWeight}</td>
                    <td className="px-4 py-3 font-medium text-indigo-600">{p.recommendedWeight}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
}
