namespace AnimeTracker.Adapters.AniList.GraphQL;

/// <summary>
///     A studio and the works it was the main studio of, one page at a time. No airing schedule: a
///     filmography lists works, it does not predict them, and the schedule is what makes a season query
///     heavy. The order asked for is only a paging order — the application sorts what it shows.
/// </summary>
internal static class StudioQuery
{
	public const string Document = """
		query Studio($id: Int, $page: Int, $perPage: Int) {
		  Studio(id: $id) {
		    id
		    name
		    siteUrl
		    media(isMain: true, sort: START_DATE_DESC, page: $page, perPage: $perPage) {
		      pageInfo { hasNextPage }
		      nodes {
		        id
		        siteUrl
		        type
		        format
		        isAdult
		        episodes
		        averageScore
		        title { romaji english }
		        coverImage { extraLarge large }
		        startDate { year month day }
		        season
		        seasonYear
		      }
		    }
		  }
		}
		""";
}
