using Serilog;

namespace Example.Api.Web.Technical.Extensions;

/// <summary>
///     Logging Extensions methods for <see cref="IServiceCollection" />
/// </summary>
public static class LoggingExtensions
{
	/// <summary>
	///     Activate logging
	/// </summary>
	/// <param name="host"></param>
	/// <returns></returns>
	public static ConfigureHostBuilder AddLogging(this ConfigureHostBuilder host)
	{
		// Setup Logging
		host.UseSerilog((ctx, lc) => lc
			.ReadFrom.Configuration(ctx.Configuration)
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}")
		);

		return host;
	}
}