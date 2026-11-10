import { useOutletContext } from "react-router";
import type { SeasonFilters } from "@/core/filters";
import type { AnimeDate } from "@/core/api/types";

/** What the layout hands the page it is showing. */
export interface LayoutContext {
	/** The season in the address bar, or the last one that was while a studio is on screen. */
	season: AnimeDate;
	/** Held by the layout so they outlive the season page: coming back from a studio keeps them. */
	filters: SeasonFilters;
	setFilters: (patch: Partial<SeasonFilters>) => void;
	resetFilters: () => void;
}

export function useLayoutContext(): LayoutContext {
	return useOutletContext<LayoutContext>();
}
