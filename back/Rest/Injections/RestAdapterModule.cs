using Example.Api.Abstractions.Interfaces.Injections;
using Example.Api.Adapters.Rest.Adapters;
using Example.Api.Adapters.Rest.AuthenticationApi;
using Example.Api.Adapters.Rest.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Api.Adapters.Rest.Injections;

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



	}
}