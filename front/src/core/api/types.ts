// Mirrors the backend JSON. Enums are serialised by name (JsonStringEnumConverter), so these
// unions must stay in step with the C# enums they shadow.

/**
 * Every union mirroring a C# enum is listed once in a `Record<Union, true>` and its members read
 * back off that. The Record is what makes the compiler complain if a member is added to the union
 * and forgotten here — a plain array would happily stay short, and the missing value would only
 * show up as an unhandled string at runtime.
 */
function membersOf<T extends string>(members: Record<T, true>): readonly T[] {
	return Object.keys(members) as T[];
}

export type AnimeSeason = "Winter" | "Spring" | "Summer" | "Fall";

export const animeSeasons = membersOf<AnimeSeason>({
	Winter: true,
	Spring: true,
	Summer: true,
	Fall: true,
});

export interface AnimeDate {
	year: number;
	season: AnimeSeason;
}

export interface Episode {
	number: number;
	/** ISO date (yyyy-MM-dd) of the Japanese broadcast day. */
	releaseDate: string;
}

export type AnimeFormat =
	| "Unknown"
	| "Tv"
	| "TvShort"
	| "Ona"
	| "Ova"
	| "Movie"
	| "Special"
	| "Music";

export const animeFormats = membersOf<AnimeFormat>({
	Unknown: true,
	Tv: true,
	TvShort: true,
	Ona: true,
	Ova: true,
	Movie: true,
	Special: true,
	Music: true,
});

export type BingeStatus = "BingeableNow" | "Announced" | "Estimated" | "UnknownEnd";

export const bingeStatuses = membersOf<BingeStatus>({
	BingeableNow: true,
	Announced: true,
	Estimated: true,
	UnknownEnd: true,
});

export interface BingePrediction {
	status: BingeStatus;
	/** ISO date of the last episode. Null when `status` is "UnknownEnd". */
	bingeableAt: string | null;
	releasedEpisodes: number;
	totalEpisodes: number | null;
}

export type RefreshStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Interrupted";

export const refreshStatuses = membersOf<RefreshStatus>({
	Queued: true,
	Running: true,
	Succeeded: true,
	Failed: true,
	Interrupted: true,
});

/** One execution of a season refresh. `POST /api/animes/refresh` answers with one, 202 or 409. */
export interface RefreshRun {
	id: string;
	runId: string;
	date: AnimeDate;
	status: RefreshStatus;
	/** Animes stored for the season. Zero until the run has finished successfully. */
	total: number;
	/** ISO timestamps. */
	startedAt: string;
	updatedAt: string;
	finishedAt: string | null;
	error: string | null;
}

export interface Anime {
	id: string;
	/** The source's own identifier. Stable across renames, unlike a title. */
	sourceId: number;
	date: AnimeDate;
	title: string;
	/** English, native and synonym titles, for search only. Empty until the season is refreshed. */
	alternativeTitles: string[];
	description: string;
	studio: string;
	imageUrl: string;
	url: string;
	format: AnimeFormat;
	isAdult: boolean;
	/** Average grade out of 10. Null until somebody has graded it. */
	score: number | null;
	/** Members who listed the show, whether or not they graded it. Not the number of ratings. */
	popularity: number;
	/** How many members graded it. */
	votesCount: number | null;
	episodesCount: number | null;
	genres: string[];
	episodes: Episode[];
	binge: BingePrediction;
}
