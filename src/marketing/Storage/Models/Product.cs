using System.Collections.Generic;
using core.Infrastructure;
using MongoDB.Bson;

namespace marketing.Storage.Models
{
    [CollectionName("products")]
    public class Product
    {
        public ObjectId Id { get; set; }
        public string DevelopmentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public IEnumerable<string> TargetDemographics { get; set; }
        public IEnumerable<string> Skus { get; set; }
    }
}