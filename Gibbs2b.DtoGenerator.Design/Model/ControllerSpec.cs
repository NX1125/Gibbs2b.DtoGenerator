using System.Reflection;
using Gibbs2b.DtoGenerator.Annotation;
using Microsoft.AspNetCore.Mvc;

namespace Gibbs2b.DtoGenerator.Model;

public class ControllerSpec
{
    public ProjectSpec Project { get; }
    public string? Area { get; set; }
    public string Name { get; set; }

    public TypescriptProjectSpec? TypescriptProject { get; set; }

    public ICollection<HandlerSpec> Handlers { get; set; }

    public ControllerSpec(Type type, ProjectSpec project, GenControllerAttribute attr)
    {
        if (!type.IsSubclassOf(typeof(ControllerBase)))
        {
            throw new ArgumentException($"Type {type.Name} is not a controller");
        }

        Project = project;
        Name = type.Name.Replace("Controller", string.Empty);

        TypescriptProject = attr.TypescriptProject != null
            ? project.FindTypescriptProjectByName(attr.TypescriptProject)
            : project.FindTypescriptProjectByNamespace(type.Namespace!);

        var area = type.GetCustomAttribute<AreaAttribute>();
        Area = area?.RouteValue;

        Handlers = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is { IsPublic: true, IsSpecialName: false, IsConstructor: false, IsStatic: false })
            .Where(m => m.GetCustomAttribute<GenHandlerAttribute>() != null)
            .Select(m => new HandlerSpec(m, this))
            .ToList();
    }
}

public class HandlerSpec
{
    private string? _route;

    public ControllerSpec Controller { get; set; }
    public NameSpec Name { get; set; }
    public string RouteName { get; set; }

    public bool IsPost { get; set; }
    public bool IsForm { get; set; }

    public string Route
    {
        get => _route ?? $"/{Controller.Area}/{Controller.Name}/{RouteName}".Replace("//", "/");
        set => _route = value;
    }

    public TsDtoModelSpec? Query { get; set; }
    public TsDtoModelSpec? Response { get; set; }

    public HandlerSpec(MethodInfo methodInfo, ControllerSpec controller)
    {
        Controller = controller;

        // supports only one parameter
        if (methodInfo.GetParameters().Length != 1)
        {
            throw new ArgumentException($"Method {methodInfo.Name} has more than one parameter");
        }

        var parameter = methodInfo
            .GetParameters()
            .First();

        RouteName = methodInfo.Name;
        IsPost = methodInfo.GetCustomAttribute<HttpPostAttribute>() != null;
        // whether the first parameter uses FromForm attribute
        IsForm = parameter.GetCustomAttribute<FromFormAttribute>() != null;

        Query = controller.Project.TsDto
            .SelectMany(d => d.Models)
            .Single(d => d.Type == parameter.ParameterType);

        var returnType = methodInfo.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // ActionResult<T>
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        Response = controller.Project.TsDto
            .SelectMany(d => d.Models)
            .Single(d => d.Type == returnType);

        Name = Response.Dto.DtoName;
    }
}