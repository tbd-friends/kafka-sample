using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using core.console;
using core.Infrastructure;
using core.Kafka;
using core.Storage;
using marketing.Consumer.Models;
using marketing.Publishing;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using StorageProduct = marketing.Storage.Models.Product;

namespace marketing
{
    class Program
    {
        private static MongoDataStore _store;
        private static Publisher _publisher;

        public static void Main(string[] args)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            var configuration = Configure();

            var consumers = StartListeningToTopics(configuration, source.Token);

            var console = new ConsoleMenuInput(new Menu()
            {
                Items = new List<MenuItem>
                {
                    new MenuItem {Name = "List Products Available", OnSelected = ListProducts},
                    new MenuItem {Name = "Configure Product", OnSelected = ConfigureProduct},
                    new MenuItem {Name = "Publish Product", OnSelected = PublishProduct }
                }
            }).Start(source.Token);

            Task.WaitAll(consumers.Union(new[] { console }).ToArray()); // This will never end, bad cancellation token code
        }

        private static Task[] StartListeningToTopics(IConfigurationRoot configuration, CancellationToken token)
        {
            var products = new ConsumerProcess<Null, Product>(configuration.GetOptions<KafkaOptions>("kafka"));

            products.OnMessageConsumed += ProductsOnOnMessageConsumed;

            Task[] consumers = {
                products.Consume("products", token)
            };

            return consumers;
        }

        private static IConfigurationRoot Configure()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _store = new MongoDataStore(new DataStoreConfiguration
            {
                ConnectionString = configuration.GetConnectionString("mongo"),
                DatabaseName = configuration["database"]
            });

            _publisher = new Publisher(configuration.GetOptions<KafkaOptions>("kafka"));

            return configuration;
        }

        private static async Task ProductsOnOnMessageConsumed(Message<Null, Product> message)
        {
            await _store.Add(new StorageProduct
            {
                Name = message.Value.Name,
                Description = message.Value.Description,
                Category = message.Value.Category,
                DevelopmentId = message.Value.Id,
            });
        }

        private static async Task ListProducts()
        {
            var entries = await _store.GetAll<StorageProduct>();

            Console.WriteLine();

            foreach (var product in entries)
            {
                Console.WriteLine($"[{product.Category}] {product.Name} - {product.Description} ({product.Id})");
            }

            Console.WriteLine();
        }

        private static async Task ConfigureProduct()
        {
            await ListProducts();

            Console.Write("Enter a product id: ");
            string id = Console.ReadLine();

            var product = await _store.Get<StorageProduct>(p => p.Id == ObjectId.Parse(id));

            if (product != null)
            {
                Console.Write("Enter Target Demographics: ");
                string demographics = Console.ReadLine();
                Console.Write("Enter Skus: ");
                string skus = Console.ReadLine();

                await _store.Update(f => f.Id == ObjectId.Parse(id),
                    Builders<StorageProduct>.Update.Set(f => f.TargetDemographics, demographics.Split(','))
                        .Set(f => f.Skus, skus.Split(',')));
            }
        }

        private static async Task PublishProduct()
        {
            await ListProducts();

            Console.Write("Enter a product id: ");
            string id = Console.ReadLine();

            var product = await _store.Get<StorageProduct>(p => p.Id == ObjectId.Parse(id));

            await _publisher.Publish(new PublishProduct
            {
                Name = product.Name,
                Description = product.Description,
                MarketingId = product.Id,
                Skus = product.Skus,
                Category = product.Category
            }, "marketing-products");
        }
    }
}
