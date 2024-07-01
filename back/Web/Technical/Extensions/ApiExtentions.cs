using System.Text.Json.Serialization;
using AnimeTracker.Api.Abstractions.Common.Helpers.Json;
using AnimeTracker.Api.Web.Technical.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AnimeTracker.Api.Web.Technical.Extensions;

/// <summary>
///     Api Extensions methods for <see cref="IServiceCollection" />
/// </summary>
public static class ApiExtentions
{
	/// <summary>
	///     Setup Controllers configuration
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddAppControllers(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		// services.AddSingleton<CustomAuthorizeFilter>();

		services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

		services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

		services.AddControllers(o =>
				{
					o.Conventions.Add(new ControllerDocumentationConvention());
					o.OutputFormatters.RemoveType<StringOutputFormatter>();
					o.Filters.Add<HttpExceptionFilter>();
				}
			)
			.AddNewtonsoftJson(x =>
			{
				x.SerializerSettings.Formatting = Formatting.None;
				x.SerializerSettings.ContractResolver = new JsonContractResolver();

				// x.SerializerSettings.ContractResolver = new JsonContractResolver();
				x.SerializerSettings.Converters.Add(new StringEnumConverter());
				x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
			});

		return services;
	}

	/// <summary>
	///     Setup SignalR configuration
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddAppSignalR(this IServiceCollection services)
	{
		services.AddSignalR(options => { options.EnableDetailedErrors = true; })
			.AddJsonProtocol(options =>
				{
					options.PayloadSerializerOptions.IncludeFields = true;
					options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
					options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
					options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
				}
			);

		return services;
	}

	/// <summary>
	///     Setup CORS for local development
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection SetupDevelopmentCors(this IServiceCollection services)
	{
		services.AddCors(options =>
			{
				options.AddDefaultPolicy(b =>
					{
						b.AllowAnyOrigin();
						b.AllowAnyHeader();
						b.AllowAnyMethod();
					}
				);
			}
		);

		return services;
	}
}