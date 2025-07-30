using AgoraVai.Shared.Configs;
using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Jobs;
using AgoraVai.WebAPI.Models;
using AgoraVai.WebAPI.Publishers;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
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
            builder.Services.AddSingleton<Publisher>();

            var brokerHost = config.GetRequiredSection("NetMQ:Host").Get<string>()!;
            var brokerPort = config.GetRequiredSection("NetMQ:Port").Get<int>();
            var brokerTopic = config.GetRequiredSection("NetMQ:Topic").Get<string>()!;
            builder.Services.AddSingleton(new BrokerConfig
            {
                Host = brokerHost,
                Port = brokerPort,
                Topic = brokerTopic
            });

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

            builder.Services.AddHttpClients(config);
            builder.Services.AddScoped<PaymentProcessingOrchestratorService>();

            var app = builder.Build();

            app.MapPost("/payments", async (
                [FromServices] ProcessingChannel processingChannel,
                [FromBody] NewPaymentRequest request) =>
            {
                await processingChannel.WriteAsync(request)
                    .ConfigureAwait(false);
                return Results.Accepted();
            });

            app.MapHealthChecks("/healthz");

            app.Run();
        }
    }

    [JsonSerializable(typeof(NewPaymentRequest))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
