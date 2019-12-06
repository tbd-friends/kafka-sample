using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.console;
using core.Infrastructure;
using core.Kafka;
using core.Storage;
using Microsoft.Extensions.Configuration;
using product_development.Models;

namespace product_development
{
    class Program
    {
        private static MongoDataStore _store;
        private static Publisher _publisher;

        static void Main(string[] args)
        {
            var configuration = Configure();

            _publisher = new Publisher(configuration.GetOptions<KafkaOptions>("kafka"));

            CancellationTokenSource source = new CancellationTokenSource();

            ConsoleMenuInput menu = new ConsoleMenuInput(new Menu()
            {
                Items = new List<MenuItem>
                {
                    new MenuItem {Name = "Add Product", OnSelected = AddNewProduct}
                }
            });

            Task.WaitAll(menu.Start(source.Token));
        }

        private static IConfiguration Configure()
        {
            var configuration = GetConfiguration();

            _store = new MongoDataStore(new DataStoreConfiguration
            {
                ConnectionString = configuration.GetConnectionString("mongo"),
                DatabaseName = configuration["database"]
            });
            return configuration;
        }

        public static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public static async Task AddNewProduct()
        {
            Console.WriteLine("Adding New Product");
            Console.WriteLine("=============================");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Description: ");
            string description = Console.ReadLine();

            Console.Write("Category: ");
            string category = Console.ReadLine();

            var product = new Product
            {
                Name = name,
                Description = description,
                Category = category
            };

            await _store.Add(product);

            await _publisher.Publish(product);
        }
    }
}
