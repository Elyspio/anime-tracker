import { Box, type SxProps, type Theme } from "@mui/material";
import type { ReactNode } from "react";
import { Eyebrow } from "@/view/components/Eyebrow";

interface Props {
	label: string;
	children: ReactNode;
	sx?: SxProps<Theme>;
}

/**
 * The toolbar's one control shape: a hairline box carrying its own label, with a borderless input
 * inside it. Every filter uses it, so the row reads as a single object rather than seven widgets.
 */
export function FilterField({ label, children, sx }: Props) {
	return (
		<Box
			sx={[
				{
					display: "flex",
					alignItems: "center",
					gap: 1,
					minHeight: 34,
					pl: 1.5,
					pr: 1.25,
					border: 1,
					borderColor: "divider",
					borderRadius: 1,
					bgcolor: "background.paper",
				},
				// SxProps may be an object, a theme callback or an array; spreading it would drop the last two.
				...(Array.isArray(sx) ? sx : [sx]),
			]}
		>
			<Eyebrow sx={{ fontSize: 10 }}>{label}</Eyebrow>
			{children}
		</Box>
	);
}
