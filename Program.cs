using AgoraVai.Channels;
using AgoraVai.Jobs;
using AgoraVai.Requests;
using AgoraVai.Services;
using AgoraVai.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AgoraVai
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddSingleton<ProcessorChannel>();
            builder.Services.AddSingleton<PersistenceChannel>();
            builder.Services.AddHostedService<PaymentProcessingJob>();
            builder.Services.AddHostedService<PaymentPersistingJob>();

            builder.Services.AddHttpClients(config);
            builder.Services.AddScoped<IPaymentProcessingOrchestratorService, PaymentProcessingOrchestratorService>();

            var app = builder.Build();

            app.MapPost("/payments", async (
                [FromBody] NewPaymentRequest request,
                [FromServices] ProcessorChannel channelManager) =>
            {
                await channelManager.WriteAsync(request);
                return Results.Accepted();
            });

            app.Run();
        }
    }

    [JsonSerializable(typeof(NewPaymentRequest))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
