using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using HtmlAgilityPack;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Pins the exact values read from one known tile of the recorded listing. The aggregate checks
///     in <see cref="AnimeTileAssemblerTests" /> catch a selector that stopped matching; these catch
///     one that matches the wrong thing.
/// </summary>
public class AnimeTileValuesTests
{
	private static AnimeBase FirstTile()
	{
		var dom = new HtmlDocument();
		dom.LoadHtml(Fixtures.Read("season-ete-2026.html"));

		var node = dom.DocumentNode.SelectNodes("//div[@class='elt' and not(ancestor::div[@id='saison_continue'])]")![0];

		var tile = new HtmlDocument();
		tile.LoadHtml(node.InnerHtml);

		return new AnimeTileAssembler().Convert(new AnimeDate(2026, AnimeSeason.Summer), tile);
	}

	[Fact]
	public void Reads_every_field_of_a_known_tile()
	{
		var anime = FirstTile();

		Assert.Equal("Mushoku Tensei III - Isekai Ittara Honki Dasu", anime.Title);
		Assert.Equal("Studio Bind", anime.Studio);
		Assert.Equal("https://nautiljon.com/animes/mushoku+tensei+iii+-+isekai+ittara+honki+dasu.html", anime.Url);
		Assert.Equal(14, anime.EpisodesCount);
		Assert.Equal(9.14, anime.Score);
		Assert.Equal(976, anime.Popularity);
	}

	[Fact]
	public void Reads_the_synopsis_rather_than_leaving_it_blank()
	{
		Assert.StartsWith("Il s'agit de la troisième saison", FirstTile().Description);
	}

	[Fact]
	public void Reads_the_cover_url_without_the_css_wrapper()
	{
		var imageUrl = FirstTile().ImageUrl;

		Assert.StartsWith("https://www.nautiljon.com/images/anime/", imageUrl);
		Assert.EndsWith("_12782.webp?11785434296", imageUrl);
	}

	[Fact]
	public void Reads_the_genre_tags_with_their_links()
	{
		var tags = FirstTile().Tags.ToArray();

		Assert.Equal(7, tags.Length);
		Assert.Equal("Aventure", tags[0].Name);
		Assert.Contains("genres_include", tags[0].Url);
		Assert.Contains(tags, tag => tag.Name == "Réincarnation / Transmigration");
	}
}
