using FlameTrack.API.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            
            // Global limiter: 100 requests per 1 minute
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Stricter limiter for Auth: 5 attempts per 30 seconds
            options.AddFixedWindowLimiter("auth-limiter", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromSeconds(30);
                opt.QueueLimit = 0;
            });
        });

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
        services.AddScoped<IDebtService, DebtService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<ISandboxService, SandboxService>();
    })
    .Build();

host.Run();
