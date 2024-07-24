using AnimeTracker.Api.Abstractions.Interfaces.Services;
using AnimeTracker.Api.Adapters.MassTransit.Messages;
using MassTransit;

namespace AnimeTracker.Api.Adapters.MassTransit.Consumers;

public class AnimeRefreshConsumer: IConsumer<RefreshAnimeEpisodesMessage>
{
	private readonly IAnimeService _animeService;

	public AnimeRefreshConsumer(IAnimeService animeService)
	{
		_animeService = animeService;
	}


	public async Task Consume(ConsumeContext<RefreshAnimeEpisodesMessage> context)
	{
		await _animeService.Refresh(context.Message.AnimeUrl);
	}
}