using System.ComponentModel.Design;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gibbs2b.DtoGenerator;

public class Hello : IHost
{
    public Hello(IServiceProvider services)
    {
        Services = services;
    }

    public void Dispose()
    {
    }

    public Task StartAsync(CancellationToken cancellationToken = new())
    {
        var context = (DbContext) Services.GetRequiredService(GeneratorExtensions.ContextType);
        throw new GeneratorShutdownException(context);
    }

    public Task StopAsync(CancellationToken cancellationToken = new())
    {
        Console.WriteLine("Stop");
        return Task.CompletedTask;
    }

    public IServiceProvider Services { get; }
}

public class GeneratorShutdownException : Exception
{
    public DbContext Context { get; }

    public GeneratorShutdownException(DbContext context)
    {
        Context = context;
    }
}

public static class GeneratorExtensions
{
    public static bool Enabled { get; set; }
    public static Type ContextType { get; set; } = null!;

    public static void AddGenerator(this IServiceCollection builder)
    {
        if (Enabled)
            builder.Replace(ServiceDescriptor.Transient<IHost, Hello>());
    }
}