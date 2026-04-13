using System.Text.Json;
using Azure.AI.OpenAI;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace InstaWriter.Infrastructure.AI;

public class AzureOpenAIContentGenerator(AzureOpenAIClient client, string deploymentName, ILogger<AzureOpenAIContentGenerator> logger) : IContentGenerator
{
    private ChatClient ChatClient => client.GetChatClient(deploymentName);

    private const string SystemPrompt = """
        You are an expert Instagram content strategist for a health and fitness technology brand.
        The brand builds an AI-assisted health decision engine that interprets labs, wearables, and biomarkers.
        The founder is also a fitness enthusiast who shares his journey publicly.

        Content pillars: Founder journey, Fitness lifestyle, App build in public, Data-to-insight education,
        Labs/wearables education, Beta/tester recruitment, Transformation/progress, Trust/explainability.

        Rules:
        - Write in an authentic, knowledgeable but approachable tone
        - Use short punchy sentences for hooks
        - Include a clear CTA in every caption
        - Never make medical claims or diagnose conditions
        - Use "may", "can help", "consider" instead of definitive health statements
        - Hashtags should be a mix of broad reach and niche targeting
        """;

    public async Task<GeneratedDraft> GenerateDraftAsync(GenerateDraftRequest request, CancellationToken ct = default)
    {
        var format = request.TargetFormat ?? "single_image";

        var scriptInstruction = format is "reel" or "reels"
            ? "\"a 30-second talking points script\""
            : "null";

        var userPrompt = $$"""
            Generate an Instagram {{format}} post for this content idea:

            Title: {{request.IdeaTitle}}
            Summary: {{request.IdeaSummary ?? "N/A"}}
            Content Pillar: {{request.PillarName ?? "General"}}

            Respond in this exact JSON format (no markdown, no code fences):
            {
              "caption": "the full caption text with emojis and line breaks as \n",
              "script": {{scriptInstruction}},
              "hashtagSet": "#hashtag1 #hashtag2 (10-15 relevant hashtags)",
              "coverText": "short bold text for the cover/thumbnail (under 10 words)"
            }
            """;

        try
        {
            logger.LogInformation("Generating draft for idea: {Title}", request.IdeaTitle);

            var response = await ChatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage(userPrompt)
                ],
                new ChatCompletionOptions { Temperature = 0.8f },
                ct);

            var json = response.Value.Content[0].Text;
            // Strip markdown code fences if the model adds them
            json = json.Trim();
            if (json.StartsWith("```")) json = json[json.IndexOf('\n')..];
            if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")];
            json = json.Trim();

            var result = JsonSerializer.Deserialize<GeneratedDraft>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new GeneratedDraft("Failed to parse response", null, "", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Draft generation failed");
            throw;
        }
    }

    public async Task<string> RegenerateCaptionAsync(RegenerateCaptionRequest request, CancellationToken ct = default)
    {
        var directionNote = request.Direction != null ? $" with this direction: {request.Direction}" : "";

        var userPrompt = $$"""
            Rewrite this Instagram caption{{directionNote}}:

            Current caption:
            {{request.CurrentCaption}}

            Return ONLY the new caption text, nothing else. Keep hashtags separate if they were included.
            """;

        try
        {
            logger.LogInformation("Regenerating caption");

            var response = await ChatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage(userPrompt)
                ],
                new ChatCompletionOptions { Temperature = 0.9f },
                ct);

            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Caption regeneration failed");
            throw;
        }
    }

    public async Task<ComplianceResult> ScoreComplianceAsync(string caption, CancellationToken ct = default)
    {
        var userPrompt = $$"""
            Analyze this Instagram caption for health/medical compliance risk.
            This is for a health tech brand — we must avoid making medical claims.

            Caption:
            {{caption}}

            Respond in this exact JSON format (no markdown, no code fences):
            {
              "score": 0.0 to 1.0 (0 = no risk, 1 = high risk),
              "riskLevel": "Low" or "Medium" or "High",
              "flags": ["list of specific phrases or claims that are problematic"],
              "suggestedRewrite": "a safer version of the caption if risk is Medium or High, otherwise null"
            }
            """;

        try
        {
            logger.LogInformation("Scoring compliance for caption");

            var response = await ChatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a health content compliance reviewer. Be strict about medical claims, supplement recommendations, and diagnostic language."),
                    new UserChatMessage(userPrompt)
                ],
                new ChatCompletionOptions { Temperature = 0.2f },
                ct);

            var json = response.Value.Content[0].Text.Trim();
            if (json.StartsWith("```")) json = json[json.IndexOf('\n')..];
            if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")];
            json = json.Trim();

            var result = JsonSerializer.Deserialize<ComplianceResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ComplianceResult(0.5, "Medium", ["Unable to parse response"], null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Compliance scoring failed");
            throw;
        }
    }
}
