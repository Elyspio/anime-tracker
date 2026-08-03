namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Recorded AniList replies. They pin the API's *shape* — field names, nesting, which fields come
///     back null — which is the thing a schema change upstream breaks and no hand-written payload
///     would catch. Re-record one by replaying the adapter's own query rather than editing it.
/// </summary>
public static class Fixtures
{
	public static string Read(string name)
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

		return File.ReadAllText(path);
	}
}
