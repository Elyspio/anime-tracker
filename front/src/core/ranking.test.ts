import { describe, expect, it } from "vitest";
import {
	matchesAdult,
	matchesFormat,
	matchesRange,
	sortAnimes,
	episodicFormats,
} from "@/core/ranking";
import type { Anime, AnimeFormat } from "@/core/api/types";

function anime(
	title: string,
	votesCount: number | null,
	score: number | null,
	overrides: Partial<Anime> = {},
): Anime {
	return {
		id: title,
		sourceId: 1,
		date: { year: 2026, season: "Summer" },
		title,
		description: "",
		studio: "",
		imageUrl: "",
		url: "",
		format: "Tv",
		isAdult: false,
		score,
		popularity: 0,
		votesCount,
		episodesCount: 12,
		genres: [],
		episodes: [],
		binge: {
			status: "Announced",
			bingeableAt: "2026-09-20",
			releasedEpisodes: 4,
			totalEpisodes: 12,
		},
		...overrides,
	};
}

describe("sortAnimes", () => {
	it("puts the most rated anime first", () => {
		const sorted = sortAnimes([anime("few", 10, 9.5), anime("many", 800, 7)], "votes");

		expect(sorted.map((item) => item.title)).toEqual(["many", "few"]);
	});

	it("breaks a tie on the vote count with the grade", () => {
		const sorted = sortAnimes([anime("worse", 100, 6.2), anime("better", 100, 8.4)], "votes");

		expect(sorted.map((item) => item.title)).toEqual(["better", "worse"]);
	});

	it("sinks an anime nobody has rated", () => {
		const sorted = sortAnimes([anime("unrated", null, null), anime("rated", 1, 5)], "votes");

		expect(sorted.map((item) => item.title)).toEqual(["rated", "unrated"]);
	});

	it("leaves the array it was given alone", () => {
		const input = [anime("few", 10, 9.5), anime("many", 800, 7)];

		sortAnimes(input, "votes");

		expect(input.map((item) => item.title)).toEqual(["few", "many"]);
	});
});

describe("matchesRange", () => {
	it("keeps everything when both thresholds are zero", () => {
		expect(matchesRange(anime("unrated", null, null), { minScore: 0, minVotes: 0 })).toBe(true);
	});

	it("excludes an anime graded below the minimum", () => {
		expect(matchesRange(anime("low", 500, 6.4), { minScore: 7, minVotes: 0 })).toBe(false);
		expect(matchesRange(anime("high", 500, 7.1), { minScore: 7, minVotes: 0 })).toBe(true);
	});

	it("excludes an anime with too few votes", () => {
		expect(matchesRange(anime("fresh", 12, 9.9), { minScore: 0, minVotes: 50 })).toBe(false);
	});

	it("excludes an unrated anime as soon as a threshold is asked for", () => {
		// "At least 7/10" means "show me graded shows above 7", not "and also the ungraded ones".
		expect(matchesRange(anime("unrated", null, null), { minScore: 7, minVotes: 0 })).toBe(
			false,
		);
		expect(matchesRange(anime("unrated", null, null), { minScore: 0, minVotes: 1 })).toBe(
			false,
		);
	});
});

describe("matchesFormat", () => {
	it.each<[AnimeFormat, boolean]>([
		["Tv", true],
		["Ona", true],
		["TvShort", true],
		["Movie", false],
		["Special", false],
		["Ova", false],
	])("shows %s by default: %s", (format, expected) => {
		expect(matchesFormat(anime("x", 1, 8, { format }), episodicFormats)).toBe(expected);
	});

	it("shows everything when no format is selected", () => {
		// An empty selection is "no filter", not "nothing" — an empty grid would look like a bug.
		expect(matchesFormat(anime("x", 1, 8, { format: "Movie" }), [])).toBe(true);
	});
});

describe("matchesAdult", () => {
	it("hides adult entries by default", () => {
		expect(matchesAdult(anime("x", 1, 8, { isAdult: true }), false)).toBe(false);
		expect(matchesAdult(anime("x", 1, 8, { isAdult: false }), false)).toBe(true);
	});

	it("shows them once asked for", () => {
		expect(matchesAdult(anime("x", 1, 8, { isAdult: true }), true)).toBe(true);
	});
});
