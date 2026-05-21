using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Models.Entities;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace FlameTrack.API.Services;

public interface IAiService
{
    Task<AiResponseDto> AskAsync(string userId, string message);
}

public class AiService : IAiService
{
    private readonly IMongoCollection<UserEntity> _users;
    private readonly ITransactionService _transactionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiService> _logger;

    public AiService(IMongoClient mongoClient, ITransactionService transactionService, HttpClient httpClient, ILogger<AiService> logger)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _users = database.GetCollection<UserEntity>("Users");
        _transactionService = transactionService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiResponseDto> AskAsync(string userId, string message)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null)
            throw new Exception("User profile not found.");

        if (string.IsNullOrWhiteSpace(user.AiApiKey))
            throw new Exception("Intelligence link not active. Please provide an API Key and select a provider in Settings > Network.");

        var provider = (user.AiProvider ?? "openai").ToLower();
        var dashboard = await _transactionService.GetDashboardSummaryAsync(userId);
        
        var context = BuildContext(dashboard);
        var systemPrompt = "You are the FlameTrack Neural Advisor. Provide strategic financial insights based on the provided ledger data. Be precise, professional, and use a terminal-inspired tone. Focus on actionable optimizations.";

        return provider switch
        {
            "gemini" => await CallGeminiAsync(user.AiApiKey, systemPrompt, context, message),
            "claude" => await CallClaudeAsync(user.AiApiKey, systemPrompt, context, message),
            _ => await CallOpenAiAsync(user.AiApiKey, systemPrompt, context, message)
        };
    }

    private string BuildContext(DashboardSummaryDto dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("User Financial Context (Live Ledger):");
        sb.AppendLine($"- Total Balance: {dashboard.TotalBalance:N2}");
        sb.AppendLine($"- Monthly Income: {dashboard.MonthlyIncome:N2}");
        sb.AppendLine($"- Monthly Expenses: {dashboard.MonthlyExpenses:N2}");
        sb.AppendLine($"- Savings Rate: {dashboard.SavingsRate:F2}%");
        
        sb.AppendLine("\nAccounts Status:");
        foreach (var account in dashboard.Accounts)
        {
            sb.AppendLine($"- {account.Name}: {account.Balance:N2} ({account.Type})");
        }

        sb.AppendLine("\nSpending Intensity by Category (Current Month):");
        foreach (var category in dashboard.CategoryExpenses.Take(5))
        {
            sb.AppendLine($"- CategoryID {category.CategoryId}: {category.Amount:N2} ({category.Percentage:F2}%)");
        }
        return sb.ToString();
    }

    private async Task<AiResponseDto> CallOpenAiAsync(string apiKey, string systemPrompt, string context, string userQuery)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"{context}\n\nUser Query: {userQuery}" }
            },
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, "OpenAI", root => 
            root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString());
    }

    private async Task<AiResponseDto> CallGeminiAsync(string apiKey, string systemPrompt, string context, string userQuery)
    {
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = $"{systemPrompt}\n\n{context}\n\nUser Query: {userQuery}" } } }
            },
            generationConfig = new { temperature = 0.7 }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey.Trim()}";
        
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        
        return await HandleResponseAsync(response, "Gemini", root => 
        {
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                return "Signal blocked. The neural core refused to respond based on safety protocols.";
            
            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
                return "Signal lost. Candidate contains no valid data.";

            return parts[0].GetProperty("text").GetString();
        });
    }

    private async Task<AiResponseDto> CallClaudeAsync(string apiKey, string systemPrompt, string context, string userQuery)
    {
        var payload = new
        {
            model = "claude-3-5-sonnet-20240620",
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = $"{context}\n\nUser Query: {userQuery}" }
            },
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", apiKey.Trim());
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync(response, "Claude", root => 
            root.GetProperty("content")[0].GetProperty("text").GetString());
    }

    private async Task<AiResponseDto> HandleResponseAsync(HttpResponseMessage response, string providerName, Func<JsonElement, string?> extractor)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("{Provider} API Failure: {StatusCode} - {Error}", providerName, response.StatusCode, errorContent);
            
            string message = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Invalid API Key. Please check your settings.",
                System.Net.HttpStatusCode.Forbidden => "API Key does not have permission to use this model.",
                System.Net.HttpStatusCode.TooManyRequests => "Quota exceeded or rate limited. Try again later.",
                _ => $"{providerName} API Error: {response.StatusCode}. Verify your configuration."
            };
            
            throw new Exception(message);
        }

        try 
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var aiText = extractor(result);
            return new AiResponseDto { Response = aiText ?? "Signal lost. Could not decode neural response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse {Provider} response", providerName);
            throw new Exception($"Failed to decode response from {providerName}. Core desync detected.");
        }
    }
}
