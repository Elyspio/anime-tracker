import { Typography, type SxProps, type Theme } from "@mui/material";
import type { ReactNode } from "react";

interface Props {
	children: ReactNode;
	sx?: SxProps<Theme>;
}

/**
 * Uppercase mono at 0.08em — the design system's one licence for uppercase, and the label shape
 * every filter, table header and status line in the app shares.
 */
export function Eyebrow({ children, sx }: Props) {
	return (
		<Typography
			variant="overline"
			component="span"
			sx={{ color: "text.disabled", whiteSpace: "nowrap", ...sx }}
		>
			{children}
		</Typography>
	);
}
