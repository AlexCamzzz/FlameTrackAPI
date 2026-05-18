using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Models.Entities;
using MongoDB.Driver;

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

    public AiService(IMongoClient mongoClient, ITransactionService transactionService, HttpClient httpClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _users = database.GetCollection<UserEntity>("Users");
        _transactionService = transactionService;
        _httpClient = httpClient;
    }

    public async Task<AiResponseDto> AskAsync(string userId, string message)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null || string.IsNullOrEmpty(user.AiApiKey))
        {
            throw new Exception("AI API Key not configured. Please add your OpenAI key in settings.");
        }

        var dashboard = await _transactionService.GetDashboardSummaryAsync(userId);
        
        var context = new StringBuilder();
        context.AppendLine("User Financial Context:");
        context.AppendLine($"- Total Balance: {dashboard.TotalBalance:C}");
        context.AppendLine($"- Monthly Income: {dashboard.MonthlyIncome:C}");
        context.AppendLine($"- Monthly Expenses: {dashboard.MonthlyExpenses:C}");
        context.AppendLine($"- Savings Rate: {dashboard.SavingsRate:F2}%");
        
        context.AppendLine("\nAccounts:");
        foreach (var account in dashboard.Accounts)
        {
            context.AppendLine($"- {account.Name}: {account.Balance:C} ({account.Type})");
        }

        context.AppendLine("\nTop Expenses this Month:");
        foreach (var category in dashboard.CategoryExpenses.Take(5))
        {
            context.AppendLine($"- Category {category.CategoryId}: {category.Amount:C} ({category.Percentage:F2}%)");
        }

        var prompt = new
        {
            model = "gpt-4o-mini", // Cost-effective model
            messages = new[]
            {
                new { role = "system", content = "You are a professional financial advisor. Analyze the user's data and provide concise, actionable advice. Be encouraging but realistic." },
                new { role = "user", content = $"{context}\n\nUser Question: {message}" }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {user.AiApiKey}");
        request.Content = JsonContent.Create(prompt);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API Error: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var aiText = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return new AiResponseDto { Response = aiText ?? "I couldn't generate a response." };
    }
}
