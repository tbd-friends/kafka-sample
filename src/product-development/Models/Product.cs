using core.Infrastructure;
using MongoDB.Bson;

namespace product_development.Models
{
    [CollectionName("products")]
    public class Product
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }
}