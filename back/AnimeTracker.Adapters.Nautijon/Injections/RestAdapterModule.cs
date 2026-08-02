using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.Rest.Adapters;
using AnimeTracker.Api.Adapters.Rest.Assemblers;
using AnimeTracker.Api.Adapters.Rest.Configs;
using Example.Api.Adapters.Rest.AuthenticationApi;
using FlareSolverrSharp;
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

		services.AddHttpClient(NautijonAdapter.ClientName,
			client =>
			{
				client.DefaultRequestHeaders.Add("User-Agent",
					"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36 " + Random.Shared.Next(1000, 9999)
				);
			}).ConfigurePrimaryHttpMessageHandler<ClearanceHandler>();

		services.AddTransient<ClearanceHandler>(_ => new ClearanceHandler("http://localhost:8191/")
		{
			MaxTimeout = 60000,
		});


		services.Scan(s => s.FromAssemblyOf<RestAdapterModule>()
			.AddClasses(c => c.InNamespaceOf<JwtAdapter>())
			.AsImplementedInterfaces()
			.WithSingletonLifetime()
		);
		services.Scan(s => s.FromAssemblyOf<RestAdapterModule>()
			.AddClasses(c => c.InNamespaceOf<AnimeTileAssembler>())
			.AsSelf()
			.WithSingletonLifetime()
		);
	}
}