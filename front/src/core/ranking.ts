import type { Anime, AnimeFormat } from "@/core/api/types";

export type SortKey = "votes" | "score" | "binge";

export const sortLabels: Record<SortKey, string> = {
	votes: "Number of ratings",
	score: "Rating",
	binge: "Bingeable soonest",
};

/**
 * An anime nobody has rated has no vote count at all. Treated as zero it sinks to the bottom,
 * which is the honest place for it: nothing is known about how it was received.
 */
function votes(anime: Anime): number {
	return anime.votesCount ?? 0;
}

function score(anime: Anime): number {
	return anime.score ?? 0;
}

/** Sorted ascending, so an unknown end has to sort last rather than first. */
function bingeableAt(anime: Anime): string {
	return anime.binge.bingeableAt ?? "9999-12-31";
}

const comparators: Record<SortKey, (a: Anime, b: Anime) => number> = {
	// Ties on the vote count fall back to the grade, which is the only thing that separates two
	// shows the same number of people bothered to rate.
	votes: (a, b) => votes(b) - votes(a) || score(b) - score(a),
	score: (a, b) => score(b) - score(a) || votes(b) - votes(a),
	binge: (a, b) => bingeableAt(a).localeCompare(bingeableAt(b)) || b.popularity - a.popularity,
};

/** Returns a new array: the query cache owns the one it was given. */
export function sortAnimes(animes: readonly Anime[], key: SortKey): Anime[] {
	return [...animes].sort(comparators[key]);
}

export interface RangeFilter {
	minScore: number;
	minVotes: number;
}

export const noRangeFilter: RangeFilter = { minScore: 0, minVotes: 0 };

/**
 * Both thresholds exclude anything unrated: asking for "at least 7/10" is asking to be shown shows
 * that have a grade, not shows whose grade is unknown.
 */
export function matchesRange(anime: Anime, filter: RangeFilter): boolean {
	if (filter.minScore > 0 && (anime.score ?? 0) < filter.minScore) return false;

	return !(filter.minVotes > 0 && votes(anime) < filter.minVotes);
}

export const formatLabels: Record<AnimeFormat, string> = {
	Unknown: "Other",
	Tv: "TV",
	TvShort: "TV short",
	Ona: "ONA",
	Ova: "OVA",
	Movie: "Movie",
	Special: "Special",
	Music: "Music",
};

/**
 * What the grid shows until told otherwise: the formats that release episode by episode, which are
 * the only ones a countdown says anything about. A movie is bingeable the day it comes out.
 */
export const episodicFormats: readonly AnimeFormat[] = ["Tv", "Ona", "TvShort"];

export function matchesFormat(anime: Anime, selected: readonly AnimeFormat[]): boolean {
	return selected.length === 0 || selected.includes(anime.format);
}

/** Adult entries are fetched and stored like any other, and hidden until explicitly asked for. */
export function matchesAdult(anime: Anime, includeAdult: boolean): boolean {
	return includeAdult || !anime.isAdult;
}
