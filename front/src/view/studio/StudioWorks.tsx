import { Box, Chip, Link, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router";
import { darkTokens, fonts, lightTokens, shadows, topbarHeight } from "@/config/tokens";
import type { ViewMode } from "@/config/viewMode";
import { seasonLabels } from "@/core/binge";
import { formatLabels } from "@/core/ranking";
import { seasonPath } from "@/core/routes";
import { formatWorkCount, formatWorkDate, type WorkGroup } from "@/core/studio";
import { Eyebrow } from "@/view/components/Eyebrow";
import { Mono } from "@/view/components/Mono";
import { Poster } from "@/view/components/Poster";
import type { StudioWork } from "@/core/api/types";

/** The rail the years run down, and the column everything else lines up on. */
const spine = { xs: "1fr", md: "148px minmax(0, 1fr)" } as const;

/** Poster, title, season, date, format, episodes, rating. Shared by the header and every row. */
const rowColumns = { xs: "36px minmax(0, 1fr) auto", md: "36px minmax(0, 1fr) 96px 148px 84px 64px 44px" } as const;

const onlyWide = { display: { xs: "none", md: "block" } } as const;

/** The work's season, as the way into the app's own view of it: that is where the countdown lives. */
function SeasonLink({ work, withYear }: { work: StudioWork; withYear: boolean }) {
	if (!work.season) return null;

	return (
		<Link component={RouterLink} to={seasonPath(work.season)} underline="hover" color="inherit" title="Open this season">
			{seasonLabels[work.season.season]}
			{withYear ? ` ${work.season.year}` : ""}
		</Link>
	);
}

function episodes(work: StudioWork): string | null {
	if (work.episodesCount === null) return null;

	return `${work.episodesCount} ep`;
}

function WorkTile({ work }: { work: StudioWork }) {
	const date = formatWorkDate(work);

	return (
		<Stack sx={{ gap: 1, minWidth: 0 }}>
			<Link
				href={work.url || undefined}
				target="_blank"
				rel="noopener noreferrer"
				underline="none"
				color="inherit"
				sx={{
					display: "flex",
					flexDirection: "column",
					gap: 1,
					borderRadius: 1.25,
					"&:hover .cover": {
						boxShadow: (theme) => (theme.palette.mode === "dark" ? shadows.dark.md : shadows.light.md),
					},
					"&:hover .title": { textDecoration: "underline" },
				}}
			>
				<Box className="cover" sx={{ position: "relative", borderRadius: 1.25, overflow: "hidden", transition: (theme) => theme.transitions.create("box-shadow") }}>
					<Poster src={work.imageUrl} />
					{work.score !== null && (
						<Chip label={work.score.toFixed(1)} size="small" sx={{ position: "absolute", top: 8, right: 8, bgcolor: "background.paper", color: "text.primary" }} />
					)}
				</Box>
				<Typography
					className="title"
					variant="subtitle2"
					title={work.title}
					// Two lines whether or not the title needs them, so a row of tiles keeps one baseline.
					sx={{ display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden", lineHeight: 1.35, minHeight: "2.7em" }}
				>
					{work.title || "Untitled"}
				</Typography>
			</Link>

			{/* Spacing rather than separators: three short facts, each in its own place. */}
			<Stack direction="row" title={date ?? undefined} sx={{ columnGap: 1.25, rowGap: 0.25, flexWrap: "wrap", color: "text.disabled" }}>
				{work.season && (
					<Mono sx={{ color: "text.secondary" }}>
						<SeasonLink work={work} withYear={false} />
					</Mono>
				)}
				<Mono>{formatLabels[work.format]}</Mono>
				{episodes(work) && <Mono>{episodes(work)}</Mono>}
			</Stack>
		</Stack>
	);
}

function WorkRow({ work }: { work: StudioWork }) {
	return (
		<Box
			sx={{
				display: "grid",
				gridTemplateColumns: rowColumns,
				alignItems: "center",
				columnGap: 2,
				py: 0.75,
				borderBottom: 1,
				borderColor: (theme) => (theme.palette.mode === "dark" ? darkTokens.lineSoft : lightTokens.lineSoft),
				"&:last-of-type": { borderBottom: 0 },
			}}
		>
			<Poster src={work.imageUrl} sx={{ width: 36, borderRadius: 0.5 }} />

			<Stack sx={{ minWidth: 0, gap: 0.25 }}>
				<Link
					href={work.url || undefined}
					target="_blank"
					rel="noopener noreferrer"
					underline="hover"
					color="inherit"
					title={work.title}
					noWrap
					sx={{ fontWeight: 500, fontSize: 14 }}
				>
					{work.title || "Untitled"}
				</Link>
				{/* Narrow screens fold the columns that follow into one line under the title. */}
				<Stack direction="row" sx={{ display: { xs: "flex", md: "none" }, columnGap: 1.25, color: "text.disabled" }}>
					{work.season && (
						<Mono sx={{ color: "text.secondary" }}>
							<SeasonLink work={work} withYear={false} />
						</Mono>
					)}
					<Mono>{formatLabels[work.format]}</Mono>
					{episodes(work) && <Mono>{episodes(work)}</Mono>}
				</Stack>
			</Stack>

			<Box sx={onlyWide}>
				<Mono sx={{ color: "text.secondary" }}>{work.season ? <SeasonLink work={work} withYear={false} /> : "—"}</Mono>
			</Box>
			<Box sx={onlyWide}>
				<Mono sx={{ color: "text.disabled" }}>{formatWorkDate(work) ?? "—"}</Mono>
			</Box>
			<Box sx={onlyWide}>
				<Mono sx={{ color: "text.disabled" }}>{formatLabels[work.format]}</Mono>
			</Box>
			<Box sx={{ ...onlyWide, textAlign: "right" }}>
				<Mono sx={{ color: "text.disabled" }}>{work.episodesCount ?? "—"}</Mono>
			</Box>
			<Box sx={{ textAlign: "right" }}>
				<Mono variant="body2">{work.score?.toFixed(1) ?? "—"}</Mono>
			</Box>
		</Box>
	);
}

/** Column names for the list, set on the same grid as the rows and standing clear of the year rail. */
export function WorkListHeader() {
	return (
		<Box sx={{ display: { xs: "none", md: "grid" }, gridTemplateColumns: spine, columnGap: 3, pb: 1 }}>
			<span />
			<Box sx={{ display: "grid", gridTemplateColumns: rowColumns, columnGap: 2 }}>
				<span />
				<Eyebrow>Title</Eyebrow>
				<Eyebrow>Season</Eyebrow>
				<Eyebrow>Started</Eyebrow>
				<Eyebrow>Format</Eyebrow>
				<Eyebrow sx={{ textAlign: "right" }}>Episodes</Eyebrow>
				<Eyebrow sx={{ textAlign: "right" }}>Rating</Eyebrow>
			</Box>
		</Box>
	);
}

interface Props {
	group: WorkGroup;
	mode: ViewMode;
}

/**
 * One year of the filmography. The year is the page's structure, not a label on it: it sits in a
 * rail of its own and stays in view while its works scroll past, so the reader always knows how far
 * back they have gone.
 */
export function YearSection({ group, mode }: Props) {
	const heading = group.year === null ? "Not yet dated" : String(group.year);

	return (
		<Box
			component="section"
			aria-label={heading}
			sx={{ display: "grid", gridTemplateColumns: spine, columnGap: 3, rowGap: 1.5, borderTop: 1, borderColor: "divider", pt: 2, pb: mode === "cards" ? 4.5 : 3 }}
		>
			<Stack
				direction={{ xs: "row", md: "column" }}
				sx={{
					position: { md: "sticky" },
					// Clear of the top bar when it is showing; it slides away on the way down.
					top: { md: topbarHeight + 16 },
					alignSelf: "start",
					alignItems: { xs: "baseline", md: "flex-start" },
					columnGap: 1.5,
					rowGap: 0.25,
				}}
			>
				<Typography
					component="h2"
					sx={
						group.year === null
							? { fontSize: 17, fontWeight: 600, letterSpacing: "-0.015em", lineHeight: 1.3 }
							: { fontFamily: fonts.mono, fontVariantNumeric: "tabular-nums", fontSize: { xs: 24, md: 34 }, fontWeight: 500, letterSpacing: "-0.04em", lineHeight: 1 }
					}
				>
					{heading}
				</Typography>
				<Mono sx={{ color: "text.disabled" }}>{formatWorkCount(group.works.length)}</Mono>
			</Stack>

			{mode === "cards" ? (
				<Box
					sx={{
						display: "grid",
						gridTemplateColumns: { xs: "repeat(auto-fill, minmax(112px, 1fr))", sm: "repeat(auto-fill, minmax(148px, 1fr))" },
						columnGap: 2.5,
						rowGap: 3,
					}}
				>
					{group.works.map((work) => (
						<WorkTile key={work.sourceId} work={work} />
					))}
				</Box>
			) : (
				<Box>
					{group.works.map((work) => (
						<WorkRow key={work.sourceId} work={work} />
					))}
				</Box>
			)}
		</Box>
	);
}
