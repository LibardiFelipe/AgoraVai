using AgoraVai.WebAPI.Services;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Diagnostics;

namespace AgoraVai.WebAPI.Utils
{
    public static class Extensions
    {
        public static long ElapsedMilliseconds(this long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000 / Stopwatch.Frequency;

        public static IServiceCollection AddHttpClients(
            this IServiceCollection services, IConfiguration config)
        {
            var defaultUrl = config.GetRequiredSection("PaymentProcessors:Default:BaseUrl").Value!;
            var fallbackUrl = config.GetRequiredSection("PaymentProcessors:Fallback:BaseUrl").Value!;

            services.AddHttpClient<DefaultPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(defaultUrl);
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(DefaultRetryPolicy);

            services.AddHttpClient<FallbackPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(fallbackUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(FallbackRetryPolicy);

            var otherServiceUrl = config.GetConnectionString("OtherService")!;
            services.AddHttpClient("cross-comm",
                client =>
                {
                    client.BaseAddress = new Uri(otherServiceUrl);
                    client.Timeout = TimeSpan.FromSeconds(5);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(DefaultRetryPolicy);

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> DefaultRetryPolicy
        {
            get
            {
                var backoffDelay = Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromSeconds(1.5),
                    retryCount: 3);

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(backoffDelay);
            }
        }

        private static IAsyncPolicy<HttpResponseMessage> FallbackRetryPolicy
        {
            get
            {
                var backoffDelay = Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromSeconds(1.5),
                    retryCount: 5);

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(backoffDelay);
            }
        }
    }
}
