using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Entities;
using AnimeTracker.Adapters.MongoDB.Technical;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Shouldly;
using Xunit;

namespace AnimeTracker.Adapters.Tests;

/// <summary>
///     Round-trips the stored entities through BSON, with no server involved. This is the layer that
///     used to have no test at all, and the first thing it caught was a run failing to insert because
///     driver 3.x refuses to serialise a Guid until told which representation to use.
/// </summary>
public class MongoMappingsTests
{
	public MongoMappingsTests()
	{
		MongoMappings.Register();
	}

	private static BsonDocument Serialize<T>(T value)
	{
		return value!.ToBsonDocument();
	}

	private static RefreshRunEntity Run()
	{
		return new RefreshRunEntity
		{
			RunId = Guid.Parse("6f2f8f2e-2b0a-4a53-9b7f-9c1a2d3e4f50"),
			Date = new AnimeDate(2026, AnimeSeason.Summer),
			Status = RefreshStatus.Running,
			Total = 78,
			StartedAt = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
			UpdatedAt = new DateTimeOffset(2026, 8, 3, 12, 0, 2, TimeSpan.Zero),
			FinishedAt = null,
			Error = null
		};
	}

	[Fact]
	public void Serialises_a_run_id()
	{
		var document = Serialize(Run());

		// Subtype 4 is the RFC 4122 layout. The legacy subtype 3 byte-orders the value differently,
		// so a document written under one and read under the other is silently a different id.
		document["RunId"].BsonType.ShouldBe(BsonType.Binary);
		document["RunId"].AsBsonBinaryData.SubType.ShouldBe(BsonBinarySubType.UuidStandard);
	}

	[Fact]
	public void Round_trips_a_run()
	{
		var restored = BsonSerializer.Deserialize<RefreshRunEntity>(Serialize(Run()));

		restored.RunId.ShouldBe(Run().RunId);
		restored.Status.ShouldBe(RefreshStatus.Running);
		restored.Date.ShouldBe(new AnimeDate(2026, AnimeSeason.Summer));
		restored.Total.ShouldBe(78);
		restored.FinishedAt.ShouldBeNull();
	}

	[Fact]
	public void Stores_enums_as_names()
	{
		// A reordered enum must not silently reinterpret documents already written.
		var document = Serialize(Run());

		document["Status"].AsString.ShouldBe("Running");
		document["Date"]["Season"].AsString.ShouldBe("Summer");
	}

	[Fact]
	public void Round_trips_an_anime()
	{
		var anime = new AnimeEntity
		{
			SourceId = 178789,
			Date = new AnimeDate(2026, AnimeSeason.Summer),
			Title = "Mushoku Tensei III",
			AlternativeTitles = ["Mushoku Tensei: Jobless Reincarnation Season 3", "無職転生 Ⅲ"],
			Description = "",
			Studio = "Studio Bind",
			ImageUrl = "https://s4.anilist.co/cover.jpg",
			Url = "https://anilist.co/anime/178789",
			Format = AnimeFormat.Tv,
			IsAdult = false,
			Score = 8.5,
			Popularity = 135534,
			VotesCount = 9119,
			EpisodesCount = 14,
			Genres = ["Adventure", "Drama"],
			Episodes = [new Episode(1, new DateOnly(2026, 7, 4))]
		};

		var restored = BsonSerializer.Deserialize<AnimeEntity>(Serialize(anime));

		restored.SourceId.ShouldBe(178789);
		restored.Format.ShouldBe(AnimeFormat.Tv);
		restored.Score.ShouldBe(8.5);
		restored.VotesCount.ShouldBe(9119);
		restored.AlternativeTitles.ShouldBe(["Mushoku Tensei: Jobless Reincarnation Season 3", "無職転生 Ⅲ"]);
		restored.Genres.ShouldBe(["Adventure", "Drama"]);
		restored.Episodes.Single().ReleaseDate.ShouldBe(new DateOnly(2026, 7, 4));
	}

	[Fact]
	public void Reads_an_anime_stored_before_alternative_titles_existed_as_having_none()
	{
		// Seasons nobody refreshed since the field was added still carry documents without it.
		var document = Serialize(new AnimeEntity
		{
			SourceId = 1,
			Date = new AnimeDate(2026, AnimeSeason.Summer),
			Title = "",
			Description = "",
			Studio = "",
			ImageUrl = "",
			Url = "",
			Format = AnimeFormat.Unknown,
			IsAdult = false,
			Score = null,
			Popularity = 0,
			VotesCount = null,
			EpisodesCount = null,
			Genres = [],
			Episodes = []
		});
		document.Remove("AlternativeTitles");

		BsonSerializer.Deserialize<AnimeEntity>(document).AlternativeTitles.ShouldBeEmpty();
	}

	[Fact]
	public void Keeps_an_unrated_anime_null_rather_than_zero()
	{
		var anime = new AnimeEntity
		{
			SourceId = 1,
			Date = new AnimeDate(2026, AnimeSeason.Summer),
			Title = "",
			Description = "",
			Studio = "",
			ImageUrl = "",
			Url = "",
			Format = AnimeFormat.Unknown,
			IsAdult = false,
			Score = null,
			Popularity = 0,
			VotesCount = null,
			EpisodesCount = null,
			Genres = [],
			Episodes = []
		};

		var restored = BsonSerializer.Deserialize<AnimeEntity>(Serialize(anime));

		restored.Score.ShouldBeNull();
		restored.VotesCount.ShouldBeNull();
		restored.EpisodesCount.ShouldBeNull();
	}
}
