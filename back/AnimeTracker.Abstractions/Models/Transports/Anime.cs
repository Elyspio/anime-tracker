using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Transports;

public class Anime : AnimeBase, ITransport
{
	public required Guid Id { get; init; }

	/// <summary>Computed on read — see <see cref="BingePrediction" />.</summary>
	public required BingePrediction Binge { get; init; }

	/// <summary>
	///     The French dub on every platform the anime was matched on, best first. Empty when no platform
	///     was matched, which says the dub is unknown — not that there is none.
	/// </summary>
	public required IReadOnlyCollection<DubAvailability> Dubs { get; init; }
}
