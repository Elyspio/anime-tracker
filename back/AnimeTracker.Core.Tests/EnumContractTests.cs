using System.Text.Json;
using System.Text.Json.Serialization;
using AnimeTracker.Abstractions.Models.Base.Anime;
using AnimeTracker.Abstractions.Models.Base.Refresh;
using AnimeTracker.Abstractions.Models.Transports;
using Shouldly;
using Xunit;

namespace AnimeTracker.Core.Tests;

/// <summary>
///     These enum names are a contract. The API serialises them by name (Program.cs adds a
///     <see cref="JsonStringEnumConverter" />), and the frontend mirrors them as hand-written
///     TypeScript unions in <c>front/src/core/api/types.ts</c>. Renaming a member without touching
///     the other side breaks at runtime, in the browser, silently. These tests are the tripwire —
///     the matching frontend test lives in <c>front/src/core/api/types.test.ts</c>.
/// </summary>
public class EnumContractTests
{
	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() }
	};

	private static string Serialize<T>(T value)
	{
		return JsonSerializer.Serialize(value, Options).Trim('"');
	}

	[Theory]
	[InlineData(AnimeSeason.Winter, "Winter")]
	[InlineData(AnimeSeason.Spring, "Spring")]
	[InlineData(AnimeSeason.Summer, "Summer")]
	[InlineData(AnimeSeason.Fall, "Fall")]
	public void Serialises_a_season_by_name(AnimeSeason season, string expected)
	{
		Serialize(season).ShouldBe(expected);
	}

	[Theory]
	[InlineData(BingeStatus.BingeableNow, "BingeableNow")]
	[InlineData(BingeStatus.Announced, "Announced")]
	[InlineData(BingeStatus.Estimated, "Estimated")]
	[InlineData(BingeStatus.UnknownEnd, "UnknownEnd")]
	public void Serialises_a_binge_status_by_name(BingeStatus status, string expected)
	{
		Serialize(status).ShouldBe(expected);
	}

	[Theory]
	[InlineData(AnimeFormat.Unknown, "Unknown")]
	[InlineData(AnimeFormat.Tv, "Tv")]
	[InlineData(AnimeFormat.TvShort, "TvShort")]
	[InlineData(AnimeFormat.Ona, "Ona")]
	[InlineData(AnimeFormat.Ova, "Ova")]
	[InlineData(AnimeFormat.Movie, "Movie")]
	[InlineData(AnimeFormat.Special, "Special")]
	[InlineData(AnimeFormat.Music, "Music")]
	public void Serialises_a_format_by_name(AnimeFormat format, string expected)
	{
		Serialize(format).ShouldBe(expected);
	}

	[Theory]
	[InlineData(RefreshStatus.Queued, "Queued")]
	[InlineData(RefreshStatus.Running, "Running")]
	[InlineData(RefreshStatus.Succeeded, "Succeeded")]
	[InlineData(RefreshStatus.Failed, "Failed")]
	[InlineData(RefreshStatus.Interrupted, "Interrupted")]
	public void Serialises_a_refresh_status_by_name(RefreshStatus status, string expected)
	{
		Serialize(status).ShouldBe(expected);
	}

	[Fact]
	public void Declares_no_member_the_frontend_has_not_been_told_about()
	{
		// A member added here and nowhere else is the failure mode these tests exist for: the API
		// starts emitting a string the TypeScript union does not contain.
		Enum.GetNames<AnimeSeason>().ShouldBe(["Winter", "Spring", "Summer", "Fall"]);
		Enum.GetNames<BingeStatus>().ShouldBe(["BingeableNow", "Announced", "Estimated", "UnknownEnd"]);
		Enum.GetNames<RefreshStatus>().ShouldBe(["Queued", "Running", "Succeeded", "Failed", "Interrupted"]);
		Enum.GetNames<AnimeFormat>().ShouldBe(["Unknown", "Tv", "TvShort", "Ona", "Ova", "Movie", "Special", "Music"]);
	}
}
