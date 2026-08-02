using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using HtmlAgilityPack;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Runs the tile parser over a recorded season listing. Everything here is asserted against the
///     real markup, so a layout change upstream fails these before it silently empties the grid.
/// </summary>
public class AnimeTileAssemblerTests
{
	private static readonly AnimeDate Date = new(2026, AnimeSeason.Summer);

	private static AnimeBase[] ParseSeason()
	{
		var dom = new HtmlDocument();
		dom.LoadHtml(Fixtures.Read("season-ete-2026.html"));

		var assembler = new AnimeTileAssembler();

		var nodes = dom.DocumentNode.SelectNodes("//div[@class='elt' and not(ancestor::div[@id='saison_continue'])]");
		Assert.NotNull(nodes);

		return nodes.Select(node =>
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(node.InnerHtml);
			return assembler.Convert(Date, doc);
		}).ToArray();
	}

	[Fact]
	public void Parses_the_whole_season_without_throwing()
	{
		var animes = ParseSeason();

		Assert.NotEmpty(animes);
		Assert.All(animes, anime => Assert.False(string.IsNullOrWhiteSpace(anime.Title)));
	}

	[Fact]
	public void Excludes_the_carried_over_shows_block()
	{
		// The page also lists shows continuing from the previous season under #saison_continue.
		// They are not this season's releases and must not reach the grid.
		var dom = new HtmlDocument();
		dom.LoadHtml(Fixtures.Read("season-ete-2026.html"));

		var all = dom.DocumentNode.SelectNodes("//div[@class='elt']")!.Count;
		var thisSeason = ParseSeason().Length;

		Assert.True(thisSeason < all, $"expected the carried-over block to be excluded, got {thisSeason} of {all}");
	}

	[Fact]
	public void Builds_absolute_anime_urls()
	{
		// Url is the key the episodes are later stored against, so a relative path here would
		// silently split every anime into two documents.
		Assert.All(ParseSeason(), anime => Assert.StartsWith("https://nautiljon.com/animes/", anime.Url));
	}

	[Fact]
	public void Extracts_cover_images_as_absolute_https_urls()
	{
		Assert.All(ParseSeason(), anime =>
		{
			Assert.StartsWith("https://", anime.ImageUrl);
			Assert.DoesNotContain(")", anime.ImageUrl);
			Assert.DoesNotContain("url(", anime.ImageUrl);
		});
	}

	[Fact]
	public void Reads_scores_within_the_ten_point_scale()
	{
		var scored = ParseSeason().Where(anime => anime.Score is not null).ToArray();

		Assert.NotEmpty(scored);
		Assert.All(scored, anime => Assert.InRange(anime.Score!.Value, 0, 10));
	}

	[Fact]
	public void Reads_a_positive_popularity_for_most_of_the_season()
	{
		// GetPopularity falls back to -1 when the node is missing; a season where that dominates
		// means the selector has drifted.
		var animes = ParseSeason();
		var withPopularity = animes.Count(anime => anime.Popularity > 0);

		Assert.True(withPopularity > animes.Length / 2, $"only {withPopularity} of {animes.Length} carried a popularity");
	}

	[Fact]
	public void Reads_announced_episode_counts_for_part_of_the_season()
	{
		// Not every show announces one — that is exactly what drives BingeStatus.UnknownEnd — but
		// a season where none does would mean the "eps" span is no longer being found.
		var animes = ParseSeason();
		var announced = animes.Where(anime => anime.EpisodesCount is not null).ToArray();

		Assert.NotEmpty(announced);
		Assert.All(announced, anime => Assert.InRange(anime.EpisodesCount!.Value, 1, 2000));
	}

	[Fact]
	public void Attaches_genre_tags_to_most_shows()
	{
		var animes = ParseSeason();
		var tagged = animes.Count(anime => anime.Tags.Count > 0);

		Assert.True(tagged > animes.Length / 2, $"only {tagged} of {animes.Length} carried tags");
	}

	[Fact]
	public void Stamps_every_anime_with_the_requested_season()
	{
		Assert.All(ParseSeason(), anime => Assert.Equal(Date, anime.Date));
	}
}
