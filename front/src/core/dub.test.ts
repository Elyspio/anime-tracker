import { describe, expect, it } from "vite-plus/test";
import { badgeDub, caseReason, formatDubBadge, matchesDub } from "@/core/dub";
import type { Anime, DubAvailability, DubCase } from "@/core/api/types";

function dub(overrides: Partial<DubAvailability> = {}): DubAvailability {
	return {
		platform: "Crunchyroll",
		url: "https://www.crunchyroll.com/series/G1",
		frenchEpisodes: 4,
		totalEpisodes: 12,
		upToDate: true,
		complete: false,
		checkedAt: "2026-09-29T03:00:00Z",
		...overrides,
	};
}

function anime(dubs: DubAvailability[]): Anime {
	return {
		id: "1",
		sourceId: 1,
		date: { year: 2026, season: "Summer" },
		title: "Title",
		alternativeTitles: [],
		description: "",
		studio: "",
		imageUrl: "",
		url: "",
		format: "Tv",
		isAdult: false,
		score: null,
		popularity: 0,
		votesCount: null,
		episodesCount: 12,
		genres: [],
		episodes: [],
		streamingLinks: [],
		binge: { status: "Announced", bingeableAt: "2026-09-20", releasedEpisodes: 4, totalEpisodes: 12 },
		dubs,
	};
}

describe("matchesDub", () => {
	it("keeps everything when no dub is asked for", () => {
		expect(matchesDub(anime([]), "any")).toBe(true);
	});

	it("excludes an anime whose dub is unknown", () => {
		// No match on any platform says nothing about the dub; it must not pass for up to date.
		expect(matchesDub(anime([]), "upToDate")).toBe(false);
		expect(matchesDub(anime([]), "complete")).toBe(false);
	});

	it("keeps an up-to-date dub under up to date but not under complete", () => {
		expect(matchesDub(anime([dub()]), "upToDate")).toBe(true);
		expect(matchesDub(anime([dub()]), "complete")).toBe(false);
	});

	it("keeps a complete dub under both", () => {
		const complete = anime([dub({ upToDate: true, complete: true, frenchEpisodes: 12 })]);

		expect(matchesDub(complete, "upToDate")).toBe(true);
		expect(matchesDub(complete, "complete")).toBe(true);
	});

	it("asks one platform to satisfy the whole condition", () => {
		// The API already decides per platform; two half-dubs do not add up to one.
		expect(matchesDub(anime([dub({ upToDate: false }), dub({ platform: "Adn", upToDate: false })]), "upToDate")).toBe(false);
	});
});

describe("badgeDub", () => {
	it("badges the best platform once something is out in French", () => {
		expect(badgeDub(anime([dub(), dub({ platform: "Adn" })]))?.platform).toBe("Crunchyroll");
	});

	it("leaves a matched anime with no French episode unbadged", () => {
		expect(badgeDub(anime([dub({ frenchEpisodes: 0, upToDate: false })]))).toBeUndefined();
	});
});

describe("formatDubBadge", () => {
	it("says how many episodes are out in French, of how many, and where", () => {
		expect(formatDubBadge(dub({ platform: "Adn", frenchEpisodes: 8 }))).toBe("FR dub 8/12 · ADN");
	});

	it("shows an unannounced total as unknown", () => {
		expect(formatDubBadge(dub({ totalEpisodes: null }))).toBe("FR dub 4/? · Crunchyroll");
	});
});

describe("caseReason", () => {
	const base: DubCase = {
		sourceId: 1,
		title: "Title",
		platform: "Crunchyroll",
		status: "Matched",
		method: "Search",
		seriesTitle: "Series",
		seriesUrl: "https://www.crunchyroll.com/series/G1",
		frenchEpisodes: 0,
		override: "Auto",
		checkedAt: null,
	};

	it("asks for a check on a match made by title alone", () => {
		expect(caseReason(base)).toContain("title alone");
	});

	it("tells a pinned series that stopped lining up from an automatic one", () => {
		expect(caseReason({ ...base, status: "Unaligned", method: "Pinned", override: "Pinned" })).toContain("pinned series");
		expect(caseReason({ ...base, status: "Unaligned", method: "Link" })).toContain("A series was found");
	});

	it("says a blocked platform is blocked whatever the last match was", () => {
		expect(caseReason({ ...base, status: null, override: "Blocked" })).toContain("Blocked");
	});
});
