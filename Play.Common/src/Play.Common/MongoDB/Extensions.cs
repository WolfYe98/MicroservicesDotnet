using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Driver;
using Play.Common.Entities;
using Play.Common.Repositories;
using Play.Common.Settings;

namespace Play.Common.MongoDB
{
    public static class Extensions
    {
        public static IServiceCollection AddMongo(this IServiceCollection services)
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

            services.AddSingleton(provider =>
            {
                var configuration = provider.GetService<IConfiguration>(); // get the configuration service
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>(); // get the serviceSettings section.
                var mongodbConfiguration = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
                var mongoClient = new MongoClient(mongodbConfiguration.ConnectionString);
                return mongoClient.GetDatabase(serviceSettings.ServiceName);
            });// add a singleton object of mongo db so wherever is getting IMongoClient, is getting MongoClient object defined here

            return services;
        }
        public static IServiceCollection AddMongoRepository<T, TKey>(this IServiceCollection services, string collectionName) where T : IEntity<TKey>
        {
            //Now we want to add a singleton for the IRepository interface, this one is for items
            services.AddSingleton<IRepository<T, TKey>>(provider =>
            {
                var db = provider.GetService<IMongoDatabase>(); // we retrieve the db from services that we added before.
                return new MongoRepository<T, TKey>(db, collectionName);
            });
            return services;
        }
    }
}