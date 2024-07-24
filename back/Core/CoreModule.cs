using AnimeTracker.Api.Abstractions.Common.Extensions;
using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Core.Hosted;
using AnimeTracker.Api.Core.Services;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace AnimeTracker.Api.Core;

public class CoreModule : IDotnetModule
{
	public void Load(IServiceCollection services, IConfiguration configuration)
	{
		var nsp = typeof(CoreModule).Namespace!;

		services.Scan(scan => scan
			.FromAssemblyOf<CoreModule>()
			.AddClasses(classes => classes.InNamespaceOf<AnimeService>())
			.AsImplementedInterfaces()
			.WithSingletonLifetime()
		);

		services.AddHostedService<AnimeHostedService>();


		TypeAdapterConfig.GlobalSettings.ForType<Guid, ObjectId>().MapWith(id => id.AsObjectId());
		TypeAdapterConfig.GlobalSettings.ForType<ObjectId, Guid>().MapWith(id => id.AsGuid());

	}
}