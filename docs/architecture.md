# Social Content Orchestration Platform (SCOP) - Architecture & PRD

Yes. The right way to build this is as a **content operations platform** for your brand, not as a simple "post scheduler."

Because your strategy now includes both:

* the **app brand**, and
* **your fitness lifestyle / founder journey**,

the system needs to support three different outcomes:

1. fully automated content generation and publishing where safe,
2. human-reviewed publishing for higher-risk or more personal content,
3. scheduled tasks and reminders when a step still must be done manually, such as recording a Reel, filming a workout clip, approving a caption, or posting from the Instagram mobile app.

Instagram's current platform supports publishing for **professional accounts** and supports content publishing for **single images, videos, reels, and carousels** through the Instagram platform. The platform is for **business and creator professional accounts**, not personal accounts. Meta also exposes media and account insights through the Instagram API, and API-published posting is subject to publish limits, including a commonly documented **25 API-published posts in a 24-hour moving period** for Instagram Business accounts. ([Facebook Developers][1])

That means the system should be designed with:

* **professional Instagram account required**,
* **API-driven publishing where supported**,
* **human-in-the-loop approval**,
* **task/event orchestration for manual gaps**,
* **analytics feedback loop**.

Your current app direction already supports this kind of content engine well because you already have meaningful product assets and proof points to draw from, including dashboard/sign-in/settings/labs and a broader AI-assisted health decision engine direction.   

---

## 1. Solution overview

### Proposed system name

**Social Content Orchestration Platform (SCOP)**

### Primary business objective

Automate the lifecycle of Instagram marketing content from:

* idea capture,
* to content planning,
* to generation,
* to review,
* to scheduling,
* to posting,
* to analytics,
* to optimization.

### Secondary objective

Where a post cannot or should not be fully automated, the platform must:

* create a task,
* assign an owner,
* schedule a calendar event,
* send reminders,
* preserve the publishing deadline,
* track completion state.

---

## 2. High-level operating model

The platform should operate in **three lanes**.

### Lane A: Fully automated

Use when all content inputs already exist and risk is low.

Examples:

* educational carousel from lab/HRV/sleep topic library,
* static image post generated from approved templates,
* repurposed "build in public" update from approved release notes,
* testimonial post from approved text.

Flow:
Idea -> AI draft -> policy check -> approve rule -> schedule -> auto-publish -> collect insights

### Lane B: Human approval required

Use when content is brand-sensitive, health-sensitive, or founder-facing.

Examples:

* captions containing health recommendations,
* posts referencing biomarker interpretation,
* founder story content,
* before/after progress claims,
* beta tester invitations,
* product feature announcements.

Flow:
Idea -> AI draft -> review queue -> approval -> schedule -> auto-publish or manual-post task

### Lane C: Manual capture required

Use when original media must be created by you.

Examples:

* workout Reel,
* "talking to camera" founder video,
* app demo screen recording,
* lifestyle footage,
* voiceover clip.

Flow:
Idea -> task created -> calendar event scheduled -> reminder sent -> media uploaded -> caption finalized -> publish

---

## 3. Architecture principles

The system should be designed with these enterprise principles:

### 3.1 Human-in-the-loop by design

The platform should automate production, not remove judgment.

### 3.2 API-first

Every planning, drafting, approval, scheduling, and posting action should be exposed through APIs.

### 3.3 Event-driven orchestration

Every content state change should emit an event.

### 3.4 Channel abstraction

Instagram is first, but the model should later support Facebook, LinkedIn, TikTok, X, and email.

### 3.5 Auditability

Every draft, edit, approval, rejection, publish action, and analytics snapshot must be tracked.

### 3.6 Safety and compliance

Because your brand touches health, the system must enforce guardrails around medical claims, performance claims, and testimonial language.

---

## 4. Target architecture

### 4.1 Recommended Azure-aligned stack

This fits your broader app direction and keeps operational patterns consistent.

#### Front end

* **Admin Web Portal**: React or Blazor
* **Mobile-friendly Founder Console**: optional MAUI or responsive web
* **Content Calendar UI**
* **Approval Queue UI**
* **Asset Library UI**
* **Analytics Dashboard UI**

#### Backend services

* **Content Strategy Service**
* **Content Generation Service**
* **Workflow Orchestration Service**
* **Publishing Service**
* **Calendar/Task Service**
* **Insights Service**
* **Notification Service**
* **Policy/Compliance Service**

