using AnimeTracker.Abstractions.Common.Extensions;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;

namespace AnimeTracker.Core.Assemblers;

/// <summary>Entity to transport. Hand-written so the exposed shape stays greppable.</summary>
public static class RefreshRunAssembler
{
	public static RefreshRun Convert(RefreshRunEntity entity)
	{
		return new RefreshRun
		{
			Id = entity.Id.AsGuid(),
			RunId = entity.RunId,
			Date = entity.Date,
			Status = entity.Status,
			Total = entity.Total,
			StartedAt = entity.StartedAt,
			UpdatedAt = entity.UpdatedAt,
			FinishedAt = entity.FinishedAt,
			Error = entity.Error
		};
	}
}
