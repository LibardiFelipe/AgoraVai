using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Jobs;
using AgoraVai.WebAPI.Repositories;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AgoraVai
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

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

            var cs = config.GetConnectionString("Postgres")!;
            builder.Services.AddScoped<IPaymentRepository>(_ =>
                new PaymentRepository(cs));

            builder.Services.AddHttpClients(config);
            builder.Services.AddScoped<IPaymentProcessingOrchestratorService, PaymentProcessingOrchestratorService>();

            var app = builder.Build();

            app.MapPost("/payments", async (
                [FromServices] ProcessorChannel channelManager,
                [FromBody] NewPaymentRequest request) =>
            {
                await channelManager.WriteAsync(request);
                return Results.Accepted();
            });

            app.MapGet("/payments-summary", async (
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                var payments = await paymentRepository.GetProcessorsSummaryAsync(from, to);

                var defaultPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "default") ?? new SummaryRowReadModel();
                var fabllbackPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "fallback") ?? new SummaryRowReadModel();

                return Results.Ok(new SummariesReadModel(
                    new SummaryReadModel(defaultPayment.TotalRequests, defaultPayment.TotalAmount),
                    new SummaryReadModel(fabllbackPayment.TotalRequests, fabllbackPayment.TotalAmount)));
            });

            app.MapPost("/purge-payments", async (
                [FromServices] IPaymentRepository paymentRepository) =>
            {
                await paymentRepository.PurgeAsync();
                return Results.Ok();
            });

            app.MapHealthChecks("/healthz");

            await app.RunAsync();
        }
    }

    [JsonSerializable(typeof(NewPaymentRequest))]
    [JsonSerializable(typeof(SummariesReadModel))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
