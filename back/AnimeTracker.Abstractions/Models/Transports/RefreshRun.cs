using AnimeTracker.Abstractions.Interfaces.Business;
using AnimeTracker.Abstractions.Models.Base.Refresh;

namespace AnimeTracker.Abstractions.Models.Transports;

/// <summary>What the job dashboard reads. Served anonymously — the data is a scraping schedule.</summary>
public class RefreshRun : RefreshRunBase, ITransport
{
	public required Guid Id { get; init; }
}
