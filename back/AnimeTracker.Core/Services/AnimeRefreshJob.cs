using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Abstractions.Models.Base.Anime;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Core.Services;

/// <summary>
///     The recurring-job entry point. It exists as its own type because Hangfire resolves the
///     target of a recurring job from the container, and a hosted service is not a good target.
/// </summary>
public class AnimeRefreshJob(IAnimeService animeService, ILogger<AnimeRefreshJob> logger) : TracingService(logger)
{
	public const string RecurringJobId = "anime-season-refresh";

	// ReSharper disable once MemberCanBePrivate.Global — called by Hangfire through an expression.
	public async Task RefreshCurrentSeason()
	{
		using var _ = LogService();

		await animeService.RefreshAll(AnimeDate.Current(DateOnly.FromDateTime(DateTime.UtcNow)));
	}
}
