import { Typography, type SxProps, type Theme, type TypographyProps } from "@mui/material";
import type { ReactNode } from "react";
import { fonts } from "@/config/tokens";

interface Props {
	children: ReactNode;
	variant?: TypographyProps["variant"];
	sx?: SxProps<Theme>;
}

/**
 * Geist Mono with tabular figures. Every number a reader compares down a column — episode counts,
 * ratings, popularity, durations — is set in it, so digits line up and a "1" is as wide as a "9".
 */
export function Mono({ children, variant = "caption", sx }: Props) {
	return (
		<Typography
			variant={variant}
			component="span"
			sx={{ fontFamily: fonts.mono, fontVariantNumeric: "tabular-nums", ...sx }}
		>
			{children}
		</Typography>
	);
}
