import { describe, expect, it } from "vite-plus/test";
import { parseSeasonPath, parseStudioId, seasonPath, studioPath } from "@/core/routes";
import { animeSeasons } from "@/core/api/types";

describe("seasonPath", () => {
	it("writes the year then the season in lower case", () => {
		expect(seasonPath({ year: 2026, season: "Fall" })).toBe("/2026/fall");
	});

	it("is read back by parseSeasonPath for every season", () => {
		for (const season of animeSeasons) {
			const [, year, name] = seasonPath({ year: 2019, season }).split("/");

			expect(parseSeasonPath(year, name)).toEqual({ year: 2019, season });
		}
	});
});

describe("parseSeasonPath", () => {
	it("reads a season whatever its case", () => {
		expect(parseSeasonPath("2026", "FALL")).toEqual({ year: 2026, season: "Fall" });
		expect(parseSeasonPath("2026", "Winter")).toEqual({ year: 2026, season: "Winter" });
	});

	it.each([
		["26", "fall"],
		["20260", "fall"],
		["year", "fall"],
		["2026", "autumn"],
		["2026", ""],
		// The sign-in callback has two segments too, and is not a season.
		["login", "callback"],
	])("refuses %s/%s", (year, season) => {
		expect(parseSeasonPath(year, season)).toBeNull();
	});

	it("refuses an address with a part missing", () => {
		expect(parseSeasonPath(undefined, undefined)).toBeNull();
		expect(parseSeasonPath("2026", undefined)).toBeNull();
	});
});

describe("studioPath", () => {
	it("is read back by parseStudioId", () => {
		expect(studioPath(6309)).toBe("/studios/6309");
		expect(parseStudioId("6309")).toBe(6309);
	});
});

describe("parseStudioId", () => {
	it.each(["0", "-3", "12a", "1.5", "", " 12", "007", "99999999999"])("refuses %j", (raw) => {
		expect(parseStudioId(raw)).toBeNull();
	});

	it("refuses a missing id", () => {
		expect(parseStudioId(undefined)).toBeNull();
	});

	it("refuses an id the API could not read as an int", () => {
		expect(parseStudioId("2147483647")).toBe(2147483647);
		expect(parseStudioId("2147483648")).toBeNull();
	});
});
