using System;

namespace core.Infrastructure
{
    public class CollectionNameAttribute : Attribute
    {
        public string CollectionName { get; }

        public CollectionNameAttribute(string name)
        {
            CollectionName = name;
        }
    }
}