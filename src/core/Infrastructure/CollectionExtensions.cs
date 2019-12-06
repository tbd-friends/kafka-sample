using System;
using System.Reflection;

namespace core.Infrastructure
{
    public static class CollectionExtensions
    {
        public static string CollectionName(this Type type)
        {
            return (type.GetCustomAttribute(typeof(CollectionNameAttribute)) as CollectionNameAttribute)
                   ?.CollectionName ?? type.Name;
        }
    }
}