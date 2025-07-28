using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Jobs;
using AgoraVai.WebAPI.Repositories;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json.Serialization;

namespace AgoraVai.WebAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AllowSynchronousIO = false;
                options.Limits.MaxConcurrentConnections = 1000;
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
            });

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddHealthChecks();
            builder.Services.AddSingleton<ProcessorChannel>();
            builder.Services.AddSingleton<PersistenceChannel>();
            builder.Services.AddHostedService<PaymentProcessingJob>();
            builder.Services.AddHostedService<PaymentPersistingJob>();

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

            builder.Services.AddHttpClients(config);
            builder.Services.AddScoped<IPaymentProcessingOrchestratorService, PaymentProcessingOrchestratorService>();

            var app = builder.Build();

            app.MapPost("/payments", async (
                [FromServices] ProcessorChannel channelManager,
                [FromBody] NewPaymentRequest request) =>
            {
                await channelManager.WriteAsync(request)
                    .ConfigureAwait(false);
                return Results.Accepted();
            });

            app.MapGet("/payments-summary", async (
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                return Results.Ok(
                    await paymentRepository.GetProcessorsSummaryAsync(from, to)
                        .ConfigureAwait(false));
            });

            app.MapPost("/purge-payments", async (
                [FromServices] IPaymentRepository paymentRepository) =>
            {
                await paymentRepository.PurgeAsync()
                    .ConfigureAwait(false);
                return Results.Ok();
            });

            app.MapHealthChecks("/healthz");

            app.Run();
        }
    }

    [JsonSerializable(typeof(NewPaymentRequest))]
    [JsonSerializable(typeof(SummariesReadModel))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
