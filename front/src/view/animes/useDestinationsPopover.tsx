import { useState, type ReactNode } from "react";
import { DestinationsPopover, type DestinationsTarget } from "@/view/animes/DestinationsPopover";
import type { Anime } from "@/core/api/types";

/**
 * One popover per list, not one per card: a season is a hundred and fifty of them and only one can
 * be open. The list renders `popover` once and calls `open` from whatever the reader clicked.
 */
export function useDestinationsPopover(): { open: (anime: Anime, anchor: HTMLElement) => void; popover: ReactNode } {
	const [target, setTarget] = useState<DestinationsTarget | null>(null);

	return {
		open: (anime, anchor) => setTarget({ anime, anchor }),
		popover: <DestinationsPopover target={target} onClose={() => setTarget(null)} />,
	};
}
