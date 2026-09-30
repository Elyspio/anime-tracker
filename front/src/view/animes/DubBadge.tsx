import { Chip, Tooltip } from "@mui/material";
import { formatDubBadge, formatDubTitle } from "@/core/dub";
import type { DubAvailability } from "@/core/api/types";

/**
 * Ink on paper, whatever the dub says. Green means "bingeable" in this product and nothing else — a
 * complete dub included — so the badge only ever reads, it never signals.
 */
export function DubBadge({ dub }: { dub: DubAvailability }) {
	return (
		<Tooltip title={formatDubTitle(dub)}>
			<Chip label={formatDubBadge(dub)} size="small" sx={{ bgcolor: "background.paper", color: "text.primary" }} />
		</Tooltip>
	);
}
