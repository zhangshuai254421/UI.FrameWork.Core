using Log.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddSerilogServices(this IServiceCollection services)
        {
            services.AddScoped<ISerilogService, SerilogService>();
            return services;
        }
    }
}
