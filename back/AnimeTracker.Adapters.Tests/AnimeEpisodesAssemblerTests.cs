using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Adapters.Nautijon.Assemblers;
using HtmlAgilityPack;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Drives the episode table parser from markup shaped like the pages it reads. These pin the
///     parsing rules — number formats, missing dates, simulcast links — not Nautiljon's markup
///     itself; that is what the recorded fixtures under Fixtures/ are for.
/// </summary>
public class AnimeEpisodesAssemblerTests
{
	private static Episode[] Parse(string html)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		return new AnimeEpisodesAssembler().Convert(doc);
	}

	private static string EpisodeTable(string rows)
	{
		return $"<html><body><div id='episodes'><table><tbody>{rows}</tbody></table></div></body></html>";
	}

	[Fact]
	public void Reads_number_title_and_release_date()
	{
		var episodes = Parse(EpisodeTable(
			"<tr><td>1</td><td><a href='/animes/x/1.html'>Le commencement</a></td><td>2026-01-04</td></tr>" +
			"<tr><td>2</td><td><a href='/animes/x/2.html'>La suite</a></td><td>2026-01-11</td></tr>"));

		Assert.Equal(2, episodes.Length);
		Assert.Equal(1, episodes[0].Number);
		Assert.Equal("Le commencement", episodes[0].Title);
		Assert.Equal(new DateOnly(2026, 1, 4), episodes[0].ReleaseDate);
		Assert.Equal(new DateOnly(2026, 1, 11), episodes[1].ReleaseDate);
	}

	[Fact]
	public void An_episode_with_no_date_column_has_no_release_date()
	{
		var episodes = Parse(EpisodeTable("<tr><td>7</td><td><a href='/animes/x/7.html'>À venir</a></td></tr>"));

		Assert.Null(Assert.Single(episodes).ReleaseDate);
	}

	[Fact]
	public void An_unparsable_date_is_dropped_rather_than_guessed()
	{
		var episodes = Parse(EpisodeTable("<tr><td>7</td><td><a href='/animes/x/7.html'>Inconnu</a></td><td>à venir</td></tr>"));

		Assert.Null(Assert.Single(episodes).ReleaseDate);
	}

	[Fact]
	public void Half_numbered_specials_keep_their_decimal()
	{
		// Recap and .5 episodes are numbered 6.5 and must not round into episode 6 or 7.
		var episodes = Parse(EpisodeTable("<tr><td>6.5</td><td><a href='/animes/x/65.html'>Récapitulatif</a></td><td>2026-02-15</td></tr>"));

		Assert.Equal(6.5, Assert.Single(episodes).Number);
	}

	[Fact]
	public void Html_entities_in_titles_are_decoded()
	{
		var episodes = Parse(EpisodeTable("<tr><td>1</td><td><a href='/animes/x/1.html'>L&#039;été o&ugrave; tout bascule</a></td><td>2026-01-04</td></tr>"));

		Assert.Equal("L'été où tout bascule", Assert.Single(episodes).Title);
	}

	[Fact]
	public void A_page_with_no_episode_table_yields_an_empty_list()
	{
		Assert.Empty(Parse("<html><body><div id='fiche'></div></body></html>"));
	}

	[Fact]
	public void Simulcast_links_are_matched_to_their_episode_number()
	{
		var html =
			"<html><body>" +
			"<div id='simulcast'><table><tbody>" +
			"<tr><td><a href='https://simulcast.example/ep1'>Épisode 1</a></td></tr>" +
			"<tr><td><a href='https://simulcast.example/ep2'>Épisode 2</a></td></tr>" +
			"</tbody></table></div>" +
			"<div id='episodes'><table><tbody>" +
			"<tr><td>1</td><td><a href='/animes/x/1.html'>Un</a></td><td>2026-01-04</td></tr>" +
			"<tr><td>2</td><td><a href='/animes/x/2.html'>Deux</a></td><td>2026-01-11</td></tr>" +
			"<tr><td>3</td><td><a href='/animes/x/3.html'>Trois</a></td><td>2026-01-18</td></tr>" +
			"</tbody></table></div>" +
			"</body></html>";

		var episodes = Parse(html);

		Assert.Equal("https://simulcast.example/ep1", episodes[0].Url);
		Assert.Equal("https://simulcast.example/ep2", episodes[1].Url);
		Assert.Null(episodes[2].Url);
	}
}
