import { Chip, Tooltip } from "@mui/material";
import { formatBingeChip, type BingeChip } from "@/core/binge";
import type { BingePrediction } from "@/core/api/types";

type Tone = BingeChip["tone"];

/**
 * Emerald is the only saturated colour the grid is allowed, and it means "watch it now". A
 * countdown stays neutral — it is a wait, not an answer — and an unannounced end is amber, because
 * it is the one row nothing can be planned around.
 */
const hues: Record<Tone, "success" | "warning" | null> = {
	success: "success",
	default: "warning",
	info: null,
};

/** The dot the table uses where a chip would crowd the row. */
export function bingeDotColor(tone: Tone): string {
	const hue = hues[tone];

	return hue === null ? "text.disabled" : `${hue}.main`;
}

interface Props {
	binge: BingePrediction;
	now: Date;
	size?: "small" | "medium";
}

/** The countdown, rendered the same way in both views so the two never disagree. */
export function BingeBadge({ binge, now, size = "medium" }: Props) {
	const chip = formatBingeChip(binge, now);
	const hue = hues[chip.tone];

	return (
		<Tooltip title={chip.title}>
			<Chip
				label={chip.label}
				size={size}
				sx={
					hue === null
						? undefined
						: {
								color: `${hue}.main`,
								borderColor: `${hue}.main`,
								// A tint of the tone's own colour, so the chip holds over cover art.
								bgcolor: (theme) =>
									`color-mix(in srgb, ${theme.palette[hue].main} 16%, ${theme.palette.background.paper})`,
							}
				}
			/>
		</Tooltip>
	);
}
