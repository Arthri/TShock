using System.Reflection;
using System.Runtime.Loader;

Dictionary<string, Assembly> _cache = new Dictionary<string, Assembly>();

AssemblyLoadContext.Default.Resolving += Default_Resolving;

TerrariaApi.Server.Program.Main(args);

/// <summary>
/// Resolves a module from the ./bin folder, either with a .dll by preference or .exe
/// </summary>
Assembly? Default_Resolving(System.Runtime.Loader.AssemblyLoadContext arg1, AssemblyName arg2)
{
	if (arg2?.Name is null) return null;
	if (_cache.TryGetValue(arg2.Name, out Assembly? asm) && asm is not null) return asm;

	var loc = Path.Combine(AppContext.BaseDirectory, "bin", arg2.Name + ".dll");
	if (File.Exists(loc))
		asm = arg1.LoadFromAssemblyPath(loc);

	loc = Path.ChangeExtension(loc, ".exe");
	if (File.Exists(loc))
		asm = arg1.LoadFromAssemblyPath(loc);

	if(asm is not null)
		_cache[arg2.Name] = asm;

	return asm;
}
