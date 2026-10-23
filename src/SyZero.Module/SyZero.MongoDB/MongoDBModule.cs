using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace SyZero.MongoDB
{
    public static class MongoDBModule
    {
        public static IServiceCollection AddMongoDBModule(this IServiceCollection builder, IConfiguration configuration)
        {
            return builder.AddSyZeroMongoDB(configuration);
        }
    }
}
