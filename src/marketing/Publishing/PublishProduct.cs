using System.Collections.Generic;
using MongoDB.Bson;

namespace marketing.Publishing
{
    public class PublishProduct
    {
        public ObjectId MarketingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public IEnumerable<string> Skus { get; set; }
    }
}