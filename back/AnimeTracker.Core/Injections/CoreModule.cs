using AnimeTracker.Abstractions.Interfaces.Services;
using AnimeTracker.Core.Hosted;
using AnimeTracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Core.Injections;

public static class CoreModule
{
	public static IServiceCollection AddCore(this IServiceCollection services)
	{
		services.AddSingleton<IAnimeService, AnimeService>();
		services.AddSingleton<AnimeRefreshJob>();
		services.AddHostedService<AnimeHostedService>();

		return services;
	}
}
