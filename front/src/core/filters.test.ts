import { describe, expect, it } from "vite-plus/test";
import { countActiveFilters, defaultSeasonFilters, type SeasonFilters } from "@/core/filters";

function filters(overrides: Partial<SeasonFilters>): SeasonFilters {
	return { ...defaultSeasonFilters, ...overrides };
}

describe("countActiveFilters", () => {
	it("counts nothing on the view the product opens on", () => {
		expect(countActiveFilters(defaultSeasonFilters)).toBe(0);
	});

	it("counts each filter moved off its default", () => {
		expect(countActiveFilters(filters({ title: "frieren" }))).toBe(1);
		expect(countActiveFilters(filters({ status: "bingeable", genres: ["Drama"], studio: "MADHOUSE" }))).toBe(3);
		expect(countActiveFilters(filters({ adult: "include", dub: "complete" }))).toBe(2);
	});

	it("counts the two thresholds separately", () => {
		expect(countActiveFilters(filters({ range: { minScore: 7, minVotes: 0 } }))).toBe(1);
		expect(countActiveFilters(filters({ range: { minScore: 7, minVotes: 100 } }))).toBe(2);
	});

	it("counts any adult setting but the default", () => {
		expect(countActiveFilters(filters({ adult: "only" }))).toBe(1);
		expect(countActiveFilters(filters({ adult: "hide" }))).toBe(0);
	});

	it("does not count the sort, which filters nothing", () => {
		expect(countActiveFilters(filters({ sort: "binge" }))).toBe(0);
	});

	it("treats the default formats as unfiltered whatever their order", () => {
		expect(countActiveFilters(filters({ formats: ["TvShort", "Ona", "Tv"] }))).toBe(0);
	});

	it("counts a different set of formats, the empty one included", () => {
		expect(countActiveFilters(filters({ formats: ["Tv", "Ona", "TvShort", "Movie"] }))).toBe(1);
		expect(countActiveFilters(filters({ formats: ["Tv"] }))).toBe(1);
		expect(countActiveFilters(filters({ formats: [] }))).toBe(1);
	});
});
