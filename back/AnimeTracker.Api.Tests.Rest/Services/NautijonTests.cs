using System.Net.Http.Json;
using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Tests.Rest.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace AnimeTracker.Api.Tests.Rest.Services;

public class NautijonTests:  TestBed<CoreFixture>
{
	public NautijonTests(ITestOutputHelper testOutputHelper, CoreFixture fixture) : base(testOutputHelper, fixture)
	{
	}

	[Fact]
	public async Task GetAnimes()
	{
		var nautijonAdapter = _fixture.GetScopedService<INautijonAdapter>(_testOutputHelper);

		Assert.NotNull(nautijonAdapter);

		var token = await nautijonAdapter.GetAnimes(new AnimeDate(2024, AnimeSeason.Winter));
		Assert.NotEmpty(token);
	}


}