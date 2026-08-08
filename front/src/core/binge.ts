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

/** Formatted in the reader's own locale — the app has no opinion on date order. */
function formatDate(iso: string): string {
	return parseDate(iso).toLocaleDateString(undefined, {
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
 * week out — "in 6 weeks" is what a decision is made on, an exact date is not.
 */
export function formatBingeChip(binge: BingePrediction, now: Date): BingeChip {
	if (binge.status === "UnknownEnd") {
		return {
			label: "No end announced",
			tone: "default",
			title: "The total episode count has not been announced, so no date can be given.",
		};
	}

	if (binge.status === "BingeableNow") {
		return {
			label: "Bingeable",
			tone: "success",
			title: binge.bingeableAt
				? `Last episode aired on ${formatDate(binge.bingeableAt)}.`
				: "Every episode is out.",
		};
	}

	const days = binge.bingeableAt ? daysUntil(binge.bingeableAt, now) : 0;

	// The distinction the product has always claimed and could not make until the source started
	// publishing future dates: a broadcaster's date, or our own extrapolation.
	const title = binge.bingeableAt
		? binge.status === "Announced"
			? `Last episode scheduled for ${formatDate(binge.bingeableAt)}.`
			: `Last episode estimated for ${formatDate(binge.bingeableAt)}, from the observed cadence.`
		: "";

	if (days <= 0) return { label: "Bingeable", tone: "success", title };
	if (days <= 7) return { label: `Binge in ${plural(days, "day")}`, tone: "info", title };

	return { label: `Binge in ${plural(Math.ceil(days / 7), "week")}`, tone: "info", title };
}

function plural(count: number, unit: string): string {
	return `${count} ${unit}${count === 1 ? "" : "s"}`;
}

export type StatusFilter = "all" | "bingeable" | "soon" | "unknown";

/** "Soon" is a month out: roughly the horizon over which waiting is still a plan. */
const SOON_DAYS = 31;

export function matchesStatus(anime: Anime, filter: StatusFilter, now: Date): boolean {
	if (filter === "all") return true;
	if (filter === "unknown") return anime.binge.status === "UnknownEnd";
	if (filter === "bingeable") return anime.binge.status === "BingeableNow";

	return (
		(anime.binge.status === "Estimated" || anime.binge.status === "Announced") &&
		anime.binge.bingeableAt !== null &&
		daysUntil(anime.binge.bingeableAt, now) <= SOON_DAYS
	);
}

export function matchesGenres(anime: Anime, selected: readonly string[]): boolean {
	if (selected.length === 0) return true;

	return selected.every((genre) => anime.genres.includes(genre));
}

/** Every genre present in the loaded season, alphabetically, for the filter dropdown. */
export function collectGenres(animes: readonly Anime[]): string[] {
	return [...new Set(animes.flatMap((anime) => anime.genres))]
		.filter((genre) => genre.trim().length > 0)
		.sort((a, b) => a.localeCompare(b));
}

export const seasonLabels: Record<AnimeSeason, string> = {
	Winter: "Winter",
	Spring: "Spring",
	Summer: "Summer",
	Fall: "Fall",
};

/** The season a date falls in, mirroring the backend's three-month blocks from January. */
export function currentSeason(now: Date): { year: number; season: AnimeSeason } {
	const seasons: AnimeSeason[] = ["Winter", "Spring", "Summer", "Fall"];

	return { year: now.getFullYear(), season: seasons[Math.floor(now.getMonth() / 3)] };
}
