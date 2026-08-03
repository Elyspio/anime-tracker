namespace AnimeTracker.Abstractions.Models.Base.Anime;

public record AnimeDate(int Year, AnimeSeason Season)
{
	/// <summary>
	///     The season a given day belongs to, following the broadcast calendar Nautiljon indexes by:
	///     three-month blocks starting in January.
	/// </summary>
	public static AnimeDate Current(DateOnly today)
	{
		var season = ((today.Month - 1) / 3) switch
		{
			0 => AnimeSeason.Winter,
			1 => AnimeSeason.Spring,
			2 => AnimeSeason.Summer,
			_ => AnimeSeason.Fall
		};

		return new AnimeDate(today.Year, season);
	}
}
