using Example.Api.Web.Technical.Filters.Swagger;

namespace Example.Api.Web.Technical.Extensions;

/// <summary>
///     Swagger Extensions methods for <see cref="IServiceCollection" />
/// </summary>
public static class SwaggerExtentions
{
	/// <summary>
	///     Activate swagger support
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddAppSwagger(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		var xmlPaths = Directory.GetFiles(AppContext.BaseDirectory).ToList().Where(f => f.EndsWith(".xml"));

		services.AddSwaggerGen(options =>
		{
			options.SupportNonNullableReferenceTypes();
			options.OperationFilter<SwaggerSetNullableOperationFilter>();
			options.OperationFilter<SwaggerRemoveVersionFilter>();
			options.SchemaFilter<NullableSchemaFilter>();

			options.UseAllOfToExtendReferenceSchemas();
			options.UseAllOfForInheritance();
			options.CustomOperationIds(e => e.ActionDescriptor.RouteValues["action"]);

			foreach (var xmlPath in xmlPaths) options.IncludeXmlComments(xmlPath);
		}).AddSwaggerGenNewtonsoftSupport();

		return services;
	}

	/// <summary>
	///     Active la gestion de swagger et son interface en gérant le versioning
	/// </summary>
	/// <param name="app"></param>
	/// <returns></returns>
	public static WebApplication UseAppSwagger(this WebApplication app)
	{
		app.UseSwagger(options =>
		{
			options.PreSerializeFilters.Add((document, request) =>
			{
				if (!request.Headers.Referer.FirstOrDefault()?.StartsWith("https://") == true) return;

				foreach (var openApiServer in document.Servers) openApiServer.Url = openApiServer.Url.Replace("http://", "https://");
			});
		});
		app.UseSwaggerUI();

		return app;
	}
}