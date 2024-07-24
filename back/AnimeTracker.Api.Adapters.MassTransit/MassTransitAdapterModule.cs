using System.Net;
using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.MassTransit.Adapters;
using AnimeTracker.Api.Adapters.MassTransit.Configurations;
using AnimeTracker.Api.Adapters.MassTransit.Consumers;
using AnimeTracker.Api.Adapters.MassTransit.Messages;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeTracker.Api.Adapters.MassTransit;

public class MassTransitAdapterModule : IDotnetModule
{
	const string ExchangeName = "anime-tracker-commands";
	const string QueueName = "anime-commands-refresh";

	public void Load(IServiceCollection services, IConfiguration configuration)
	{
		var rabbitMqConfiguration = configuration.GetRequiredSection(RabbitMqConfiguration.Section).Get<RabbitMqConfiguration>()!;

		services.Configure<RabbitMqConfiguration>(conf =>
		{
			conf.Password = rabbitMqConfiguration.Password;
			conf.Username = rabbitMqConfiguration.Username;
			conf.Nodes = rabbitMqConfiguration.Nodes;
			conf.VirtualHost = rabbitMqConfiguration.VirtualHost;
		});

		services.AddMassTransit(x =>
		{

			x.AddConsumer<AnimeRefreshConsumer>(cfg =>
			{
				cfg.UseMessageRetry(configurator => configurator
					.Interval(60, TimeSpan.FromMinutes(1))
					.Handle<HttpRequestException>(exception => exception.StatusCode == HttpStatusCode.TooManyRequests)
				);

				cfg.ConcurrentMessageLimit = 2;
			});


			x.SetKebabCaseEndpointNameFormatter();

			x.UsingRabbitMq((ctx, cfg) =>
			{
				cfg.Host("cluster", rabbitMqConfiguration.VirtualHost, c =>
				{
					c.Username(rabbitMqConfiguration.Username);
					c.Password(rabbitMqConfiguration.Password);

					c.UseCluster(cls =>
					{
						foreach (var node in rabbitMqConfiguration.Nodes)
						{
							cls.Node(node);
						}
					});
				});

				cfg.UseKillSwitch(options => options
					.SetActivationThreshold(2)
					.SetTripThreshold(0.15)
					.SetRestartTimeout(m: 1));

				cfg.ConfigureEndpoints(ctx);
			});
		});

		services.Scan(scan => scan
			.FromAssemblyOf<MassTransitAdapterModule>()
			.AddClasses(classes => classes.InNamespaceOf<MassTransitJobAdapter>())
			.AsImplementedInterfaces()
			.WithSingletonLifetime()
		);
	}
}