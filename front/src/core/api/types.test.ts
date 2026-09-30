import { describe, expect, it } from "vite-plus/test";
import { animeFormats, animeSeasons, bingeStatuses, dubMatchMethods, dubMatchStatuses, dubOverrideModes, dubPlatforms, refreshKinds, refreshStatuses } from "@/core/api/types";

/**
 * The other half of a contract: the API serialises its enums by name, and these unions are written
 * by hand to match. The C# side of the tripwire is `EnumContractTests` in `AnimeTracker.Core.Tests`
 * — the two lists have to be edited together or one of them fails.
 */
describe("enum contract with the API", () => {
	it("mirrors AnimeSeason", () => {
		expect(animeSeasons).toEqual(["Winter", "Spring", "Summer", "Fall"]);
	});

	it("mirrors BingeStatus", () => {
		expect(bingeStatuses).toEqual(["BingeableNow", "Announced", "Estimated", "UnknownEnd"]);
	});

	it("mirrors AnimeFormat", () => {
		expect(animeFormats).toEqual(["Unknown", "Tv", "TvShort", "Ona", "Ova", "Movie", "Special", "Music"]);
	});

	it("mirrors RefreshStatus", () => {
		expect(refreshStatuses).toEqual(["Queued", "Running", "Succeeded", "Failed", "Interrupted"]);
	});

	it("mirrors RefreshKind", () => {
		expect(refreshKinds).toEqual(["Season", "Dub"]);
	});

	it("mirrors the dub enums", () => {
		expect(dubPlatforms).toEqual(["Crunchyroll", "Adn"]);
		expect(dubMatchStatuses).toEqual(["Matched", "NotFound", "Unaligned"]);
		expect(dubMatchMethods).toEqual(["Link", "Search", "Pinned"]);
		expect(dubOverrideModes).toEqual(["Auto", "Pinned", "Blocked"]);
	});
});
