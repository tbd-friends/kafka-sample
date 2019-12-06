using System.Collections.Generic;

namespace manufacturing.Consumer.Models
{
    public class Product
    {
        public string MarketingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public IEnumerable<string> Skus { get; set; }
    }
}
