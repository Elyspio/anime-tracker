import { createTheme, type Theme } from "@mui/material";
import {
	controlTransition,
	darkTokens,
	fonts,
	lightTokens,
	radii,
	shadows,
	topbarHeight,
	type Tokens,
} from "@/config/tokens";

export type PaletteMode = "light" | "dark";

/**
 * The product wears the Sous-marin Jaune design language: warm-paper neutrals, ink text, one
 * emerald accent, 1px hairlines and 60-120ms transitions. Ink — not a hue — is the interactive
 * colour, which is what leaves the emerald free: success green is reserved for "bingeable", the one
 * state the whole product exists to signal, so nothing else may use it. Not a progress bar, not a
 * high rating, not a finished refresh.
 */
export function createAppTheme(mode: PaletteMode): Theme {
	const t: Tokens = mode === "dark" ? darkTokens : lightTokens;
	const shadow = mode === "dark" ? shadows.dark : shadows.light;

	/** Uppercase mono, 0.08em — the only place uppercase is allowed in this system. */
	const eyebrow = {
		fontFamily: fonts.mono,
		fontSize: 11,
		fontWeight: 500,
		letterSpacing: "0.08em",
		textTransform: "uppercase",
		lineHeight: 1.3,
	} as const;

	return createTheme({
		palette: {
			mode,
			// Ink carries every interactive surface: solid buttons, selected pills, progress fill.
			primary: { main: t.ink, contrastText: t.paper },
			secondary: { main: t.clay, contrastText: t.paper },
			success: { main: t.accent, dark: t.accent2, contrastText: "#FFFFFF" },
			warning: { main: t.warn },
			error: { main: t.danger },
			info: { main: t.clay },
			background: { default: t.paper2, paper: t.paper },
			text: { primary: t.ink, secondary: t.ink2, disabled: t.ink4 },
			divider: t.line,
		},
		typography: {
			fontFamily: fonts.sans,
			fontSize: 14.5,
			h1: { fontSize: 28, fontWeight: 600, letterSpacing: "-0.03em", lineHeight: 1.15 },
			h2: { fontSize: 22, fontWeight: 600, letterSpacing: "-0.02em", lineHeight: 1.2 },
			h3: { fontSize: 19, fontWeight: 600, letterSpacing: "-0.02em", lineHeight: 1.25 },
			h4: { fontSize: 19, fontWeight: 600, letterSpacing: "-0.02em", lineHeight: 1.25 },
			h5: { fontSize: 17, fontWeight: 600, letterSpacing: "-0.015em", lineHeight: 1.3 },
			h6: { fontSize: 15, fontWeight: 600, letterSpacing: "-0.015em", lineHeight: 1.3 },
			subtitle1: {
				fontSize: 15,
				fontWeight: 600,
				letterSpacing: "-0.015em",
				lineHeight: 1.3,
			},
			subtitle2: { fontSize: 13, fontWeight: 600, letterSpacing: "-0.005em" },
			body1: { fontSize: 14.5, letterSpacing: "-0.005em", lineHeight: 1.5 },
			body2: { fontSize: 13, letterSpacing: "-0.005em", lineHeight: 1.5 },
			caption: { fontSize: 12, letterSpacing: "-0.005em", lineHeight: 1.4 },
			button: { fontSize: 13, fontWeight: 500, textTransform: "none", letterSpacing: 0 },
			overline: eyebrow,
		},
		shape: { borderRadius: radii.control },
		components: {
			MuiCssBaseline: {
				styleOverrides: {
					body: {
						fontFeatureSettings: fonts.features,
						WebkitFontSmoothing: "antialiased",
						MozOsxFontSmoothing: "grayscale",
					},
					"code, kbd, samp, pre": { fontFamily: fonts.mono },
					"::selection": { background: t.accentSoft },
					// Focus rings are ink, not emerald: green means bingeable and nothing else.
					":focus-visible": { outline: `2px solid ${t.ink3}`, outlineOffset: 2 },
					"*": {
						scrollbarWidth: "thin",
						scrollbarColor: `color-mix(in srgb, ${t.ink} 22%, transparent) transparent`,
					},
					"*::-webkit-scrollbar": { width: 10, height: 10 },
					"*::-webkit-scrollbar-track": { background: "transparent" },
					"*::-webkit-scrollbar-thumb": {
						backgroundColor: `color-mix(in srgb, ${t.ink} 22%, transparent)`,
						borderRadius: radii.control,
						border: "2px solid transparent",
						backgroundClip: "padding-box",
					},
					// The system's one animation, reserved for live-data signals.
					"@keyframes pulse": {
						"0%, 100%": { opacity: 1 },
						"50%": { opacity: 0.4 },
					},
				},
			},
			MuiAppBar: {
				defaultProps: { elevation: 0, color: "inherit" },
				styleOverrides: {
					root: {
						backgroundColor: t.paper,
						backgroundImage: "none",
						borderBottom: `1px solid ${t.line}`,
					},
				},
			},
			MuiToolbar: {
				styleOverrides: {
					root: {
						minHeight: topbarHeight,
						"@media (min-width: 600px)": { minHeight: topbarHeight },
					},
				},
			},
			MuiPaper: {
				defaultProps: { elevation: 0 },
				styleOverrides: {
					root: { backgroundImage: "none" },
					outlined: { borderColor: t.line },
				},
			},
			MuiButton: {
				defaultProps: { disableElevation: true, disableRipple: true },
				styleOverrides: {
					root: {
						borderRadius: radii.control,
						border: "1px solid transparent",
						transition: controlTransition,
						"&:active": { transform: "translateY(1px)" },
					},
					sizeSmall: { minHeight: 28, padding: "0 10px", fontSize: 12 },
					sizeMedium: { minHeight: 34, padding: "0 14px" },
					sizeLarge: { minHeight: 40, padding: "0 18px", fontSize: 14 },
					// Soft: paper surface, hairline border — the toolbar's default weight.
					outlined: {
						backgroundColor: t.paper,
						borderColor: t.line,
						color: t.ink,
						"&:hover": { backgroundColor: t.paper2, borderColor: t.line },
					},
					text: {
						color: t.ink2,
						"&:hover": { backgroundColor: t.paper2, color: t.ink },
					},
				},
			},
			MuiIconButton: {
				defaultProps: { disableRipple: true },
				styleOverrides: {
					root: {
						borderRadius: radii.small,
						color: t.ink2,
						transition: controlTransition,
						"&:hover": { backgroundColor: t.paper2, color: t.ink },
					},
					sizeSmall: { width: 28, height: 28 },
				},
			},
			MuiChip: {
				styleOverrides: {
					root: {
						fontFamily: fonts.mono,
						fontSize: 11,
						fontWeight: 500,
						letterSpacing: "0.02em",
						height: 22,
						borderRadius: radii.pill,
						border: `1px solid ${t.line}`,
						backgroundColor: t.paper2,
						color: t.ink3,
					},
					label: { paddingLeft: 8, paddingRight: 8 },
					sizeSmall: { height: 22 },
				},
			},
			MuiOutlinedInput: {
				styleOverrides: {
					root: {
						borderRadius: radii.control,
						backgroundColor: t.paper,
						fontSize: 13,
						"& .MuiOutlinedInput-notchedOutline": { borderColor: t.line },
						"&:hover .MuiOutlinedInput-notchedOutline": { borderColor: t.ink4 },
						"&.Mui-focused .MuiOutlinedInput-notchedOutline": {
							borderWidth: 1,
							borderColor: t.ink3,
						},
					},
					input: { fontSize: 13 },
				},
			},
			MuiInputLabel: { styleOverrides: { root: { fontSize: 13 } } },
			MuiMenuItem: { styleOverrides: { root: { fontSize: 13 } } },
			MuiSelect: { styleOverrides: { select: { fontSize: 13 } } },
			MuiAutocomplete: {
				styleOverrides: {
					tag: { height: 20, fontSize: 11 },
					option: { fontSize: 13 },
				},
			},
			MuiToggleButtonGroup: {
				styleOverrides: {
					root: {
						gap: 2,
						padding: 3,
						borderRadius: radii.control,
						backgroundColor: t.paper2,
						border: `1px solid ${t.line}`,
					},
					grouped: {
						border: 0,
						borderRadius: `${radii.small}px !important`,
					},
				},
			},
			MuiToggleButton: {
				defaultProps: { disableRipple: true },
				styleOverrides: {
					root: {
						border: 0,
						padding: "5px 12px",
						color: t.ink2,
						textTransform: "none",
						fontSize: 13,
						fontWeight: 500,
						transition: controlTransition,
						"&:hover": { backgroundColor: "transparent", color: t.ink },
						"&.Mui-selected": {
							backgroundColor: t.paper,
							color: t.ink,
							boxShadow: shadow.sm,
							"&:hover": { backgroundColor: t.paper },
						},
					},
				},
			},
			MuiSwitch: {
				styleOverrides: {
					root: { width: 38, height: 22, padding: 0 },
					switchBase: {
						padding: 3,
						"&.Mui-checked": {
							transform: "translateX(16px)",
							color: "#FFFFFF",
							"& + .MuiSwitch-track": { backgroundColor: t.ink, opacity: 1 },
						},
					},
					thumb: { width: 16, height: 16, boxShadow: "none", color: t.paper },
					track: { borderRadius: radii.pill, backgroundColor: t.ink4, opacity: 1 },
				},
			},
			MuiLinearProgress: {
				styleOverrides: {
					root: { height: 2, borderRadius: 0, backgroundColor: t.line },
					bar: { borderRadius: 0 },
				},
			},
			MuiTableCell: {
				styleOverrides: {
					root: { borderColor: t.lineSoft },
					head: {
						...eyebrow,
						color: t.ink3,
						backgroundColor: t.paper2,
						borderColor: t.line,
					},
				},
			},
			MuiTableRow: {
				styleOverrides: {
					root: { "&:hover": { backgroundColor: t.paper } },
				},
			},
			MuiTooltip: {
				styleOverrides: {
					tooltip: {
						backgroundColor: t.paper,
						color: t.ink,
						border: `1px solid ${t.line}`,
						borderRadius: radii.control,
						boxShadow: shadow.md,
						fontSize: 12,
						fontWeight: 400,
						padding: "6px 10px",
					},
				},
			},
			MuiDrawer: {
				styleOverrides: {
					paper: { backgroundColor: t.paper2, borderColor: t.line },
				},
			},
			MuiMenu: {
				styleOverrides: {
					paper: {
						borderRadius: radii.control,
						border: `1px solid ${t.line}`,
						boxShadow: shadow.md,
					},
				},
			},
			MuiAlert: {
				variants: [
					{
						props: { severity: "info" },
						style: { backgroundColor: t.paper, color: t.ink2 },
					},
					{
						props: { severity: "error" },
						style: { backgroundColor: t.dangerSoft, color: t.danger },
					},
				],
				styleOverrides: {
					root: {
						borderRadius: radii.control,
						border: `1px solid ${t.line}`,
						fontSize: 13,
					},
					icon: { opacity: 0.7 },
				},
			},
		},
	});
}
