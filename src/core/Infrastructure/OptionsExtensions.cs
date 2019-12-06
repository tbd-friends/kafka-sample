using Microsoft.Extensions.Configuration;

namespace core.Infrastructure
{
    public static class OptionsExtensions
    {
        public static TOptions GetOptions<TOptions>(this IConfiguration configuration, string name)
            where TOptions : class, new()
        {
            var options = new TOptions();

            configuration.GetSection(name).Bind(options);

            return options;
        }
    }
}