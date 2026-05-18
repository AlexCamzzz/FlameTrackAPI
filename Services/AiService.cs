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
            throw new Exception("Neural Link not active. Please provide your OpenAI API Key in Settings > Network.");

        _logger.LogInformation("Generating financial context for user {UserId}", userId);
        var dashboard = await _transactionService.GetDashboardSummaryAsync(userId);
        
        var context = new StringBuilder();
        context.AppendLine("User Financial Context (Live Ledger):");
        context.AppendLine($"- Total Balance: {dashboard.TotalBalance:N2}");
        context.AppendLine($"- Monthly Income: {dashboard.MonthlyIncome:N2}");
        context.AppendLine($"- Monthly Expenses: {dashboard.MonthlyExpenses:N2}");
        context.AppendLine($"- Savings Rate: {dashboard.SavingsRate:F2}%");
        
        context.AppendLine("\nAccounts Status:");
        foreach (var account in dashboard.Accounts)
        {
            context.AppendLine($"- {account.Name}: {account.Balance:N2} ({account.Type})");
        }

        context.AppendLine("\nSpending Intensity by Category (Current Month):");
        foreach (var category in dashboard.CategoryExpenses.Take(5))
        {
            context.AppendLine($"- CategoryID {category.CategoryId}: {category.Amount:N2} ({category.Percentage:F2}%)");
        }

        var promptPayload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are the FlameTrack Neural Advisor. Provide strategic financial insights based on the provided ledger data. Be precise, professional, and use a terminal-inspired tone. Focus on actionable optimizations." },
                new { role = "user", content = $"{context}\n\nUser Query: {message}" }
            },
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AiApiKey.Trim());
        request.Content = JsonContent.Create(promptPayload);

        _logger.LogInformation("Dispatching request to OpenAI for user {UserId}", userId);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API Failure: {StatusCode} - {Error}", response.StatusCode, errorContent);
            
            // Extract cleaner error if possible
            try {
                var errorDoc = JsonDocument.Parse(errorContent);
                var cleanMsg = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                throw new Exception($"OpenAI Intelligence Error: {cleanMsg}");
            } catch {
                throw new Exception($"OpenAI API reported a {response.StatusCode}. Verify your API key and quota.");
            }
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var aiText = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return new AiResponseDto { Response = aiText ?? "Signal lost. Could not decode neural response." };
    }
}
