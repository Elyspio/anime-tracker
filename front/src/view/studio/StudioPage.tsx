import { useMemo, useState } from "react";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import RefreshIcon from "@mui/icons-material/Refresh";
import { Alert, Box, Button, CircularProgress, FormControlLabel, Link, Stack, Switch, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import { Link as RouterLink, useParams } from "react-router";
import { useSnackbar } from "notistack";
import { useViewMode } from "@/config/viewMode";
import { apiErrorMessage } from "@/core/api/errors";
import { useRefreshStudio } from "@/core/api/mutations";
import { useStudio } from "@/core/api/queries";
import { useAppAuth } from "@/core/auth/AuthProvider";
import { seasonLabels } from "@/core/binge";
import { formatLabels } from "@/core/ranking";
import { homePath, parseStudioId, seasonPath } from "@/core/routes";
import { countFormats, firstYear, formatFetchedAt, formatWorkCount, groupByYear, matchesWorkAdult, matchesWorkFormat } from "@/core/studio";
import { ChoiceChip } from "@/view/components/ChoiceChip";
import { Mono } from "@/view/components/Mono";
import { useDocumentTitle } from "@/view/components/useDocumentTitle";
import { useLayoutContext } from "@/view/layout/layoutContext";
import { WorkListHeader, YearSection } from "@/view/studio/StudioWorks";
import type { AnimeDate, AnimeFormat, StudioFilmography } from "@/core/api/types";

/** The way back, named after where it leads: the season the reader came from. */
function BackToSeason({ season }: { season: AnimeDate }) {
	return (
		<Link
			component={RouterLink}
			to={seasonPath(season)}
			underline="hover"
			color="text.secondary"
			sx={{ display: "inline-flex", alignItems: "center", gap: 0.25, alignSelf: "flex-start", ml: -0.75, fontSize: 13 }}
		>
			<ChevronLeftIcon sx={{ fontSize: 18 }} />
			{seasonLabels[season.season]} {season.year}
		</Link>
	);
}

function RefreshStudioButton({ studioId }: { studioId: number }) {
	const refresh = useRefreshStudio(studioId);
	const { enqueueSnackbar } = useSnackbar();

	return (
		<Button
			variant="outlined"
			size="small"
			startIcon={<RefreshIcon fontSize="small" />}
			loading={refresh.isPending}
			onClick={() =>
				refresh.mutate(undefined, {
					onSuccess: () => enqueueSnackbar("Studio refreshed", { variant: "info" }),
					// The API decides who may refresh; what it kept is still on screen.
					onError: (error) => enqueueSnackbar(apiErrorMessage(error, "Refresh failed"), { variant: "error" }),
				})
			}
		>
			Refresh studio
		</Button>
	);
}

function Filmography({ studio }: { studio: StudioFilmography }) {
	const auth = useAppAuth();
	const [viewMode, setViewMode] = useViewMode("studio");
	const [format, setFormat] = useState<AnimeFormat | null>(null);
	const [includeAdult, setIncludeAdult] = useState(false);

	const hasAdult = useMemo(() => studio.works.some((work) => work.isAdult), [studio.works]);
	const shown = useMemo(() => studio.works.filter((work) => matchesWorkAdult(work, includeAdult)), [studio.works, includeAdult]);
	const formats = useMemo(() => countFormats(shown), [shown]);

	// A format that the adult switch just emptied falls back to "All" rather than to an empty page.
	const selected = format !== null && formats.some((entry) => entry.format === format) ? format : null;
	const groups = useMemo(() => groupByYear(shown.filter((work) => matchesWorkFormat(work, selected))), [shown, selected]);

	const since = firstYear(shown);

	return (
		<>
			<Stack direction={{ xs: "column", md: "row" }} sx={{ alignItems: { md: "flex-end" }, justifyContent: "space-between", gap: 2 }}>
				<Stack sx={{ gap: 1, minWidth: 0 }}>
					<Typography component="h1" sx={{ fontSize: { xs: 30, md: 44 }, fontWeight: 600, letterSpacing: "-0.04em", lineHeight: 1.05, overflowWrap: "anywhere" }}>
						{studio.name || "Unnamed studio"}
					</Typography>
					<Typography variant="body2" sx={{ color: "text.secondary", maxWidth: "62ch" }}>
						{formatWorkCount(shown.length)}
						{since !== null ? ` since ${since}` : ""} as main studio. Read from{" "}
						{studio.url ? (
							<Link href={studio.url} target="_blank" rel="noopener noreferrer" color="inherit">
								AniList
							</Link>
						) : (
							"AniList"
						)}{" "}
						on {formatFetchedAt(studio.fetchedAt)}.
					</Typography>
				</Stack>

				<Stack direction="row" sx={{ alignItems: "center", gap: 1.5, flexShrink: 0 }}>
					{auth.isAuthenticated && <RefreshStudioButton studioId={studio.studioId} />}
					<ToggleButtonGroup exclusive value={viewMode} onChange={(_event, next) => next && setViewMode(next)} sx={{ ml: { xs: "auto", md: 0 } }}>
						<ToggleButton value="cards" aria-label="Poster view">
							Grid
						</ToggleButton>
						<ToggleButton value="table" aria-label="List view">
							List
						</ToggleButton>
					</ToggleButtonGroup>
				</Stack>
			</Stack>

			{(formats.length > 1 || hasAdult) && (
				<Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
					{formats.length > 1 && (
						<>
							<ChoiceChip label="All" selected={selected === null} onClick={() => setFormat(null)} />
							{formats.map((entry) => (
								<ChoiceChip
									key={entry.format}
									selected={selected === entry.format}
									onClick={() => setFormat(entry.format)}
									label={
										<>
											{formatLabels[entry.format]} <Mono sx={{ opacity: 0.6, ml: 0.25 }}>{entry.count}</Mono>
										</>
									}
								/>
							))}
						</>
					)}

					{hasAdult && (
						<FormControlLabel
							sx={{ ml: formats.length > 1 ? 1 : 0, mr: 0 }}
							control={<Switch checked={includeAdult} onChange={(event) => setIncludeAdult(event.target.checked)} sx={{ mr: 1 }} />}
							label={
								<Typography variant="body2" sx={{ color: "text.secondary" }}>
									Adult
								</Typography>
							}
						/>
					)}
				</Stack>
			)}

			{groups.length === 0 ? (
				<Alert severity="info">
					{studio.works.length === 0 ? "AniList lists no work with this studio as its main one." : "Nothing to show. The rest of this studio's work is marked adult."}
				</Alert>
			) : (
				<Box>
					{viewMode === "table" && <WorkListHeader />}
					{groups.map((group) => (
						<YearSection key={group.year ?? "undated"} group={group} mode={viewMode} />
					))}
				</Box>
			)}
		</>
	);
}

/** Everything a studio made as main studio, newest first, reached from any of its anime. */
export function StudioPage() {
	const { season } = useLayoutContext();
	const studioId = parseStudioId(useParams().studioId);
	const { data, isPending, error } = useStudio(studioId);

	useDocumentTitle(data?.name ?? null);

	return (
		<Stack sx={{ gap: 2.5 }}>
			<BackToSeason season={season} />

			{studioId === null ? (
				<Alert severity="info">
					This address names no studio.{" "}
					<Link component={RouterLink} to={homePath} color="inherit">
						Go to the current season
					</Link>
				</Alert>
			) : isPending ? (
				<Stack sx={{ alignItems: "center", gap: 1.5, py: 8, textAlign: "center" }}>
					<CircularProgress size={28} />
					<Typography variant="body2" sx={{ color: "text.secondary", maxWidth: "46ch" }}>
						Loading the filmography. The first visit to a studio reads it from AniList, which takes a few seconds.
					</Typography>
				</Stack>
			) : error ? (
				<Alert severity="error">Could not load this studio: {apiErrorMessage(error)}</Alert>
			) : (
				<Filmography studio={data} />
			)}
		</Stack>
	);
}
