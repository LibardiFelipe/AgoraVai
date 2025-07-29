using System.Text.Json.Serialization;

namespace AgoraVai.Metrics.WebAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, AppJsonSerializerContext.Default);
            });

            var app = builder.Build();
            app.MapGet("/payments-summary", () =>
            {
                return Results.Ok("Hello World!");
            });
            app.Run();
        }
    }

    [JsonSerializable(typeof(string))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