#### Data/storage

* **Azure SQL** for structured workflow/state
* **Azure Blob Storage** for videos, images, thumbnails, drafts, transcripts
* **Azure Service Bus** for event-driven workflow
* **Azure Key Vault** for tokens/secrets
* **Application Insights** for telemetry
* Optional: **Azure OpenAI** for draft generation, summarization, caption variants, hashtag clustering

#### External integrations

* **Instagram Graph API / Instagram Platform**
* **Meta app + token management**
* **Google Calendar or Microsoft 365 Calendar**
* **Email/SMS/push notifications**
* Optional later:

  * Canva
  * CapCut workflow
  * Dropbox/OneDrive/Google Drive
  * Notion/Airtable
  * Slack/Teams approvals

---

## 5. Core system modules

## 5.1 Content Strategy Service

This service turns your marketing plan into structured content opportunities.

### Responsibilities

* store content pillars,
* store campaign themes,
* store audience segments,
* generate weekly and monthly content plans,
* map planned content to app milestones and founder lifestyle milestones.

### Example content pillars

* Founder journey
* Fitness lifestyle
* App build in public
* Data-to-insight education
* Labs/wearables education
* Beta/tester recruitment
* Transformation/progress
* Trust/explainability

### Inputs

* content pillars,
* campaign goals,
* launch phase,
* product roadmap updates,
* your workout and habit data,
* app release notes,
* biometric/lab topics,
* prior post performance.

### Outputs

* content backlog,
* weekly publishing plan,
* per-post creative brief.

---

## 5.2 Content Generation Service

This generates first drafts of content from structured inputs.

### Responsibilities

* write captions,
* generate carousel copy,
* create hook variants,
* produce CTA variants,
* generate hashtags,
* repurpose source content,
* summarize long-form notes into posts.

### Input sources

* founder journal entries,
* product changelog,
* app screenshots,
* workout logs,
* lab topics,
* release notes,
* beta feedback,
* feature backlog,
* your voice/tone rules.

### Outputs

* caption draft,
* Reel script,
* shot list,
* storyboard,
* carousel slide text,
* thumbnail text,
* approval notes.

### AI constraints

The service must never publish directly without policy scoring if content includes:

* health claims,
* biomarker interpretation,
* supplement recommendations,
* transformation claims,
* disease/diagnosis language.

---

## 5.3 Asset Management Service

This is the system of record for media.

### Responsibilities

* store images and videos,
* manage asset tags,
* manage versioning,
* generate thumbnails,
* track source ownership,
* store captions/transcripts,
* associate assets to campaigns and post drafts.

### Asset types

* workout clips,
* talking-head videos,
* app screenshots,
* mockups,
* screen recordings,
* photos,
* quote cards,
* carousels,
* logo variants.

### Metadata

* asset type,
* owner,
* creation date,
* status,
* pillar,
* rights/consent,
* related campaign,
* related post ID,
* publish suitability.

---

## 5.4 Workflow Orchestration Service

This is the heart of the platform.

### Responsibilities

* manage state machine,
* trigger downstream steps,
* create tasks when manual work is needed,
* create calendar events,
* send reminders,
* escalate missed deadlines,
* transition post status across lifecycle.

### Example state model

* IdeaCaptured
* BriefReady
* AwaitingAssets
* AwaitingFounderRecording
* DraftGenerated
* AwaitingReview
* Approved
* Scheduled
* PendingPublish
* Published
* InsightsCollected
* RepurposeCandidate
* Archived
* Rejected

This should be implemented as an explicit workflow engine, not hidden in code branches.

---

## 5.5 Publishing Service

This abstracts Instagram-specific publishing mechanics.

### Responsibilities

* validate eligibility for automated publishing,
* create publish jobs,
* post via API where supported,
* enforce publish rate limits,
* retry failed posts,
* log external publish IDs,
* handle status callbacks/polling.

### Instagram rules to model

* publish only for connected professional accounts,
* respect media-type support,
* respect platform rate limits,
* store external media IDs,
* separate "scheduled" from "actually published."

Instagram's platform supports content publishing for professional accounts and insights retrieval for professional media/account objects, which is exactly what this service should target. ([Facebook Developers][1])

---

## 5.6 Calendar and Task Service

This handles manual execution work.

### Responsibilities

* create tasks,
* assign due dates,
* create calendar events,
* send reminders,
* escalate when overdue,
* support recurring content rituals.

