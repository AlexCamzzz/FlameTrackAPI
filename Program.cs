using FlameTrack.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // MongoDB Configuration
        var mongoConnectionString = context.Configuration["MongoDBConnectionString"];
        if (string.IsNullOrEmpty(mongoConnectionString))
        {
            mongoConnectionString = "mongodb://localhost:27017"; // Fallback to avoid null exception
        }

        services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IGoalService, GoalService>();
    })
    .Build();

host.Run();

