using AnimeTracker.Abstractions.Common.Assemblers;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Abstractions.Models.Transports;
using Mapster;

namespace AnimeTracker.Core.Assemblers;

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