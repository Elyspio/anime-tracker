using Elyspio.Utils.Telemetry.MongoDB.Extensions;
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Elyspio.Utils.Telemetry.Tracing.Builder;

namespace Example.Api.Web.Technical.Extensions;

/// <summary>
///     OpenTelemetry Extensions methods for <see cref="IServiceCollection" />
/// </summary>
public static class OpenTelemetryExtentions
{
	/// <summary>
	///     Activate open telemetry support
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configuration"></param>
	/// <returns></returns>
	public static IServiceCollection AddAppOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
	{
		if (!configuration.IsTelemetryEnabled(out var telemetryConf)) return services;

		var telemetryBuilder = new AppOpenTelemetryBuilder<Program>(telemetryConf!)
		{
			Tracing = tracing => tracing.AddAppMongoInstrumentation()
		};

		telemetryBuilder.Build(services);

		return services;
	}
}