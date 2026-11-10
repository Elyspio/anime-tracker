namespace AnimeTracker.Adapters.AniList.GraphQL;

/// <summary>
///     The main studios of one anime and nothing else. It exists for the animes stored before a studio
///     had an id: one small request names the studio, where refreshing the whole season would be the
///     only other way to learn it.
/// </summary>
internal static class MediaStudioQuery
{
	public const string Document = """
		query MediaStudio($id: Int) {
		  Media(id: $id, type: ANIME) {
		    id
		    studios(isMain: true) { nodes { id name } }
		  }
		}
		""";
}
