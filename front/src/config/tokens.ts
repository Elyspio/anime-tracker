/**
 * Design tokens, ported from the Sous-marin Jaune design system (tokens/*.css) so the two products
 * read as one hand. Values are duplicated here rather than imported as CSS variables on purpose:
 * MUI computes contrast and alpha from real colours, and `var(--x)` would leave it guessing.
 *
 * Dark is a peer palette, not a re-tint — every entry is authored, none derived.
 */

export interface Tokens {
	/** Card / raised surface. */
	paper: string;
	/** App background, and the sunken track of grouped controls. */
	paper2: string;
	/** Wells: poster placeholders, empty thumbnails. */
	paper3: string;
	/** Text and strokes, darkest first. */
	ink: string;
	ink2: string;
	ink3: string;
	ink4: string;
	/** The strong hairline. */
	line: string;
	/** In-card and in-table separators. */
	lineSoft: string;
	/**
	 * Emerald. Bound to MUI's `success`, because in this product green means exactly one thing:
	 * bingeable. Nothing else may reach for it.
	 */
	accent: string;
	accent2: string;
	accentSoft: string;
	/** Warm secondary. Carries "a run is happening", never a countdown. */
	clay: string;
	claySoft: string;
	warn: string;
	warnSoft: string;
	danger: string;
	dangerSoft: string;
	/** Deterministic avatar backgrounds, picked by name hash. */
	avatars: readonly string[];
}

export const lightTokens: Tokens = {
	paper: "#FFFFFF",
	paper2: "#F4F4F5",
	paper3: "#E9E9EC",
	ink: "#09090B",
	ink2: "#3F3F46",
	ink3: "#71717A",
	ink4: "#A1A1AA",
	line: "#E4E4E7",
	lineSoft: "#EFEFF2",
	accent: "#0A7A55",
	accent2: "#086544",
	accentSoft: "#E6F4EE",
	clay: "#C2410C",
	claySoft: "#FEEAD9",
	warn: "#B45309",
	warnSoft: "#FEF1C8",
	danger: "#DC2626",
	dangerSoft: "#FEE2E2",
	avatars: [
		"#2E5D4A",
		"#B4543A",
		"#54678C",
		"#8C5454",
		"#486A4A",
		"#A0712E",
		"#5B5B6E",
		"#38493E",
	],
};

export const darkTokens: Tokens = {
	paper: "#18181B",
	paper2: "#0A0A0B",
	paper3: "#27272A",
	ink: "#FAFAFA",
	ink2: "#D4D4D8",
	ink3: "#A1A1AA",
	ink4: "#71717A",
	line: "#27272A",
	lineSoft: "#1F1F23",
	accent: "#10B981",
	accent2: "#059669",
	accentSoft: "rgba(16, 185, 129, 0.15)",
	clay: "#EA7C4B",
	claySoft: "rgba(234, 124, 75, 0.16)",
	warn: "#FBBF24",
	warnSoft: "rgba(251, 191, 36, 0.16)",
	danger: "#F87171",
	dangerSoft: "rgba(248, 113, 113, 0.16)",
	avatars: lightTokens.avatars,
};

/** 6 / 8 / 12 / 16 / pill — controls, surfaces, cards, dialogs, chips. */
export const radii = {
	small: 6,
	control: 8,
	card: 12,
	dialog: 16,
	pill: 999,
} as const;

export const shadows = {
	light: {
		sm: "0 1px 2px rgba(9, 9, 11, 0.04), 0 1px 3px rgba(9, 9, 11, 0.04)",
		md: "0 4px 6px -2px rgba(9, 9, 11, 0.05), 0 12px 24px -8px rgba(9, 9, 11, 0.08)",
		lg: "0 8px 16px -4px rgba(9, 9, 11, 0.08), 0 24px 48px -12px rgba(9, 9, 11, 0.12)",
	},
	dark: {
		sm: "0 1px 2px rgba(0, 0, 0, 0.3), 0 1px 3px rgba(0, 0, 0, 0.3)",
		md: "0 4px 6px -2px rgba(0, 0, 0, 0.3), 0 12px 24px -8px rgba(0, 0, 0, 0.45)",
		lg: "0 8px 16px -4px rgba(0, 0, 0, 0.4), 0 24px 48px -12px rgba(0, 0, 0, 0.6)",
	},
} as const;

export const fonts = {
	sans: '"Geist Variable", "Geist", system-ui, sans-serif',
	mono: '"Geist Mono Variable", "Geist Mono", ui-monospace, SFMono-Regular, Menlo, monospace',
	/** The brand's quiet "engineered" character: alternate glyphs plus a hair of negative tracking. */
	features: '"ss01", "cv11"',
} as const;

/**
 * One compound transition for every control. Colour moves at 120ms, the press nudge at 60ms —
 * borders never animate their width, only their colour.
 */
export const controlTransition =
	"background 120ms ease, border-color 120ms ease, color 120ms ease, transform 60ms ease";

/** The topbar is the one fixed element, and everything else scrolls under it. */
export const topbarHeight = 64;
