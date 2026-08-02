import { useCallback, useState } from "react";

/** Cards for browsing by cover art, table for comparing numbers across a whole season. */
export type ViewMode = "cards" | "table";

const STORAGE_KEY = "anime-tracker.view-mode";

export function isViewMode(value: unknown): value is ViewMode {
	return value === "cards" || value === "table";
}

export function readStoredViewMode(storage: Pick<Storage, "getItem"> | undefined): ViewMode {
	const stored = storage?.getItem(STORAGE_KEY);

	return isViewMode(stored) ? stored : "cards";
}

export function useViewMode(): [ViewMode, (mode: ViewMode) => void] {
	const [mode, setModeState] = useState<ViewMode>(() =>
		readStoredViewMode(typeof window === "undefined" ? undefined : window.localStorage),
	);

	const setMode = useCallback((next: ViewMode) => {
		setModeState(next);
		try {
			window.localStorage.setItem(STORAGE_KEY, next);
		} catch {
			// Private mode or disabled storage: the choice applies now but not after a reload.
		}
	}, []);

	return [mode, setMode];
}
