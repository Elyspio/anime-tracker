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

/** A place the source lists the anime on. Always an absolute http(s) link to a page, never a home page. */
export interface StreamingLink {
	/** The platform's name as the source spells it. */
	site: string;
	url: string;
}

export type AnimeFormat = "Unknown" | "Tv" | "TvShort" | "Ona" | "Ova" | "Movie" | "Special" | "Music";

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

/** A season refresh from AniList, or the French dub sync that follows every successful one. */
export type RefreshKind = "Season" | "Dub";

export const refreshKinds = membersOf<RefreshKind>({
	Season: true,
	Dub: true,
});

/** One execution of a season refresh or dub sync. `POST /api/animes/refresh` answers with one, 202 or 409. */
export interface RefreshRun {
	id: string;
	runId: string;
	date: AnimeDate;
	kind: RefreshKind;
	status: RefreshStatus;
	/** Animes stored for the season; for a dub sync, animes matched on a platform so far. */
	total: number;
	/** ISO timestamps. */
	startedAt: string;
	updatedAt: string;
	finishedAt: string | null;
	error: string | null;
}

export type DubPlatform = "Crunchyroll" | "Adn";

export const dubPlatforms = membersOf<DubPlatform>({
	Crunchyroll: true,
	Adn: true,
});

export type DubMatchStatus = "Matched" | "NotFound" | "Unaligned";

export const dubMatchStatuses = membersOf<DubMatchStatus>({
	Matched: true,
	NotFound: true,
	Unaligned: true,
});

export type DubMatchMethod = "Link" | "Search" | "Pinned";

export const dubMatchMethods = membersOf<DubMatchMethod>({
	Link: true,
	Search: true,
	Pinned: true,
});

export type DubOverrideMode = "Auto" | "Pinned" | "Blocked";

export const dubOverrideModes = membersOf<DubOverrideMode>({
	Auto: true,
	Pinned: true,
	Blocked: true,
});

/** The French dub on one platform, computed by the API as of today. */
export interface DubAvailability {
	platform: DubPlatform;
	/** The series page on the platform. */
	url: string;
	/** Episodes out in French, among the announced ones when the total is known. */
	frenchEpisodes: number;
	totalEpisodes: number | null;
	/** At least one episode aired, and every aired one is out in French here. */
	upToDate: boolean;
	/** Every announced episode is out in French here. */
	complete: boolean;
	/** ISO timestamp of the sync that measured it. */
	checkedAt: string;
}

/** A match worth an admin's look, from `GET /api/dubs/cases`. */
export interface DubCase {
	sourceId: number;
	title: string;
	platform: DubPlatform;
	/** Null when the anime was never matched on this platform. */
	status: DubMatchStatus | null;
	method: DubMatchMethod | null;
	seriesTitle: string | null;
	seriesUrl: string | null;
	frenchEpisodes: number;
	override: DubOverrideMode;
	checkedAt: string | null;
}

export interface DubOverrideRequest {
	mode: DubOverrideMode;
	url?: string;
}

/** The case once the override is saved. `error` says why it could not be applied yet. */
export interface DubOverrideResult {
	case: DubCase;
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
	/** One per platform. Empty until the season is refreshed. */
	streamingLinks: StreamingLink[];
	binge: BingePrediction;
	/** Every platform the anime was matched on, best first. Empty: the dub is unknown, not missing. */
	dubs: DubAvailability[];
}
