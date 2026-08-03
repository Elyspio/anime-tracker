import { Chip, Tooltip } from "@mui/material";
import { formatBingeChip } from "@/core/binge";
import type { BingePrediction } from "@/core/api/types";

interface Props {
	binge: BingePrediction;
	now: Date;
	size?: "small" | "medium";
}

/** The countdown, rendered the same way in both views so the two never disagree. */
export function BingeBadge({ binge, now, size = "medium" }: Props) {
	const chip = formatBingeChip(binge, now);

	return (
		<Tooltip title={chip.title}>
			<Chip
				label={chip.label}
				size={size}
				color={chip.tone === "default" ? "default" : chip.tone}
				variant={chip.tone === "default" ? "outlined" : "filled"}
			/>
		</Tooltip>
	);
}
