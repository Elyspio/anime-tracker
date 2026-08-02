// Mirrors the backend JSON. Enums are serialised by name (JsonStringEnumConverter), so these
// unions must stay in step with the C# enums they shadow.

export type AnimeSeason = "Winter" | "Spring" | "Summer" | "Fall";

export const animeSeasons: readonly AnimeSeason[] = ["Winter", "Spring", "Summer", "Fall"];

export interface AnimeDate {
	year: number;
	season: AnimeSeason;
}

export interface Episode {
	number: number;
	title: string;
	url: string | null;
	/** ISO date (yyyy-MM-dd), or null when the schedule does not announce one. */
	releaseDate: string | null;
}

export interface AnimeTag {
	name: string;
	url: string;
}

export type BingeStatus = "BingeableNow" | "Estimated" | "UnknownEnd";

export interface BingePrediction {
	status: BingeStatus;
	/** ISO date of the last episode. Null when `status` is "UnknownEnd". */
	bingeableAt: string | null;
	releasedEpisodes: number;
	totalEpisodes: number | null;
}

export interface Anime {
	id: string;
	date: AnimeDate;
	title: string;
	studio: string;
	description: string;
	imageUrl: string;
	url: string;
	score: number | null;
	popularity: number;
	episodesCount: number | null;
	episodes: Episode[];
	tags: AnimeTag[];
	binge: BingePrediction;
}