### Manual task examples

* Record founder Reel
* Capture workout footage
* Approve caption
* Review medical/compliance phrasing
* Upload final edited video
* Publish from mobile if API unsupported for scenario
* Respond to comments after posting

### Calendar event examples

* Monday 7:00 AM: Record "week start" Reel
* Tuesday 6:00 PM: Approve Friday carousel
* Wednesday 8:00 PM: Upload workout clips
* Friday 9:00 AM: Publish founder post manually if needed

### Reminder channels

* email,
* push notification,
* SMS,
* Teams/Slack,
* in-app founder dashboard.

---

## 5.7 Insights and Optimization Service

This closes the loop.

### Responsibilities

* pull media insights,
* pull account insights,
* compare actual vs planned,
* identify high-performing patterns,
* feed performance back into content planning.

### Metrics to track

* reach,
* views,
* watch time where available,
* likes,
* comments,
* shares,
* saves,
* profile visits,
* link clicks,
* follows per post,
* beta signups attributed,
* waitlist conversions.

Meta exposes media and account insights for Instagram professional accounts, which supports this analytics layer. ([Facebook Developers][2])

---

## 5.8 Policy and Compliance Service

This is especially important for your niche.

### Responsibilities

* flag risky language,
* classify health-related content,
* require manual review when threshold exceeded,
* enforce disclaimer insertion rules,
* prevent disallowed medical phrasing,
* score confidence.

### Policy categories

* low risk: motivational/lifestyle
* medium risk: training/recovery education
* high risk: biomarker interpretation, supplements, hormone content, symptom discussion

### Example automatic rule

If caption contains any of:

* "treat,"
* "cure,"
* "diagnose,"
* "fix your hormones,"
* "this supplement will solve,"

then:

* block autopublish,
* require compliance review,
* append safe rewrite suggestions.

---

## 6. Detailed workflows

## 6.1 Workflow A: Automated educational carousel

1. Content Strategy Service generates topic:
   "3 reasons your HRV may be low this week"
2. Content Generation Service produces:

   * hook,
   * 6-slide carousel copy,
   * caption,
   * CTA,
   * hashtags
3. Policy Service scores content
4. If low/medium risk and within allowed rule set:

   * move to approval queue
5. Approver accepts
6. Publishing Service schedules post
7. Post publishes via Instagram API
8. Insights Service pulls post performance after 24h, 72h, 7d
9. Optimization engine updates future topic weighting

---

## 6.2 Workflow B: Founder Reel with manual recording

1. Strategy engine creates brief:
   "Why I'm building this health app"
2. System creates:

   * talking points,
   * 30-second script,
   * shot list,
   * hook options,
   * CTA
3. Workflow engine sees `requires_original_video = true`
4. Task Service creates task:
   `Record founder Reel`
5. Calendar Service creates event:
   Tuesday 7:30 AM, 30 minutes
6. Reminder sent 24h before and 30 minutes before
7. You record clip and upload raw file
8. System transcribes clip and proposes:

   * caption,
   * subtitles,
   * cover text,
   * hashtag options
9. You approve
10. If publishable via API, system posts; otherwise it creates final manual publish task
11. Insights are collected afterward

---

## 6.3 Workflow C: App feature release announcement

1. Product update enters system from changelog or release note
2. Strategy engine proposes post types:

   * build-in-public Reel,
   * carousel walkthrough,
   * story sequence
3. System attaches screenshots from asset library
4. Draft generated
5. Because this is product messaging, approval required
6. Approved content is scheduled for launch date
7. Publish job triggered
8. Engagement tracked
9. Winning format reused for future releases

---

## 7. Data model

Below is the minimum core domain model.

## 7.1 Core entities

### BrandProfile

* BrandProfileId
* Name
* VoiceGuide
* ToneGuide
* CTAStyle
* DisclaimerRules
* DefaultHashtagSets

### ChannelAccount

* ChannelAccountId
* PlatformType
* AccountName
* AccountType
* ExternalAccountId
* AuthStatus
* TokenExpiry
* PublishCapabilities
* IsActive

### Campaign

* CampaignId
* Name
* Objective
* StartDate
* EndDate
* Status
* AudienceSegment
* KPISet

### ContentPillar

* ContentPillarId
* Name
* Description
* PriorityWeight

### ContentIdea

* ContentIdeaId
* SourceType
* Title
* Summary
* PillarId
* RiskLevel
* PlannedWeek
* Status

