using Gibbs2b.DtoGenerator.Design.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gibbs2b.DtoGenerator;

public class Generator
{
    internal static ILogger<Generator> Logger { get; private set; }

    private SolutionSpec _solution;
    private readonly TsGenerator _tsGenerator;
    private readonly ILogger<Generator> _logger;

    public Generator(SolutionSpec solution,
        TsGenerator tsGenerator,
        ILogger<ProjectSpec> logger2,
        ILogger<Generator> logger)
    {
        _solution = solution;
        _tsGenerator = tsGenerator;
        _logger = logger;
    }

    public void Run(string? path, string[] args)
    {
        _solution.Path = path ?? _solution.Path;

        _logger.LogInformation($"Loading solution from {_solution.Path}");
        Logger = _logger;

        _solution.Project.Load(_logger);

        GenerateTs();
    }

    private void GenerateTs()
    {
        _tsGenerator.Generate();
    }

    public static void Run(Func<SolutionSpec> solution, string[] args)
    {
        var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(c => { c.AddJsonFile("appsettings.json"); })
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ => solution());
                services.AddSingleton<TsGenerator>();
                services.AddSingleton<Generator>();
            })
            .Build();

        host.Services
            .GetRequiredService<Generator>()
            .Run(args.Length > 0 ? args[0] : null, args);
    }
}