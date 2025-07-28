using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Jobs;
using AgoraVai.WebAPI.Models;
using AgoraVai.WebAPI.Repositories;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime;
using System.Text.Json.Serialization;

namespace AgoraVai.WebAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            builder.WebHost.ConfigureKestrel(options =>
            {
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
            builder.Services.AddSingleton<ProcessingChannel>();
            builder.Services.AddSingleton<PersistenceChannel>();
            builder.Services.AddHostedService<PaymentProcessingJob>();
            builder.Services.AddHostedService<PaymentPersistingJob>();

            var cfg1 = config.GetRequiredSection("JobsConfig:ProcessingBatchSize").Get<int>();
            var cfg2 = config.GetRequiredSection("JobsConfig:ProcessingParalellism").Get<int>();
            var cfg3 = config.GetRequiredSection("JobsConfig:ProcessingWaitMs").Get<int>();
            var cfg4 = config.GetRequiredSection("JobsConfig:PersistenceBatchSize").Get<int>();
            var cfg5 = config.GetRequiredSection("JobsConfig:PersistenceWaitMs").Get<int>();
            builder.Services.AddSingleton(new JobsConfig
            {
                ProcessingBatchSize = cfg1,
                ProcessingParalellism = cfg2,
                ProcessingWaitMs = cfg3,
                PersistenceBatchSize = cfg4,
                PersistenceWaitMs = cfg5
            });

            var cs = config.GetConnectionString("Postgres")!;
            builder.Services.AddScoped<IPaymentRepository>(_ =>
                new PaymentRepository(cs));

            builder.Services.AddHttpClients(config);
            builder.Services.AddScoped<IPaymentProcessingOrchestratorService, PaymentProcessingOrchestratorService>();

            var app = builder.Build();

            app.MapPost("/payments", async (
                [FromServices] ProcessingChannel processingChannel,
                [FromBody] NewPaymentRequest request) =>
            {
                await processingChannel.WriteAsync(request)
                    .ConfigureAwait(false);
                return Results.Accepted();
            });

            app.MapGet("/payments-summary", async (
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                var payments = await paymentRepository.GetProcessorsSummaryAsync(from, to)
                    .ConfigureAwait(false);

                var defaultPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "default")
                        ?? new SummaryRowReadModel();
                var fabllbackPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "fallback")
                        ?? new SummaryRowReadModel();

                return Results.Ok(new SummariesReadModel(
                    new SummaryReadModel(defaultPayment.TotalRequests, defaultPayment.TotalAmount),
                    new SummaryReadModel(fabllbackPayment.TotalRequests, fabllbackPayment.TotalAmount)));
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

    [JsonSerializable(typeof(JobsConfig))]
    [JsonSerializable(typeof(NewPaymentRequest))]
    [JsonSerializable(typeof(SummariesReadModel))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
