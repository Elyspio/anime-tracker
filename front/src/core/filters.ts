import type { StatusFilter } from "@/core/binge";
import type { DubFilter } from "@/core/dub";
import { episodicFormats, noRangeFilter, type RangeFilter, type SortKey } from "@/core/ranking";
import type { AnimeFormat } from "@/core/api/types";

/**
 * Everything the season toolbar can be set to. One object rather than nine pieces of state, so it
 * can live above the page: a detour through a studio's filmography must not cost the reader the
 * triage they had set up.
 */
export interface SeasonFilters {
	title: string;
	status: StatusFilter;
	genres: string[];
	studio: string;
	range: RangeFilter;
	sort: SortKey;
	formats: AnimeFormat[];
	includeAdult: boolean;
	dub: DubFilter;
}

/** Highest-rated first, episodic formats only: the view the product opens on. */
export const defaultSeasonFilters: SeasonFilters = {
	title: "",
	status: "all",
	genres: [],
	studio: "",
	range: noRangeFilter,
	sort: "score",
	formats: [...episodicFormats],
	includeAdult: false,
	dub: "any",
};

function sameFormats(left: readonly AnimeFormat[], right: readonly AnimeFormat[]): boolean {
	return left.length === right.length && right.every((format) => left.includes(format));
}

/** How many filters differ from the default. The sort is a preference, not a filter, and is not counted. */
export function countActiveFilters(filters: SeasonFilters): number {
	return [
		filters.title !== "",
		filters.status !== "all",
		filters.genres.length > 0,
		filters.studio !== "",
		filters.range.minScore !== 0,
		filters.range.minVotes !== 0,
		!sameFormats(filters.formats, episodicFormats),
		filters.includeAdult,
		filters.dub !== "any",
	].filter(Boolean).length;
}
