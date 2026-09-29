import type { Anime } from "@/core/api/types";

export interface Destination {
	site: string;
	url: string;
}

const source = "AniList";

/**
 * Where a click on an anime can lead: its page on the source first, since every anime has one and it
 * is what the whole card used to link to, then each platform it can be watched on. A platform the
 * source names "anilist" is not listed twice.
 */
export function destinationsOf(anime: Anime): Destination[] {
	const destinations: Destination[] = anime.url === "" ? [] : [{ site: source, url: anime.url }];
	const seen = new Set(destinations.map((destination) => destination.site.toLowerCase()));

	for (const link of anime.streamingLinks) {
		const key = link.site.toLowerCase();
		if (seen.has(key)) continue;

		seen.add(key);
		destinations.push({ site: link.site, url: link.url });
	}

	return destinations;
}
