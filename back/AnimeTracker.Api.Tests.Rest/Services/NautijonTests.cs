using AnimeTracker.Api.Abstractions.Interfaces.Adapters;
using AnimeTracker.Api.Abstractions.Models.Base.Anime;
using AnimeTracker.Api.Tests.Rest.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace AnimeTracker.Api.Tests.Rest.Services;

public class NautijonTests:  TestBed<RestAdapterFixture>
{
	public NautijonTests(ITestOutputHelper testOutputHelper, RestAdapterFixture fixture) : base(testOutputHelper, fixture)
	{
	}

	[Fact]
	public async Task GetAnimes()
	{
		var nautijonAdapter = _fixture.GetScopedService<INautijonAdapter>(_testOutputHelper);

		Assert.NotNull(nautijonAdapter);

		var animes = await nautijonAdapter.GetAnimes(new AnimeDate(2024, AnimeSeason.Winter));
		Assert.NotEmpty(animes);
	}

	[Fact]
	public async Task GetAnimeEpisodes()
	{
		var nautijonAdapter = _fixture.GetScopedService<INautijonAdapter>(_testOutputHelper);

		Assert.NotNull(nautijonAdapter);

		var episodes = await nautijonAdapter.GetAnimeEpisodes("https://www.nautiljon.com/animes/86.html");
		Assert.NotEmpty(episodes);
		Assert.Equal(11, episodes.Length);
	}


}