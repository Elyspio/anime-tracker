namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Pages recorded from nautiljon.com through FlareSolverr. They are the only thing that catches
///     a markup change upstream, so they are stored verbatim rather than trimmed to the nodes the
///     assemblers happen to read today.
/// </summary>
public static class Fixtures
{
	public static string Read(string name)
	{
		return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
	}
}
