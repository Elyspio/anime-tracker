using System.Reflection;

namespace AnimeTracker.Api.Web.Technical.Helpers;

/// <summary>
///     AssemblyHelper is a static helper class that provides methods for retrieving classes which  implement a specific
///     interface from a specified assembly.
/// </summary>
public static class AssemblyHelper
{
	/// <summary>
	///     Get the names of all classes in TAssembly which implement TInterface.
	/// </summary>
	/// <typeparam name="TAssembly">Any class within the target assembly.</typeparam>
	/// <typeparam name="TInterface">The interface that the classes should implement.</typeparam>
	/// <returns>The names of classes that implement the given interface.</returns>
	public static List<string> GetClassWithInterface<TAssembly, TInterface>()
	{
		var interfaceType = typeof(TInterface);

		return GetAllAssemblyTypes<TAssembly>().Where(type => interfaceType.IsAssignableFrom(type)).Select(t => t.Name).ToList();
	}


	/// <summary>
	///     Get a collection of all types in assemblies referenced by the assembly of TAssembly,
	///     including the Types in TAssembly itself.
	/// </summary>
	/// <typeparam name="TAssembly">Any class within the target assembly.</typeparam>
	/// <returns>All types in TAssembly and its referenced assemblies.</returns>
	private static IEnumerable<Type> GetAllAssemblyTypes<TAssembly>()
	{
		var mainAssembly = typeof(TAssembly).Assembly; // Get the main assembly

		var referencedAssemblies = mainAssembly.GetReferencedAssemblies()
			.Select(Assembly.Load)
			.ToList(); // Load all referenced assemblies

		referencedAssemblies.Add(mainAssembly); // Add the main assembly to the list


		return referencedAssemblies.Select(assembly => assembly.GetTypes()).SelectMany(s => s);
	}
}