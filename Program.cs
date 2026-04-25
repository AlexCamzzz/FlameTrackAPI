using FlameTrack.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // MongoDB Configuration - Strict for Production
        var mongoConnectionString = context.Configuration["MongoDBConnectionString"];
        
        if (string.IsNullOrEmpty(mongoConnectionString))
        {
            // Only use local fallback if not in production
            if (context.HostingEnvironment.IsDevelopment())
            {
                mongoConnectionString = "mongodb://127.0.0.1:27017";
            }
            else 
            {
                // In Azure, we MUST have a connection string
                throw new InvalidOperationException("MongoDBConnectionString is missing in Azure Environment Variables.");
            }
        }

        services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
    })
    .Build();

host.Run();
