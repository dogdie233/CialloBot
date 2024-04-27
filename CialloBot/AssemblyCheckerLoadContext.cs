using System.Reflection;
using System.Runtime.Loader;

namespace CialloBot;

public class AssemblyCheckerLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver resolver;

    public AssemblyCheckerLoadContext(string assemblyPath) : base(true)
    {
        resolver = new(assemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, assembly.GetName(false)))
                return assembly;  // Loaded in default context
        }

        var dependencyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (dependencyPath != null)
            return LoadFromAssemblyPath(dependencyPath);

        return null;
    }
}
