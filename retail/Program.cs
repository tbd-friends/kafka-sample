using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using core.console;
using core.Infrastructure;
using core.Kafka;
using core.Storage;
using manufacturing.Publishing;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using retail.Consumer.Models;
using StorageProduct = retail.Storage.Models.Product;

namespace retail
{
    class Program
    {
        private static MongoDataStore _store;

        static void Main(string[] args)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            var configuration = Configure();

            var consumers = StartListeningToTopics(configuration, source.Token);

            var console = new ConsoleMenuInput(new Menu()
            {
                Items = new List<MenuItem>
                {
                    new MenuItem {Name = "List Product", OnSelected = ListProducts}
                }
            }).Start(source.Token);

            Task.WaitAll(consumers.Union(new[] { console }).ToArray()); // This will never end, bad cancellation token code
        }

        private static Task[] StartListeningToTopics(IConfigurationRoot configuration, CancellationToken token)
        {
            var products = new ConsumerProcess<Null, Product>(configuration.GetOptions<KafkaOptions>("kafka"));
            var stock = new ConsumerProcess<Null, Stock>(configuration.GetOptions<KafkaOptions>("kafka"));

            stock.OnMessageConsumed += StockOnMessageConsumed;
            products.OnMessageConsumed += ProductsOnMessageConsumed;

            Task[] consumers = {
                products.Consume("marketing-products", token),
                stock.Consume("manufacturing-stock", token)
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

            //_publisher = new Publisher(configuration.GetOptions<KafkaOptions>("kafka"));

            return configuration;
        }

        private static async Task ProductsOnMessageConsumed(Message<Null, Product> message)
        {
            var product =
                await _store.Get<StorageProduct>(p => p.MarketingId == ObjectId.Parse(message.Value.MarketingId));

            if (product == null)
            {
                await _store.Add(new StorageProduct
                {
                    MarketingId = ObjectId.Parse(message.Value.MarketingId),
                    Name = message.Value.Name,
                    Description = message.Value.Description,
                    Skus = message.Value.Skus
                });
            }
            else
            {
                await _store.Update(f => f.MarketingId == ObjectId.Parse(message.Value.MarketingId),
                    Builders<StorageProduct>.Update
                        .Set(f => f.Name, message.Value.Name)
                        .Set(f => f.Description, message.Value.Description)
                        .Set(f => f.Skus, message.Value.Skus));
            }
        }

        private static async Task StockOnMessageConsumed(Message<Null, Stock> message)
        {
            var product =
                await _store.Get<StorageProduct>(p => p.MarketingId == ObjectId.Parse(message.Value.MarketingId));

            if (product == null)
            {
                await _store.Add(new StorageProduct
                {
                    MarketingId = ObjectId.Parse(message.Value.MarketingId),
                    AvailableStock = message.Value.Available
                });
            }
            else
            {
                await _store.Update(f => f.MarketingId == ObjectId.Parse(message.Value.MarketingId),
                    Builders<StorageProduct>.Update.Set(f => f.AvailableStock, message.Value.Available));
            }
        }

        private static async Task ListProducts()
        {
            var entries = await _store.GetAll<StorageProduct>();

            Console.WriteLine();

            foreach (var product in entries)
            {
                Console.WriteLine(
                    $"[{product.Id}] {product.Name} - Available: {product.AvailableStock} Price{product.Price}");
            }

            Console.WriteLine();
        }
    }
}
