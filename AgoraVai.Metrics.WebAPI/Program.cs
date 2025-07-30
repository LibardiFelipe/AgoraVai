using AgoraVai.Metrics.WebAPI.Jobs;
using AgoraVai.Metrics.WebAPI.Repositories;
using AgoraVai.Shared.Configs;
using Microsoft.AspNetCore.Mvc;
using System.Runtime;
using System.Text.Json.Serialization;

namespace AgoraVai.Metrics.WebAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddHealthChecks();

            var brokerHost = config.GetRequiredSection("NetMQ:Host").Get<string>()!;
            var brokerPort = config.GetRequiredSection("NetMQ:Port").Get<int>();
            var brokerTopic = config.GetRequiredSection("NetMQ:Topic").Get<string>()!;
            builder.Services.AddSingleton(new BrokerConfig
            {
                Host = brokerHost,
                Port = brokerPort,
                Topic = brokerTopic
            });

            builder.Services.AddHostedService<PaymentMetricsJob>();
            builder.Services.AddSingleton<PaymentRepository>();

            var app = builder.Build();

            app.MapGet("/payments-summary", (
                [FromServices] PaymentRepository repository,
                [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
                    Results.Ok(repository.GetSummary(from, to)));

            app.MapPost("/purge-payments", ([FromServices] PaymentRepository repository) =>
            {
                repository.Purge();
                return Results.Ok();
            });

            app.MapHealthChecks("/healthz");
            app.Run();
        }
    }

    [JsonSerializable(typeof(PaymentStats))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
