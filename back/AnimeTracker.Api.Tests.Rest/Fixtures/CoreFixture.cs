using AnimeTracker.Api.Abstractions.Interfaces.Injections;
using AnimeTracker.Api.Adapters.Rest.Injections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace AnimeTracker.Api.Tests.Rest.Fixtures;

public class CoreFixture: TestBedFixture
{
	protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
	{

		services.AddSingleton(configuration!);

		services.AddModule<RestAdapterModule>(configuration!);
	}

	protected override ValueTask DisposeAsyncCore()
		=> new();

	protected override IEnumerable<TestAppSettings> GetTestAppSettings()
	{
		yield return new TestAppSettings { Filename = "appsettings.json", IsOptional = false };
	}
}