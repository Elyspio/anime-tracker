import { platformNames, platformSites } from "@/core/dub";
import type { Anime } from "@/core/api/types";

export interface Destination {
	site: string;
	url: string;
	/** Episodes out in French there, when the dub sync matched the anime on that platform. */
	frenchEpisodes?: number;
}

const source = "AniList";

/**
 * Where a click on an anime can lead: its page on the source first, since every anime has one and it
 * is what the whole card used to link to, then each platform it can be watched on. A series the dub
 * sync matched replaces AniList's own link to that platform — it is the one it checked — and comes
 * before the links nobody checked.
 */
export function destinationsOf(anime: Anime): Destination[] {
	const destinations: Destination[] = anime.url === "" ? [] : [{ site: source, url: anime.url }];
	const seen = new Set(destinations.map((destination) => destination.site.toLowerCase()));

	for (const dub of anime.dubs) {
		platformSites[dub.platform].forEach((site) => seen.add(site));
		destinations.push({ site: platformNames[dub.platform], url: dub.url, frenchEpisodes: dub.frenchEpisodes });
	}

	for (const link of anime.streamingLinks) {
		const key = link.site.toLowerCase();
		if (seen.has(key)) continue;

		seen.add(key);
		destinations.push({ site: link.site, url: link.url });
	}

	return destinations;
}
