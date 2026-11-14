import { describe, expect, it } from "vite-plus/test";
import { countFormats, firstYear, formatFetchedAt, formatWorkCount, formatWorkDate, groupByYear, matchesWorkAdult, matchesWorkFormat } from "@/core/studio";
import type { StudioWork, StudioWorkDate } from "@/core/api/types";

function work(sourceId: number, startDate: StudioWorkDate | null, overrides: Partial<StudioWork> = {}): StudioWork {
	return {
		sourceId,
		title: `Work ${sourceId}`,
		imageUrl: "",
		url: "",
		format: "Tv",
		isAdult: false,
		score: null,
		episodesCount: null,
		startDate,
		season: null,
		...overrides,
	};
}

const on = (year: number, month: number | null = null, day: number | null = null): StudioWorkDate => ({ year, month, day });

describe("groupByYear", () => {
	it("cuts the list where the year changes, keeping the order it was given", () => {
		const groups = groupByYear([work(1, null), work(2, null), work(3, on(2026, 10, 1)), work(4, on(2026, 7, 4)), work(5, on(2024, 4, 8))]);

		expect(groups.map((group) => group.year)).toEqual([null, 2026, 2024]);
		expect(groups.map((group) => group.works.map((entry) => entry.sourceId))).toEqual([[1, 2], [3, 4], [5]]);
	});

	it("puts a work dated by its year alone in that year", () => {
		expect(groupByYear([work(1, on(2027))]).map((group) => group.year)).toEqual([2027]);
	});

	it("has nothing to group in an empty filmography", () => {
		expect(groupByYear([])).toEqual([]);
	});
});

describe("countFormats", () => {
	it("counts the formats present, in the product's own order", () => {
		const works = [work(1, null, { format: "Movie" }), work(2, null), work(3, null), work(4, null, { format: "Special" })];

		expect(countFormats(works)).toEqual([
			{ format: "Tv", count: 2 },
			{ format: "Movie", count: 1 },
			{ format: "Special", count: 1 },
		]);
	});
});

describe("matchesWorkFormat", () => {
	it("keeps everything when no format is selected", () => {
		expect(matchesWorkFormat(work(1, null, { format: "Ova" }), null)).toBe(true);
	});

	it("keeps only the selected format", () => {
		expect(matchesWorkFormat(work(1, null, { format: "Ova" }), "Ova")).toBe(true);
		expect(matchesWorkFormat(work(1, null, { format: "Ova" }), "Tv")).toBe(false);
	});
});

describe("matchesWorkAdult", () => {
	it("hides adult works until asked for", () => {
		expect(matchesWorkAdult(work(1, null, { isAdult: true }), "hide")).toBe(false);
		expect(matchesWorkAdult(work(1, null, { isAdult: true }), "include")).toBe(true);
		expect(matchesWorkAdult(work(1, null), "hide")).toBe(true);
	});

	it("keeps only adult works when only they are asked for", () => {
		expect(matchesWorkAdult(work(1, null, { isAdult: true }), "only")).toBe(true);
		expect(matchesWorkAdult(work(1, null), "only")).toBe(false);
	});
});

describe("firstYear", () => {
	it("is the earliest dated year", () => {
		expect(firstYear([work(1, null), work(2, on(2026)), work(3, on(2019, 2, 7))])).toBe(2019);
	});

	it("is unknown when nothing is dated", () => {
		expect(firstYear([work(1, null)])).toBeNull();
		expect(firstYear([])).toBeNull();
	});
});

describe("formatWorkDate", () => {
	it("says as much as the source published and no more", () => {
		expect(formatWorkDate(work(1, on(2026, 7, 4)), "en-GB")).toBe("4 July 2026");
		expect(formatWorkDate(work(1, on(2026, 7)), "en-GB")).toBe("July 2026");
		expect(formatWorkDate(work(1, on(2027)), "en-GB")).toBe("2027");
	});

	it("has nothing to say about an undated work", () => {
		expect(formatWorkDate(work(1, null), "en-GB")).toBeNull();
	});
});

describe("formatWorkCount", () => {
	it("agrees in number", () => {
		expect(formatWorkCount(1)).toBe("1 work");
		expect(formatWorkCount(12)).toBe("12 works");
		expect(formatWorkCount(0)).toBe("0 works");
	});
});

describe("formatFetchedAt", () => {
	it("names the day", () => {
		expect(formatFetchedAt("2026-11-10T12:00:00Z", "en-GB")).toBe("10 November 2026");
	});
});
