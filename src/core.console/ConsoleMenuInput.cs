using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace core.console
{
    public class ConsoleMenuInput
    {
        private readonly Menu _menu;
        private bool _exiting;

        public ConsoleMenuInput(Menu menu)
        {
            _menu = menu;
            _exiting = false;
        }

        public Task Start(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && !_exiting)
                {
                    var itemsToSelectFrom = Enumerable.Select(_menu.Items, (element, i) => new { Index = i, Value = element })
                        .ToDictionary(k => $"{k.Index}", k => k.Value);

                    foreach (var item in itemsToSelectFrom)
                    {
                        Console.WriteLine($"[{item.Key}] {item.Value.Name}");
                    }

                    Console.Write("Select menu option: ");
                    string input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input))
                    {
                        if (itemsToSelectFrom.ContainsKey(input))
                        {
                            await itemsToSelectFrom[input].OnSelected();
                        }
                        else
                        {
                            if (input.Equals("q", StringComparison.CurrentCultureIgnoreCase))
                            {
                                _exiting = true;
                            }
                        }
                    }
                }
            }, token);
        }
    }
}