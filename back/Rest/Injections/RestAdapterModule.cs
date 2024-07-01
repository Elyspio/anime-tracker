using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.Rest.Adapters;
using AnimeTracker.Api.Adapters.Rest.Assemblers;
using AnimeTracker.Api.Adapters.Rest.Configs;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Api.Adapters.Rest.Injections;

public class RestAdapterModule : IDotnetModule
{
	public void Load(IServiceCollection services, IConfiguration configuration)
	{
		var conf = new EndpointConfig();
		configuration.GetSection(EndpointConfig.Section).Bind(conf);

		services.AddHttpClient<IJwtClient, JwtClient>(client => { client.BaseAddress = new Uri(conf.Authentication); });


		services.Scan(s => s.FromAssemblyOf<RestAdapterModule>()
			.AddClasses(c => c.InNamespaceOf<JwtAdapter>())
			.AsImplementedInterfaces()
			.WithSingletonLifetime()
		);
		services.Scan(s => s.FromAssemblyOf<RestAdapterModule>()
			.AddClasses(c => c.InNamespaceOf<AnimeAssembler>())
			.AsSelf()
			.WithSingletonLifetime()
		);



	}
}