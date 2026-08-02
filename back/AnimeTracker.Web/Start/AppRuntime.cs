using AnimeTracker.Api.Web.Technical.Extensions;
using Hangfire;

namespace AnimeTracker.Api.Web.Start;

/// <summary>
///     Application Initializer
/// </summary>
public static class AppRuntime
{
	/// <summary>
	///     Initialize runtime middlewares
	/// </summary>
	/// <param name="app"></param>
	/// <returns></returns>
	public static WebApplication Initialize(this WebApplication app)
	{
		// Allow CORS
		app.UseCors();

		app.UseAppSwagger();

		// Setup authentication
		app.UseAuthentication();
		app.UseAuthorization();

		// Setup Controllers
		app.MapControllers();

		app.UseHangfireDashboard("/hangfire", new DashboardOptions
		{

		});

		if (app.Environment.IsProduction()) UseAppStaticFiles(app);

		return app;
	}

	private static void UseAppStaticFiles(WebApplication app)
	{
		app.UseRouting();
		app.UseStaticFiles();

		app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), appBuilder =>
		{
			appBuilder.UseRouting();
			appBuilder.UseEndpoints(ep => { ep.MapFallbackToFile("index.html"); });
		});

	}
}