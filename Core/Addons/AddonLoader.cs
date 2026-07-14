using DeskNotes.Abstractions;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace DeskNotes.Core.Addons;

public sealed class AddonLoader
{
    private readonly HashSet<string> _loadedAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IDeskNotesAddon> LoadAll(string addonsDirectory, AddonSettingsStore settings)
    {
        var addons = new List<IDeskNotesAddon>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in DiscoverAssemblies(addonsDirectory))
        {
            if (assembly.FullName == null || !_loadedAssemblyNames.Add(assembly.FullName))
                continue;

            foreach (var addon in CreateAddonsFromAssembly(assembly))
            {
                if (!settings.IsEnabled(addon.Manifest.Id))
                    continue;

                if (!seenIds.Add(addon.Manifest.Id))
                    continue;

                addons.Add(addon);
            }
        }

        return addons;
    }

    private IEnumerable<Assembly> DiscoverAssemblies(string addonsDirectory)
    {
        var results = new List<Assembly>();

        // Built-in addon assemblies (copied to Addons folder at build time)
        foreach (var name in new[] { "DeskNotes.Addon.Export", "DeskNotes.Addon.Disco", "DeskNotes.Addon.Confetti", "DeskNotes.Addon.Timer" })
        {
            var dllPath = Path.Combine(addonsDirectory, $"{name}.dll");
            if (!File.Exists(dllPath))
                continue;

            if (_loadedAssemblyPaths.Contains(dllPath))
                continue;

            try
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                _loadedAssemblyPaths.Add(dllPath);
                results.Add(asm);
            }
            catch
            {
                // skip broken addon DLL
            }
        }

        if (!Directory.Exists(addonsDirectory))
            return results;

        foreach (var dll in Directory.GetFiles(addonsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            if (_loadedAssemblyPaths.Contains(dll))
                continue;

            try
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                _loadedAssemblyPaths.Add(dll);
                results.Add(asm);
            }
            catch
            {
                // skip broken addon DLL
            }
        }

        return results;
    }

    private static IEnumerable<IDeskNotesAddon> CreateAddonsFromAssembly(Assembly assembly)
    {
        IEnumerable<Type> types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>();
        }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            if (!typeof(IDeskNotesAddon).IsAssignableFrom(type))
                continue;

            IDeskNotesAddon? addon;

            try
            {
                addon = Activator.CreateInstance(type) as IDeskNotesAddon;
            }
            catch
            {
                continue;
            }

            if (addon != null)
                yield return addon;
        }
    }
}