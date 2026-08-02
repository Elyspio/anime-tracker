using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Anime;

namespace AnimeTracker.Abstractions.Models.Transports;

public class Anime : AnimeBase, ITransport
{
	public required Guid Id { get; init; }

	/// <summary>Computed on read — see <see cref="BingePrediction" />.</summary>
	public required BingePrediction Binge { get; init; }
}
