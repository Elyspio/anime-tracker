import type { Anime, DubAvailability, DubCase, DubPlatform } from "@/core/api/types";

/** Ordered: every complete dub is also up to date, so each step narrows the one before. */
export type DubFilter = "any" | "upToDate" | "complete";

export const dubFilterLabels: Record<DubFilter, string> = {
	any: "Any",
	upToDate: "Up to date",
	complete: "Complete",
};

export const platformNames: Record<DubPlatform, string> = {
	Crunchyroll: "Crunchyroll",
	Adn: "ADN",
};

/**
 * Other names AniList lists the same platforms under, lower-cased. The popover uses them to put the
 * resolved series page in place of AniList's own link, rather than beside it.
 */
export const platformSites: Record<DubPlatform, readonly string[]> = {
	Crunchyroll: ["crunchyroll"],
	Adn: ["adn", "animation digital network"],
};

/**
 * One platform has to satisfy the whole condition: nobody binges across two subscriptions. An anime
 * with no match is excluded from both — its dub is unknown, which is not the same as out.
 */
export function matchesDub(anime: Anime, filter: DubFilter): boolean {
	if (filter === "upToDate") return anime.dubs.some((dub) => dub.upToDate);
	if (filter === "complete") return anime.dubs.some((dub) => dub.complete);

	return true;
}

/** The dub worth a badge: the best platform, and only once something is out in French there. */
export function badgeDub(anime: Anime): DubAvailability | undefined {
	const best = anime.dubs[0];

	return best && best.frenchEpisodes > 0 ? best : undefined;
}

/** "FR dub 8/12 · ADN". The total reads "?" when AniList has not announced one. */
export function formatDubBadge(dub: DubAvailability): string {
	return `FR dub ${dub.frenchEpisodes}/${dub.totalEpisodes ?? "?"} · ${platformNames[dub.platform]}`;
}

/** What the badge's tooltip says: where it was measured, how far it is, and when. */
export function formatDubTitle(dub: DubAvailability): string {
	const state = dub.complete ? "Every episode is out in French" : dub.upToDate ? "In French up to the latest aired episode" : "Behind the Japanese broadcast";
	const checked = new Date(dub.checkedAt).toLocaleString(undefined, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });

	return `${state} on ${platformNames[dub.platform]}. Checked ${checked}.`;
}

/** Why a match is in front of the admin, in the words of the drawer. */
export function caseReason(dubCase: DubCase): string {
	if (dubCase.override === "Blocked") return "Blocked: never matched on this platform";
	if (dubCase.status === "Unaligned")
		return dubCase.method === "Pinned" ? "The pinned series has no season that aired with this anime" : "A series was found, but no season aired with this anime";
	if (dubCase.status === "NotFound") return "AniList lists this platform, but no series matched";
	if (dubCase.status === "Matched" && dubCase.method === "Search") return "Matched on its title alone — check it is the right series";
	if (dubCase.override === "Pinned") return "Pinned";

	return "Not matched yet";
}
