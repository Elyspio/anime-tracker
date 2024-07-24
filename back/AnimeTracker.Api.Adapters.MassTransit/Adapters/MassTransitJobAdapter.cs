using System.Linq.Expressions;
using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Adapters.MassTransit.Consumers;
using AnimeTracker.Api.Adapters.MassTransit.Messages;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AnimeTracker.Api.Adapters.MassTransit.Adapters;

public class MassTransitJobAdapter: TracingAdapter, IMassTransitJobAdapter
{
	private readonly IBus  _sendEndpoint;

	public MassTransitJobAdapter(ILogger<MassTransitJobAdapter> logger, IBus  sendEndpoint) : base(logger)
	{
		_sendEndpoint = sendEndpoint;
	}


	public async Task SendAnimeRefreshMessage(string animeUrl)
	{
		using var _ = LogAdapter($"{Log.F(animeUrl)}");

		await _sendEndpoint.Publish(new RefreshAnimeEpisodesMessage(animeUrl));
	}
}