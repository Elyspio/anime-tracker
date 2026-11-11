import { useCallback, useState } from "react";

/** Cards for browsing by cover art, table for comparing numbers down a column. */
export type ViewMode = "cards" | "table";

/**
 * A season and a filmography each remember their own choice: a hundred posters invite browsing, and
 * four hundred works by one studio are usually better read as a list.
 */
export type ViewScope = "season" | "studio";

const STORAGE_KEYS: Record<ViewScope, string> = {
	// The season's key predates the scopes; renaming it would reset a choice people already made.
	season: "anime-tracker.view-mode",
	studio: "anime-tracker.studio-view-mode",
};

export function isViewMode(value: unknown): value is ViewMode {
	return value === "cards" || value === "table";
}

export function readStoredViewMode(storage: Pick<Storage, "getItem"> | undefined, scope: ViewScope = "season"): ViewMode {
	const stored = storage?.getItem(STORAGE_KEYS[scope]);

	return isViewMode(stored) ? stored : "cards";
}

export function useViewMode(scope: ViewScope = "season"): [ViewMode, (mode: ViewMode) => void] {
	const [mode, setModeState] = useState<ViewMode>(() => readStoredViewMode(typeof window === "undefined" ? undefined : window.localStorage, scope));

	const setMode = useCallback(
		(next: ViewMode) => {
			setModeState(next);
			try {
				window.localStorage.setItem(STORAGE_KEYS[scope], next);
			} catch {
				// Private mode or disabled storage: the choice applies now but not after a reload.
			}
		},
		[scope]
	);

	return [mode, setMode];
}
