namespace AnimeTracker.Api.Adapters.Rest.Utils.Extensions;

public static class EnumerableExtensions
{
	public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int size)
	{
		var list = source.ToList();
		for (var i = 0; i < list.Count; i += size)
		{
			yield return list.Skip(i).Take(size);
		}
	}
}