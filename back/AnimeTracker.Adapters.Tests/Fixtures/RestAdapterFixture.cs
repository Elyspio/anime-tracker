using AnimeTracker.Abstractions.Interfaces.Injections;
using AnimeTracker.Adapters.Nautijon.Injections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace AnimeTracker.Tests.Rest.Fixtures;

public class RestAdapterFixture: TestBedFixture
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