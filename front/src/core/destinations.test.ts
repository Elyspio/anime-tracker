import { describe, expect, it } from "vite-plus/test";
import { destinationsOf } from "@/core/destinations";
import type { Anime, StreamingLink } from "@/core/api/types";

function anime(url: string, streamingLinks: StreamingLink[]): Anime {
	return {
		id: "1",
		sourceId: 1,
		date: { year: 2026, season: "Summer" },
		title: "Title",
		alternativeTitles: [],
		description: "",
		studio: "",
		imageUrl: "",
		url,
		format: "Tv",
		isAdult: false,
		score: null,
		popularity: 0,
		votesCount: null,
		episodesCount: 12,
		genres: [],
		episodes: [],
		streamingLinks,
		binge: { status: "UnknownEnd", bingeableAt: null, releasedEpisodes: 0, totalEpisodes: null },
	};
}

describe("destinationsOf", () => {
	it("lists the source first, then each platform in the order the source gave them", () => {
		const destinations = destinationsOf(
			anime("https://anilist.co/anime/1", [
				{ site: "Crunchyroll", url: "https://www.crunchyroll.com/series/A/x" },
				{ site: "Netflix", url: "https://www.netflix.com/title/1" },
			])
		);

		expect(destinations.map((destination) => destination.site)).toEqual(["AniList", "Crunchyroll", "Netflix"]);
		expect(destinations[0]?.url).toBe("https://anilist.co/anime/1");
	});

	it("still offers the source when no platform is known", () => {
		expect(destinationsOf(anime("https://anilist.co/anime/1", []))).toEqual([{ site: "AniList", url: "https://anilist.co/anime/1" }]);
	});

	it("leaves the source out when the anime has no page there", () => {
		// A source entry with no URL stores an empty string, and an href="" resolves to the current page.
		expect(destinationsOf(anime("", [{ site: "Netflix", url: "https://www.netflix.com/title/1" }]))).toEqual([{ site: "Netflix", url: "https://www.netflix.com/title/1" }]);
	});

	it("does not list the source twice when a platform link is named after it", () => {
		const destinations = destinationsOf(anime("https://anilist.co/anime/1", [{ site: "anilist", url: "https://anilist.co/anime/1" }]));

		expect(destinations).toHaveLength(1);
	});

	it("returns nothing for an anime with neither", () => {
		expect(destinationsOf(anime("", []))).toEqual([]);
	});
});
