using System;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;

namespace retail.Storage.Models
{
    public class Product
    {
        public ObjectId Id { get; set; }
        public ObjectId MarketingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> Skus { get; set; }
        public int AvailableStock { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
    }
}