### ContentBrief

* ContentBriefId
* ContentIdeaId
* TargetFormat
* Objective
* Audience
* HookDirection
* KeyMessage
* CTA
* RequiresOriginalMedia
* RequiresManualApproval

### Asset

* AssetId
* AssetType
* BlobUri
* ThumbnailUri
* Transcript
* Owner
* Status
* Tags
* UsageRightsStatus

### ContentDraft

* DraftId
* BriefId
* Caption
* Script
* CarouselCopyJson
* HashtagSet
* CoverText
* ComplianceScore
* VersionNo
* Status

### Approval

* ApprovalId
* DraftId
* Approver
* Decision
* Comments
* Timestamp

### PublishJob

* PublishJobId
* DraftId
* ChannelAccountId
* PlannedPublishDateTime
* PublishMode
* ExternalContainerId
* ExternalMediaId
* Status
* FailureReason

### TaskItem

* TaskItemId
* RelatedEntityType
* RelatedEntityId
* Owner
* DueDateTime
* TaskType
* Priority
* Status

### CalendarEvent

* CalendarEventId
* TaskItemId
* ExternalCalendarId
* StartDateTime
* EndDateTime
* ReminderProfile

### InsightSnapshot

* InsightSnapshotId
* PublishJobId
* SnapshotDate
* Reach
* Views
* Likes
* Comments
* Shares
* Saves
* ProfileVisits
* FollowsAttributed

### WorkflowEvent

* WorkflowEventId
* EventType
* EntityType
* EntityId
* EventTime
* PayloadJson
* CorrelationId

---

## 8. API design

Representative internal APIs:

### Content planning

* `POST /api/content/ideas/generate`
* `POST /api/content/briefs/generate`
* `GET /api/content/calendar`

### Drafting

* `POST /api/content/drafts/generate`
* `POST /api/content/drafts/{id}/regenerate-caption`
* `POST /api/content/drafts/{id}/score-compliance`

### Assets

* `POST /api/assets/upload`
* `POST /api/assets/{id}/transcribe`
* `GET /api/assets/search`

### Workflow

* `POST /api/workflow/events`
* `POST /api/tasks/create`
* `POST /api/tasks/{id}/complete`
* `POST /api/calendar/events/create`

### Publishing

* `POST /api/publish/jobs`
* `POST /api/publish/jobs/{id}/execute`
* `GET /api/publish/jobs/{id}/status`

### Insights

* `POST /api/insights/sync`
* `GET /api/insights/dashboard`
* `GET /api/insights/recommendations`

---

## 9. Scheduling model

This is where your question about manual steps gets solved properly.

The system should not assume every scheduled item is "a post."
It should schedule **work units**.

### Work unit types

* Publish content
* Record content
* Review draft
* Upload media
* Edit video
* Approve caption
* Respond to comments
* Repurpose top performer

### Scheduling rules

* every content brief gets a due chain,
* if original media is required, create upstream recording task,
* if approval is required, reserve reviewer time,
* if publish date approaches and dependencies are incomplete, escalate.

### Example dependency chain

`Record video` -> `Upload raw media` -> `Generate caption` -> `Approve draft` -> `Schedule post` -> `Publish` -> `Collect insights`

### Escalation logic

* T-48h: remind owner
* T-24h: escalate to founder
* T-4h: downgrade to fallback asset/caption if not ready
* T-1h: convert to manual publish reminder if automation path not ready

---

## 10. Manual-step handling design

Some steps should remain manual by design.

### Must remain manual or at least reviewed

* showing your face in videos,
* founder story narratives,
* personal transformation claims,
* nuanced health content,
* raw workout footage selection,
* emotionally sensitive posts.

### System behavior for manual steps

When a step is manual, the orchestration service must:

* mark stage as blocked by manual dependency,
* create task,
* create calendar event,
* attach AI-generated instructions,
* attach script/brief,
* attach file upload link,
* remind you until completion,
* offer fallback content if deadline is missed.

This turns manual work into a managed operational step instead of chaos.

---

## 11. Security and platform governance

## 11.1 Instagram/Meta integration controls

* professional account required,
* secure token storage,
* token refresh job,
* least-privilege access,
* audit all publish actions,
* never hardcode account credentials.

## 11.2 Internal controls

* RBAC: Founder, Content Editor, Reviewer, Publisher, Analyst, Admin
* immutable audit logs for approvals and publishing
* separation between draft and published content
* approval thresholds by content risk class

