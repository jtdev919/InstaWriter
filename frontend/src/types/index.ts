// --- Enums (string unions matching C# JsonStringEnumConverter) ---

export type ContentIdeaStatus = "Captured" | "Planned" | "InProgress" | "Published" | "Archived" | "Rejected";
export type ContentRiskLevel = "Low" | "Medium" | "High";
export type ContentDraftStatus = "Draft" | "AwaitingReview" | "Approved" | "Rejected" | "Published";
export type PublishJobStatus = "Pending" | "Scheduled" | "Publishing" | "Published" | "Failed" | "Cancelled";
export type PublishMode = "Auto" | "Manual";
export type TaskItemStatus = "Pending" | "InProgress" | "Completed" | "Cancelled" | "Overdue";
export type TaskPriority = "Low" | "Medium" | "High" | "Urgent";
export type AssetType = "Image" | "Video" | "Screenshot" | "Mockup" | "QuoteCard" | "Carousel" | "Logo";
export type AssetStatus = "Uploaded" | "Processing" | "Ready" | "Archived";
export type ContentFormat = "StaticImage" | "Carousel" | "Reel" | "Video" | "Story";
export type ApprovalDecision = "Pending" | "Approved" | "Rejected" | "RevisionRequested";
export type PlatformType = "Instagram";
export type AuthStatus = "Pending" | "Connected" | "Expired" | "Revoked";
export type CampaignStatus = "Draft" | "Active" | "Paused" | "Completed" | "Archived";

// --- Entities ---

export interface ContentIdea {
  id: string;
  title: string;
  summary?: string;
  sourceType?: string;
  pillarName?: string;
  riskLevel: ContentRiskLevel;
  status: ContentIdeaStatus;
  createdAt: string;
  plannedPublishDate?: string;
}

export interface ContentBrief {
  id: string;
  contentIdeaId: string;
  targetFormat: ContentFormat;
  objective: string;
  audience: string;
  hookDirection: string;
  keyMessage: string;
  cta: string;
  requiresOriginalMedia: boolean;
  requiresManualApproval: boolean;
  createdAt: string;
}

export interface ContentDraft {
  id: string;
  contentIdeaId: string;
  contentBriefId?: string;
  caption: string;
  script?: string;
  carouselCopyJson?: string;
  hashtagSet?: string;
  coverText?: string;
  complianceScore?: number;
  versionNo: number;
  status: ContentDraftStatus;
  createdAt: string;
  contentIdea?: ContentIdea;
}

export interface PublishJob {
  id: string;
  contentDraftId: string;
  channelAccountId?: string;
  plannedPublishDate?: string;
  publishMode: PublishMode;
  externalContainerId?: string;
  externalMediaId?: string;
  status: PublishJobStatus;
  failureReason?: string;
  createdAt: string;
  contentDraft?: ContentDraft;
}

export interface TaskItem {
  id: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  owner: string;
  dueDate?: string;
  taskType: string;
  priority: TaskPriority;
  status: TaskItemStatus;
  description?: string;
  createdAt: string;
}

export interface Asset {
  id: string;
  assetType: AssetType;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  blobUri?: string;
  thumbnailUri?: string;
  owner?: string;
  status: AssetStatus;
  tags?: string;
  pillarName?: string;
  contentIdeaId?: string;
  contentDraftId?: string;
  createdAt: string;
}

export interface Approval {
  id: string;
  contentDraftId: string;
  approver: string;
  decision: ApprovalDecision;
  comments?: string;
  timestamp: string;
}

export interface CalendarEvent {
  id: string;
  taskItemId: string;
  externalCalendarId?: string;
  startDateTime: string;
  endDateTime: string;
  reminderProfile?: string;
  createdAt: string;
}

export interface WorkflowEvent {
  id: string;
  eventType: string;
  entityType: string;
  entityId: string;
  eventTime: string;
  payloadJson?: string;
  correlationId?: string;
}

export interface InsightSnapshot {
  id: string;
  publishJobId: string;
  snapshotDate: string;
  reach: number;
  views: number;
  likes: number;
  comments: number;
  shares: number;
  saves: number;
  profileVisits: number;
  followsAttributed: number;
}

export interface Campaign {
  id: string;
  name: string;
  objective: string;
  startDate?: string;
  endDate?: string;
  status: CampaignStatus;
  audienceSegment?: string;
  kpiSet?: string;
  createdAt: string;
}

export interface ContentPillar {
  id: string;
  name: string;
  description?: string;
  priorityWeight: number;
  createdAt: string;
}

export interface BrandProfile {
  id: string;
  name: string;
  voiceGuide: string;
  toneGuide: string;
  ctaStyle: string;
  disclaimerRules: string;
  defaultHashtagSets: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ChannelAccount {
  id: string;
  platformType: PlatformType;
  accountName: string;
  externalAccountId?: string;
  tokenExpiry?: string;
  authStatus: AuthStatus;
  isActive: boolean;
  createdAt: string;
}

// --- Analytics types ---

export interface PostScore {
  publishJobId: string;
  contentDraftId: string;
  pillarName?: string;
  targetFormat?: string;
  engagementScore: number;
  reach: number;
  totalEngagements: number;
  engagementRate: number;
}

export interface PerformanceCluster {
  groupKey: string;
  groupType: string;
  postCount: number;
  avgEngagementScore: number;
  avgReach: number;
  avgEngagementRate: number;
}

export interface PillarPerformance {
  pillarName: string;
  postCount: number;
  avgEngagementScore: number;
  currentWeight: number;
  recommendedWeight: number;
}

export interface PostRecommendation {
  pillarName: string;
  suggestedFormat: string;
  rationale: string;
  confidenceScore: number;
}

// --- Transition map (mirrors StatusTransitions.cs) ---

export const IDEA_TRANSITIONS: Record<ContentIdeaStatus, ContentIdeaStatus[]> = {
  Captured: ["Planned", "Rejected"],
  Planned: ["InProgress", "Rejected"],
  InProgress: ["Published", "Rejected"],
  Published: ["Archived"],
  Rejected: ["Archived", "Captured"],
  Archived: [],
};

export const DRAFT_TRANSITIONS: Record<ContentDraftStatus, ContentDraftStatus[]> = {
  Draft: ["AwaitingReview"],
  AwaitingReview: ["Approved", "Rejected"],
  Approved: ["Published"],
  Rejected: ["Draft"],
  Published: [],
};

export const JOB_TRANSITIONS: Record<PublishJobStatus, PublishJobStatus[]> = {
  Pending: ["Scheduled", "Cancelled"],
  Scheduled: ["Publishing", "Cancelled"],
  Publishing: ["Published", "Failed"],
  Published: [],
  Failed: ["Pending", "Cancelled"],
  Cancelled: [],
};

export const TASK_TRANSITIONS: Record<TaskItemStatus, TaskItemStatus[]> = {
  Pending: ["InProgress", "Cancelled", "Overdue"],
  InProgress: ["Completed", "Cancelled"],
  Overdue: ["InProgress", "Cancelled"],
  Completed: [],
  Cancelled: [],
};
