using System;
using System.Threading.Tasks;

namespace core.console
{
    public class MenuItem
    {
        public string Name { get; set; }
        public Func<Task> OnSelected { get; set; }
    }
}