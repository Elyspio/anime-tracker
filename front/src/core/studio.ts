import { animeFormats, type AnimeFormat, type StudioWork } from "@/core/api/types";
import { matchesAdultFilter, type AdultFilter } from "@/core/ranking";

/** A year's worth of a filmography. `year` is null for the works nobody has dated yet. */
export interface WorkGroup {
	year: number | null;
	works: StudioWork[];
}

/**
 * Cuts the filmography into years. The API already sends it newest first with the undated works
 * ahead, so this only draws the lines: it never reorders, and two sides can never disagree on what
 * "newest" means.
 */
export function groupByYear(works: readonly StudioWork[]): WorkGroup[] {
	const groups: WorkGroup[] = [];

	for (const work of works) {
		const year = work.startDate?.year ?? null;
		const last = groups.at(-1);

		if (last && last.year === year) last.works.push(work);
		else groups.push({ year, works: [work] });
	}

	return groups;
}

export interface FormatCount {
	format: AnimeFormat;
	count: number;
}

/** The formats a studio has actually worked in, in the order the product always lists them. */
export function countFormats(works: readonly StudioWork[]): FormatCount[] {
	return animeFormats.map((format) => ({ format, count: works.filter((work) => work.format === format).length })).filter((entry) => entry.count > 0);
}

/** The empty selection is "All". */
export function matchesWorkFormat(work: StudioWork, selected: AnimeFormat | null): boolean {
	return selected === null || work.format === selected;
}

/** Adult works are stored like any other and hidden until asked for, as in a season. */
export function matchesWorkAdult(work: StudioWork, filter: AdultFilter): boolean {
	return matchesAdultFilter(work.isAdult, filter);
}

/** The year of the studio's earliest dated work, for "since 2019". Null when nothing is dated. */
export function firstYear(works: readonly StudioWork[]): number | null {
	const years = works.flatMap((work) => (work.startDate ? [work.startDate.year] : []));

	return years.length === 0 ? null : Math.min(...years);
}

/**
 * As much of the start date as the source published, and no more: "4 July 2026", "July 2026" or
 * "2026". A missing day is not the 1st.
 */
export function formatWorkDate(work: StudioWork, locale?: string): string | null {
	const start = work.startDate;
	if (!start) return null;
	if (start.month === null) return String(start.year);

	const date = new Date(start.year, start.month - 1, start.day ?? 1);

	return date.toLocaleDateString(locale, start.day === null ? { month: "long", year: "numeric" } : { day: "numeric", month: "long", year: "numeric" });
}

export function formatWorkCount(count: number): string {
	return `${count} ${count === 1 ? "work" : "works"}`;
}

/** The day the filmography was last read from the source, in the reader's own locale. */
export function formatFetchedAt(iso: string, locale?: string): string {
	return new Date(iso).toLocaleDateString(locale, { day: "numeric", month: "long", year: "numeric" });
}
