using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Jobs;
using AgoraVai.WebAPI.Repositories;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.Net;
using System.Runtime;
using System.Text.Json.Serialization;
using System.Web;

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
                options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
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

            //var cs = config.GetConnectionString("Postgres")!;
            builder.Services.AddSingleton<IPaymentRepository>(_ =>
                new InMemoryPaymentRepository());

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

            app.MapGet("/internal/payments-summary", async (
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

                return Results.Ok(new SummariesReadModel
                {
                    Default = new SummaryReadModel
                    {
                        TotalRequests = defaultPayment.TotalRequests,
                        TotalAmount = defaultPayment.TotalAmount
                    },
                    Fallback = new SummaryReadModel
                    {
                        TotalRequests = fabllbackPayment.TotalRequests,
                        TotalAmount = fabllbackPayment.TotalAmount
                    }
                });
            });

            app.MapGet("/payments-summary", async (
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                if (from.HasValue)
                    queryParams["from"] = from.Value.ToString("O");
                if (to.HasValue)
                    queryParams["to"] = to.Value.ToString("O");

                using var httpClient = httpClientFactory.CreateClient("cross-comm");

                var relativeUri = new Uri(
                    $"internal/payments-summary?{queryParams}", UriKind.Relative);
                var externalResult = await httpClient.GetFromJsonAsync(
                    relativeUri, AppJsonSerializerContext.Default.SummariesReadModel)
                    .ConfigureAwait(false);

                externalResult ??= new SummariesReadModel();

                var localPayments = await paymentRepository.GetProcessorsSummaryAsync(from, to)
                    .ConfigureAwait(false);

                var localDefaultPayment = localPayments
                    .FirstOrDefault(p => p.ProcessedBy == "default")
                        ?? new SummaryRowReadModel();
                var localFallbackPayment = localPayments
                    .FirstOrDefault(p => p.ProcessedBy == "fallback")
                        ?? new SummaryRowReadModel();

                var combinedResult = new SummariesReadModel
                {
                    Default = new SummaryReadModel
                    {
                        TotalRequests = localDefaultPayment.TotalRequests + externalResult.Default.TotalRequests,
                        TotalAmount = localDefaultPayment.TotalAmount + externalResult.Default.TotalAmount
                    },
                    Fallback = new SummaryReadModel
                    {
                        TotalRequests = localFallbackPayment.TotalRequests + externalResult.Fallback.TotalRequests,
                        TotalAmount = localFallbackPayment.TotalAmount + externalResult.Fallback.TotalAmount
                    }
                };

                return Results.Ok(combinedResult);
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
