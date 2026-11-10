import { animeSeasons, type AnimeDate } from "@/core/api/types";

/** The current season, whichever it is. A link to it never goes stale. */
export const homePath = "/";

/** `/2026/fall` — lower case, because that is how anyone would type it. */
export function seasonPath({ year, season }: AnimeDate): string {
	return `/${year}/${season.toLowerCase()}`;
}

/**
 * Reads back what `seasonPath` wrote. The address bar is user input: anything that is not a
 * four-digit year followed by a season name is refused rather than guessed at.
 */
export function parseSeasonPath(year: string | undefined, season: string | undefined): AnimeDate | null {
	if (year === undefined || season === undefined || !/^\d{4}$/.test(year)) return null;

	const match = animeSeasons.find((candidate) => candidate.toLowerCase() === season.toLowerCase());

	return match ? { year: Number(year), season: match } : null;
}

export function studioPath(studioId: number): string {
	return `/studios/${studioId}`;
}

/**
 * Where an anime's studio is found. An anime stored before studios had an id carries only a name:
 * its link goes through the anime instead, and the API names the studio on the way. An anime that
 * credits no studio at all has nowhere to go.
 */
export function studioPathOf(anime: { sourceId: number; studio: string; studioId: number | null }): string | null {
	if (anime.studio.trim() === "") return null;

	return anime.studioId !== null ? studioPath(anime.studioId) : `/animes/${anime.sourceId}/studio`;
}

/** AniList ids are positive integers; nothing else is worth a request. */
export function parseStudioId(raw: string | undefined): number | null {
	if (raw === undefined || !/^[1-9]\d{0,9}$/.test(raw)) return null;

	const id = Number(raw);

	return id <= 2_147_483_647 ? id : null;
}