## 11.3 Health-brand safeguards

* disclaimer injection rules,
* prohibited term library,
* compliance override role,
* evidence/source note field for educational claims,
* forced review on supplement or hormone-related content.

---

## 12. Non-functional requirements

### Availability

* 99.5% minimum for internal platform
* publish queue must be durable

### Scalability

* design for 1 brand first, many brands later
* multi-tenant capable data model

### Performance

* draft generation under 15 seconds target
* publish job creation under 3 seconds
* analytics sync batch jobs under 10 minutes

### Observability

* all workflow transitions logged
* failure reasons visible in dashboard
* per-channel success/failure rate tracked

### Resilience

* retry publish failures
* dead-letter queue for failed publish jobs
* fallback from auto-post to manual task when API failure persists

---

## 13. Recommended implementation pattern

## Phase 1: Foundation

Build:

* BrandProfile
* ChannelAccount
* ContentIdea
* ContentBrief
* ContentDraft
* TaskItem
* PublishJob
* basic dashboard
* calendar integration
* notification service

Outcome:
You can plan content, generate drafts, assign tasks, and track deadlines.

## Phase 2: Instagram automation

Build:

* Meta app integration
* token management
* publish job service
* media upload/publish flow
* publish audit logging
* basic insights pull

Outcome:
Static/image/video/carousel/Reel workflows can be automated where supported and approved. Meta's current publishing support for professional accounts is the backbone here. ([Facebook Developers][1])

## Phase 3: Human-in-the-loop optimization

Build:

* compliance scoring
* approval queue
* transcription
* video brief generation
* missed-deadline escalation
* fallback content substitution

Outcome:
The system can intelligently adapt when you do not record or approve on time.

## Phase 4: Performance intelligence

Build:

* post performance scoring
* best-post clustering
* CTA optimization
* content pillar weighting
* recommended next-post engine

Outcome:
The system starts helping decide what to publish next based on what actually works.

---

## 14. Suggested user journeys

### Founder weekly operating rhythm

Sunday:

* system generates next week's content plan

Monday:

* founder approves plan

Tuesday:

* record two Reels

Wednesday:

* upload assets, approve captions

Thursday:

* system schedules/publishes educational post

Friday:

* founder journey post publishes

Saturday:

* performance review dashboard updates next week plan

### Daily operating rhythm

Morning:

* review due tasks

Midday:

* approve content or record clip

Evening:

* respond to comments and DMs

---

## 15. Recommended technology choices

If you want this aligned to your broader architecture and skill set:

### Backend

* .NET 8/9 Web API or Azure Functions
* Durable Functions if you want orchestrated workflows
* Service Bus for event-driven queueing

### Database

* Azure SQL first, because workflow state is relational and auditable

### Storage

* Azure Blob for media and generated artifacts

### Frontend

* React admin portal

### AI layer

* Azure OpenAI for:

  * caption generation,
  * summarization,
  * hook variants,
  * transcript-to-post transformation,
  * analytics narrative explanation

### Calendar/tasks

* Microsoft Graph if you want Outlook/365 alignment
* Google Calendar if personal workflow is simpler there

### Notifications

* SendGrid email
* Twilio SMS
* push via your app later
* Teams optional

---

## 16. Delivery recommendation

I would build this as **an internal marketing operations platform first**, not as a customer-facing feature of the health app.

That gives you:

* faster iteration,
* lower risk,
* cleaner separation of concerns,
* the option to later commercialize the platform if desired.

---

## 17. Executive summary

The correct architecture is:

**AI-assisted content planning + workflow orchestration + API publishing + manual task/event scheduling + analytics feedback**

Not just:
**"Generate captions and auto-post."**

The platform should:

* automate what is safe,
* schedule what is manual,
* enforce review where risk exists,
* learn from performance,
* keep you on a repeatable creator operating cadence.

That is the enterprise-grade way to do it.

* scope,
* functional requirements,
* non-functional requirements,
* logical architecture,
* physical Azure architecture,
* sequence diagrams,
* entity model,
* phased implementation plan.

[1]: https://developers.facebook.com/docs/instagram-platform/content-publishing/ "Publish Content - Instagram Platform - Meta for Developers"
[2]: https://developers.facebook.com/docs/instagram-platform/reference/instagram-media/insights/ "Instagram Media Insights - Meta for Developers"
