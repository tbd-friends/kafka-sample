using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using core.console;
using core.Infrastructure;
using core.Kafka;
using core.Storage;
using manufacturing.Consumer.Models;
using manufacturing.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;
using StorageProduct = manufacturing.Storage.Models.Product;

namespace manufacturing
{
    class Program
    {
        private static MongoDataStore _store;
        private static Publisher _publisher;

        static void Main(string[] args)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            var configuration = Configure();

            var consumers = StartListeningToTopics(configuration, source.Token);

            var console = new ConsoleMenuInput(new Menu()
            {
                Items = new List<MenuItem>
                {
                    new MenuItem {Name = "List Product", OnSelected = ListProducts},
                    new MenuItem { Name = "Produce Inventory", OnSelected = ProduceInventory }
                }
            }).Start(source.Token);

            Task.WaitAll(consumers.Union(new[] { console }).ToArray()); // This will never end, bad cancellation token code
        }

        private static Task[] StartListeningToTopics(IConfigurationRoot configuration, CancellationToken token)
        {
            var products = new ConsumerProcess<Null, Product>(configuration.GetOptions<KafkaOptions>("kafka"));

            products.OnMessageConsumed += ProductsOnMessageConsumed;

            Task[] consumers = {
                products.Consume("marketing-products", token)
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

        private static async Task ProductsOnMessageConsumed(Message<Null, Product> message)
        {
            await _store.Add(new StorageProduct
            {
                MarketingId = ObjectId.Parse(message.Value.MarketingId),
                Name = message.Value.Name,
                Available = 0,
                Manufacturing = 0
            });
        }

        private static async Task ListProducts()
        {
            var entries = await _store.GetAll<StorageProduct>();

            Console.WriteLine();

            foreach (var product in entries)
            {
                Console.WriteLine(
                    $"[{product.Id}] {product.Name} - Available: {product.Available} Manufacture: {product.Manufacturing}");
            }

            Console.WriteLine();
        }

        private static async Task ProduceInventory()
        {
            await ListProducts();

            Console.Write("Enter Product to Produce: ");
            var productId = Console.ReadLine();

            var product = await _store.Get<StorageProduct>(p => p.Id == ObjectId.Parse(productId));

            Console.Write("How many to produce: ");
            string toProduce = Console.ReadLine();

            if (int.TryParse(toProduce, out int quantity))
            {
                await _store.Update(p => p.Id == ObjectId.Parse(productId),
                    Builders<StorageProduct>.Update.Set(f => f.Available, quantity));

                await _publisher.Publish(new Stock
                {
                    Available = quantity,
                    MarketingId = product.MarketingId.ToString()
                }, "manufacturing-stock");
            }
        }
    }
}
