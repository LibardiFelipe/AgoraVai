using System.Text.Json.Serialization;

namespace AgoraVai
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
            app.Run();
        }
    }

    [JsonSerializable(typeof(object))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
