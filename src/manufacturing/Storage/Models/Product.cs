using MongoDB.Bson;

namespace manufacturing.Storage.Models
{
    public class Product
    {
        public ObjectId Id { get; set; }
        public ObjectId MarketingId { get; set; }
        public string Name { get; set; }
        public int Manufacturing { get; set; }
        public int Available { get; set; }
    }
}