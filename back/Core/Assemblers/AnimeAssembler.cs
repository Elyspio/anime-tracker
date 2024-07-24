using AnimeTracker.Api.Abstractions.Common.Assemblers;
using AnimeTracker.Api.Abstractions.Models.Entities;
using AnimeTracker.Api.Abstractions.Models.Transports;
using Mapster;

namespace AnimeTracker.Api.Core.Assemblers;

public class AnimeAssembler : BaseAssembler<Anime, AnimeEntity>
{
	public override Anime Convert(AnimeEntity obj)
	{
		return obj.Adapt<Anime>();
	}

	public override AnimeEntity Convert(Anime obj)
	{
		return obj.Adapt<AnimeEntity>();
	}
}