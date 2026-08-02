import type { Anime, AnimeSeason, BingePrediction } from "@/core/api/types";

export interface BingeChip {
	label: string;
	/** Maps to a MUI palette colour; success is reserved for "watch it now". */
	tone: "success" | "info" | "default";
	title: string;
}

const MS_PER_DAY = 86_400_000;

function parseDate(iso: string): Date {
	// The API sends a plain yyyy-MM-dd; appending the time keeps it out of UTC-shift territory.
	return new Date(`${iso}T00:00:00`);
}

function formatDate(iso: string): string {
	return parseDate(iso).toLocaleDateString("fr-FR", {
		day: "numeric",
		month: "long",
		year: "numeric",
	});
}

/** Whole days from `now` to `iso`, rounded up: a date later today still counts as one day away. */
export function daysUntil(iso: string, now: Date): number {
	const target = parseDate(iso).getTime();
	const today = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();

	return Math.ceil((target - today) / MS_PER_DAY);
}

/**
 * The one label the product is built around. Weeks rather than dates for anything more than a
 * week out — "dans 6 semaines" is what a decision is made on, an exact date is not.
 */
export function formatBingeChip(binge: BingePrediction, now: Date): BingeChip {
	if (binge.status === "UnknownEnd") {
		return {
			label: "Fin inconnue",
			tone: "default",
			title: "Le nombre total d'épisodes n'est pas annoncé : aucune date ne peut être estimée.",
		};
	}

	if (binge.status === "BingeableNow") {
		return {
			label: "Bingeable",
			tone: "success",
			title: binge.bingeableAt
				? `Dernier épisode sorti le ${formatDate(binge.bingeableAt)}.`
				: "Tous les épisodes sont sortis.",
		};
	}

	const days = binge.bingeableAt ? daysUntil(binge.bingeableAt, now) : 0;
	const title = binge.bingeableAt
		? `Dernier épisode estimé au ${formatDate(binge.bingeableAt)}, d'après la cadence observée.`
		: "";

	if (days <= 0) return { label: "Bingeable", tone: "success", title };
	if (days <= 7) return { label: `${days} j`, tone: "info", title };

	return { label: `${Math.ceil(days / 7)} sem.`, tone: "info", title };
}

export type StatusFilter = "all" | "bingeable" | "soon" | "unknown";

/** "Bientôt" is a month out: roughly the horizon over which waiting is still a plan. */
const SOON_DAYS = 31;

export function matchesStatus(anime: Anime, filter: StatusFilter, now: Date): boolean {
	if (filter === "all") return true;
	if (filter === "unknown") return anime.binge.status === "UnknownEnd";
	if (filter === "bingeable") return anime.binge.status === "BingeableNow";

	return (
		anime.binge.status === "Estimated" &&
		anime.binge.bingeableAt !== null &&
		daysUntil(anime.binge.bingeableAt, now) <= SOON_DAYS
	);
}

export function matchesTags(anime: Anime, selected: readonly string[]): boolean {
	if (selected.length === 0) return true;

	return selected.every((tag) => anime.tags.some((animeTag) => animeTag.name === tag));
}

/** Every tag present in the loaded season, alphabetically, for the filter dropdown. */
export function collectTags(animes: readonly Anime[]): string[] {
	return [...new Set(animes.flatMap((anime) => anime.tags.map((tag) => tag.name)))]
		.filter((tag) => tag.trim().length > 0)
		.sort((a, b) => a.localeCompare(b, "fr"));
}

export const seasonLabels: Record<AnimeSeason, string> = {
	Winter: "Hiver",
	Spring: "Printemps",
	Summer: "Été",
	Fall: "Automne",
};

/** The season a date falls in, mirroring the backend's three-month blocks from January. */
export function currentSeason(now: Date): { year: number; season: AnimeSeason } {
	const seasons: AnimeSeason[] = ["Winter", "Spring", "Summer", "Fall"];

	return { year: now.getFullYear(), season: seasons[Math.floor(now.getMonth() / 3)] };
}
