import type { RefreshKind, RefreshRun, RefreshStatus } from "@/core/api/types";

export const statusLabels: Record<RefreshStatus, string> = {
	Queued: "Queued",
	Running: "Running",
	Succeeded: "Done",
	Failed: "Failed",
	Interrupted: "Interrupted",
};

/**
 * Success green is reserved for "bingeable" everywhere in this product, so a finished run is not
 * allowed to borrow it — it uses the neutral tone instead.
 */
export const statusTones: Record<RefreshStatus, "default" | "info" | "error" | "warning"> = {
	Queued: "default",
	Running: "info",
	Succeeded: "default",
	Failed: "error",
	Interrupted: "warning",
};

export const kindLabels: Record<RefreshKind, string> = {
	Season: "Season",
	Dub: "French dub",
};

export function isRunActive(run: RefreshRun): boolean {
	return run.status === "Queued" || run.status === "Running";
}

/**
 * The season refresh in flight for a season, if one is. A dub sync of the season does not count: it
 * runs for minutes after the refresh, and the refresh button stays usable meanwhile.
 */
export function findRunFor(runs: readonly RefreshRun[] | undefined, year: number, season: string): RefreshRun | undefined {
	return runs?.find((run) => run.kind === "Season" && isRunActive(run) && run.date.year === year && run.date.season === season);
}

/** Formatted in the reader's own locale — the app has no opinion on date order. */
export function formatInstant(iso: string): string {
	return new Date(iso).toLocaleString(undefined, {
		day: "numeric",
		month: "short",
		hour: "2-digit",
		minute: "2-digit",
	});
}

/**
 * How long the run took, or has been going. A refresh is a single fetch and normally lands in
 * about a second, so seconds are the useful unit below a minute; a dub sync takes a few minutes.
 */
export function formatDuration(run: RefreshRun): string {
	const end = new Date(run.finishedAt ?? run.updatedAt).getTime();
	const seconds = Math.max(0, Math.round((end - new Date(run.startedAt).getTime()) / 1000));

	if (seconds < 1) return "under a second";
	if (seconds < 60) return `${seconds}s`;

	return `${Math.round(seconds / 60)} min`;
}
