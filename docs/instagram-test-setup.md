# Instagram Test Account & Meta App Setup Guide

Step-by-step guide for setting up a test Instagram professional account and Meta developer app so you can validate the InstaWriter publishing flow end-to-end.

---

## 1. Create a Test Instagram Account

1. Create a new Instagram account at [instagram.com](https://www.instagram.com/) (or use the mobile app)
   - Use a dedicated email for your dev/test account
   - Pick a handle like `instawriter_dev` or similar
2. **Convert to a Professional account**:
   - Go to Settings → Account → Switch to Professional Account
   - Choose **Business** (not Creator) — Business accounts have full Graph API access
   - Connect to a Facebook Page (required — create a new one if needed, e.g. "InstaWriter Dev Page")
3. Optional: set the account to **Private** so test posts aren't publicly visible
   - Note: private accounts can still publish via the API, but insights may be limited

## 2. Create a Meta Developer App

1. Go to [developers.facebook.com](https://developers.facebook.com/) and log in with your Facebook account
2. Click **My Apps → Create App**
3. Select app type: **Business**
4. Fill in:
   - App name: `InstaWriter Dev`
   - Contact email: your dev email
   - Business portfolio: create or select one
5. On the app dashboard, click **Add Product** and add **Instagram Graph API**

## 3. Configure Permissions

In your Meta app settings, you need these permissions:

| Permission | Purpose |
|---|---|
| `instagram_basic` | Read account profile and media |
| `instagram_content_publish` | Publish images, reels, carousels |
| `instagram_manage_insights` | Pull post performance metrics |
| `pages_show_list` | List Facebook Pages linked to IG |
| `pages_read_engagement` | Required for publishing flow |

While the app is in **Development Mode**, these permissions work immediately for any user with a role on the app (admin/developer/tester). No App Review needed for testing.

## 4. Add Test Users

1. In your Meta app, go to **App Roles → Roles**
2. Add your test Facebook account as an **Admin** or **Developer**
3. The invited user must accept the invitation

## 5. Generate a Long-Lived Access Token

### Option A: Graph API Explorer (quickest for dev)

1. Go to [Graph API Explorer](https://developers.facebook.com/tools/explorer/)
2. Select your app from the dropdown
3. Click **Generate Access Token**
4. Grant the permissions listed above
5. This gives you a **short-lived token** (~1 hour)

### Option B: Exchange for a Long-Lived Token (~60 days)

```bash
curl -X GET "https://graph.facebook.com/v22.0/oauth/access_token?\
grant_type=fb_exchange_token&\
client_id={APP_ID}&\
client_secret={APP_SECRET}&\
fb_exchange_token={SHORT_LIVED_TOKEN}"
```

The response contains a `access_token` valid for ~60 days and an `expires_in` field (seconds).

### Option C: Page-Level Never-Expiring Token

For unattended operation (closest to production):

1. Get a long-lived user token (Option B)
2. Get your Page ID:
   ```bash
   curl "https://graph.facebook.com/v22.0/me/accounts?access_token={LONG_LIVED_USER_TOKEN}"
   ```
3. The `access_token` returned for each page in that response is a **page token that does not expire** as long as the user remains an admin of the page

## 6. Get Your Instagram Business Account ID

```bash
curl "https://graph.facebook.com/v22.0/me/accounts?\
fields=instagram_business_account&\
access_token={ACCESS_TOKEN}"
```

Look for `instagram_business_account.id` — this is the `igUserId` that InstaWriter uses in `IInstagramPublisher`.

## 7. Configure InstaWriter

### User Secrets (local development)

```bash
dotnet user-secrets set "Instagram:DefaultAccessToken" "{YOUR_TOKEN}" --project src/InstaWriter.Api
dotnet user-secrets set "Instagram:DefaultIgUserId" "{YOUR_IG_USER_ID}" --project src/InstaWriter.Api
```

### Register via the API

```bash
# Create a channel account
curl -X POST https://localhost:7201/api/channels \
  -H "Content-Type: application/json" \
  -d '{
    "platformType": "Instagram",
    "accountName": "instawriter_dev",
    "externalAccountId": "{YOUR_IG_USER_ID}"
  }'

# Store the token (use the returned channel ID)
curl -X PUT https://localhost:7201/api/channels/{channelId}/token \
  -H "Content-Type: application/json" \
  -d '{
    "accessToken": "{YOUR_TOKEN}",
    "tokenExpiry": "2026-06-12T00:00:00Z"
  }'
```

## 8. Validate the Publishing Flow

### Test 1: Publish a Single Image

The image must be publicly accessible via URL (Instagram fetches it server-side). For testing, upload an asset to Azure Blob Storage first, or use any public image URL.

```bash
# Create a content idea
curl -X POST https://localhost:7201/api/content/ideas \
  -H "Content-Type: application/json" \
  -d '{"title": "Test Post", "summary": "Smoke test", "riskLevel": "Low"}'

# Create a draft (use the returned idea ID)
curl -X POST https://localhost:7201/api/content/drafts \
  -H "Content-Type: application/json" \
  -d '{
    "contentIdeaId": "{ideaId}",
    "caption": "Testing InstaWriter publishing flow 🚀 #test"
  }'

# Create a publish job (use the returned draft ID)
curl -X POST https://localhost:7201/api/publish/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "contentDraftId": "{draftId}",
    "channelAccountId": "{channelId}"
  }'

# Execute the publish job
curl -X POST https://localhost:7201/api/publish/jobs/{jobId}/execute
```

### Test 2: Check the Result

```bash
# Check job status
curl https://localhost:7201/api/publish/jobs/{jobId}/status
```

A successful publish returns `externalMediaId` and status `Published`. Check your test Instagram account — the post should appear.

## 9. Rate Limits & Quotas

| Limit | Value | Notes |
|---|---|---|
| API-published posts | 25 per 24-hour rolling window | Per Instagram Business account |
| API rate limit | 200 calls/user/hour | Across all Graph API endpoints |
| Video processing | Up to 60 seconds | InstagramPublisher polls every 2s |

During development you're unlikely to hit these, but be aware if running automated tests that actually publish.

## 10. Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `OAuthException: Invalid access token` | Token expired or revoked | Generate a new token (Section 5) |
| `Application does not have permission` | Missing permission scope | Re-authorize with required permissions (Section 3) |
| `Media posted before this limit` | Hit 25-post/24hr limit | Wait for the rolling window to clear |
| `The image could not be fetched` | Image URL not publicly accessible | Ensure the URL is reachable from Meta's servers (no auth, no localhost) |
| `The video could not be processed` | Unsupported format or too large | Use MP4, H.264, AAC audio, max 100MB |
| Container status stuck at `IN_PROGRESS` | Video still processing | Wait longer; check video format compliance |

## 11. Development Mode vs. Live Mode

| | Development Mode | Live Mode |
|---|---|---|
| Who can use it | Users with app roles only | Any authorized user |
| App Review required | No | Yes — must submit for review |
| Rate limits | Same production limits | Same production limits |
| Token behavior | Same | Same |
| Recommended for | Local dev & staging | Production |

**Stay in Development Mode** for all testing. Only switch to Live Mode when you're ready to onboard real users, which requires Meta App Review for `instagram_content_publish` and other permissions.
