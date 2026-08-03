import { createTheme, type Theme } from "@mui/material";

export type PaletteMode = "light" | "dark";

// Dark is the default (see themeMode): the grid is mostly cover art, and a dark surround keeps
// the posters the brightest thing on screen. Success green is reserved for "bingeable" — it is
// the one state the whole product exists to signal, so nothing else may use it.
const palettes = {
	dark: {
		primary: "#c084fc",
		secondary: "#f472b6",
		background: { default: "#0e0b16", paper: "#171326" },
		success: "#4ade80",
		warning: "#fbbf24",
		error: "#fb7185",
		divider: "rgba(255,255,255,0.08)",
	},
	light: {
		primary: "#7c3aed",
		secondary: "#db2777",
		background: { default: "#f5f3ff", paper: "#ffffff" },
		success: "#15803d",
		warning: "#b45309",
		error: "#e11d48",
		divider: "rgba(0,0,0,0.10)",
	},
} as const;

export function createAppTheme(mode: PaletteMode): Theme {
	const colors = palettes[mode];

	return createTheme({
		palette: {
			mode,
			primary: { main: colors.primary },
			secondary: { main: colors.secondary },
			background: colors.background,
			success: { main: colors.success },
			warning: { main: colors.warning },
			error: { main: colors.error },
			divider: colors.divider,
		},
		typography: {
			fontFamily: '"Space Grotesk Variable", system-ui, sans-serif',
			h6: { fontWeight: 700 },
		},
		shape: { borderRadius: 12 },
		components: {
			MuiTableCell: {
				styleOverrides: {
					root: { borderColor: colors.divider },
				},
			},
			MuiChip: {
				styleOverrides: {
					root: { fontWeight: 600 },
				},
			},
		},
	});
}
