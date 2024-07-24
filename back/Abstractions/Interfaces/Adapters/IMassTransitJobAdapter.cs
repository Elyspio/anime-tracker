namespace AnimeTracker.Api.Abstractions.Interfaces.Adapters;

public interface IMassTransitJobAdapter
{
	public Task SendAnimeRefreshMessage(string animeUrl);
}