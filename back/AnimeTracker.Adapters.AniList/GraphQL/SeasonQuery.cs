using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Adapters.AniList.GraphQL;

/// <summary>
///     The single query this adapter sends. Everything the product needs about a season comes back
///     in one round trip, airing schedule included — asking for the schedule inline is what removes
///     the per-anime request the previous source forced on us.
/// </summary>
internal static class SeasonQuery
{
	public const string Document = """
		query Season($season: MediaSeason, $year: Int, $page: Int, $perPage: Int) {
		  Page(page: $page, perPage: $perPage) {
		    pageInfo { hasNextPage }
		    media(season: $season, seasonYear: $year, type: ANIME, sort: POPULARITY_DESC) {
		      id
		      siteUrl
		      format
		      isAdult
		      episodes
		      averageScore
		      popularity
		      description(asHtml: false)
		      title { romaji english }
		      coverImage { large }
		      genres
		      studios(isMain: true) { nodes { name } }
		      stats { scoreDistribution { amount } }
		      airingSchedule { nodes { episode airingAt } }
		    }
		  }
		}
		""";

	/// <summary>AniList spells its seasons in upper case; ours are a C# enum.</summary>
	public static string SeasonName(AnimeSeason season)
	{
		return season switch
		{
			AnimeSeason.Winter => "WINTER",
			AnimeSeason.Spring => "SPRING",
			AnimeSeason.Summer => "SUMMER",
			AnimeSeason.Fall => "FALL",
			_ => throw new ArgumentOutOfRangeException(nameof(season), season, null)
		};
	}
}
