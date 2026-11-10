import { useCallback, useEffect, useMemo, useState } from "react";
import { AppBar, Box, Button, Container, Divider, Link, Stack, Toolbar, Typography } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import { Link as RouterLink, Outlet, ScrollRestoration, useNavigate, useParams } from "react-router";
import { useSnackbar } from "notistack";
import { useAppAuth } from "@/core/auth/AuthProvider";
import { apiErrorMessage } from "@/core/api/errors";
import { useRefreshSeason } from "@/core/api/mutations";
import { useRefreshRuns, useSeasonRefetchOnRunCompletion } from "@/core/api/queries";
import { findRunFor } from "@/core/refreshRuns";
import { currentSeason } from "@/core/binge";
import { defaultSeasonFilters, type SeasonFilters } from "@/core/filters";
import { homePath, parseSeasonPath, seasonPath } from "@/core/routes";
import { RefreshRunsDrawer } from "@/view/layout/RefreshRunsDrawer";
import { DubCasesDrawer } from "@/view/layout/DubCasesDrawer";
import { SeasonSelector } from "@/view/layout/SeasonSelector";
import { SyncIndicator } from "@/view/layout/SyncIndicator";
import { ThemeToggle } from "@/view/layout/ThemeToggle";
import { UserAvatar } from "@/view/layout/UserAvatar";
import type { LayoutContext } from "@/view/layout/layoutContext";
import type { AnimeDate, AnimeSeason } from "@/core/api/types";

function sameSeason(left: AnimeDate, right: AnimeDate): boolean {
	return left.year === right.year && left.season === right.season;
}

export function AppLayout() {
	const auth = useAppAuth();
	const refresh = useRefreshSeason();
	const { enqueueSnackbar } = useSnackbar();
	const runs = useRefreshRuns();
	const navigate = useNavigate();
	const params = useParams();

	const [today] = useState(() => currentSeason(new Date()));
	const onStudio = params.studioId !== undefined || params.sourceId !== undefined;

	// The address names the season. A studio's address names none, so the selector keeps showing the
	// one the reader came from — which is also where it takes them back to.
	const routed = onStudio ? null : (parseSeasonPath(params.year, params.season) ?? today);
	const [lastSeason, setLastSeason] = useState(routed ?? today);
	if (routed && !sameSeason(routed, lastSeason)) setLastSeason(routed);
	const selected = routed ?? lastSeason;

	const [filters, setFiltersState] = useState<SeasonFilters>(defaultSeasonFilters);
	const setFilters = useCallback((patch: Partial<SeasonFilters>) => setFiltersState((previous) => ({ ...previous, ...patch })), []);
	const resetFilters = useCallback(() => setFiltersState(defaultSeasonFilters), []);

	const [runsOpen, setRunsOpen] = useState(false);
	const [dubCasesOpen, setDubCasesOpen] = useState(false);
	const [headerHidden, setHeaderHidden] = useState(false);

	useEffect(() => {
		let previousY = window.scrollY;
		const onScroll = () => {
			const currentY = Math.max(0, window.scrollY);
			if (currentY <= 0) setHeaderHidden(false);
			else if (currentY > previousY) setHeaderHidden(true);
			else if (currentY < previousY) setHeaderHidden(false);
			previousY = currentY;
		};
		window.addEventListener("scroll", onScroll, { passive: true });
		return () => window.removeEventListener("scroll", onScroll);
	}, []);

	// The 202 carries no new data — the grid fills in when the fetch itself lands, a moment later.
	useSeasonRefetchOnRunCompletion(runs.data);

	const currentRun = findRunFor(runs.data, selected.year, selected.season);

	const runRefresh = () =>
		refresh.mutate(selected, {
			onSuccess: (outcome) => {
				// A 409 is an answer, not a failure: the season is already being refreshed, and the
				// drawer opens on the run doing it.
				if (outcome.alreadyRunning) {
					enqueueSnackbar("This season is already being refreshed", {
						variant: "warning",
					});
					setRunsOpen(true);
					return;
				}

				enqueueSnackbar("Refresh queued. The season will update in a moment", {
					variant: "info",
				});
			},
			// The API is the authority on who may refresh: render its refusal rather than
			// deciding from the token what to show.
			onError: (error) => enqueueSnackbar(apiErrorMessage(error, "Refresh failed"), { variant: "error" }),
		});

	const change = (year: number, season: AnimeSeason) => void navigate(seasonPath({ year, season }));

	// No N-of-M any more: one fetch has no halfway point, so the button just says it is busy.
	const refreshLabel = currentRun ? "Refreshing…" : "Refresh";

	const context = useMemo<LayoutContext>(() => ({ season: selected, filters, setFilters, resetFilters }), [selected, filters, setFilters, resetFilters]);

	return (
		<Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
			<AppBar
				position="sticky"
				inert={headerHidden}
				sx={{
					transform: headerHidden ? "translateY(-100%)" : "translateY(0)",
					transition: "transform 180ms ease",
					"@media (prefers-reduced-motion: reduce)": { transition: "none" },
				}}
			>
				<Toolbar sx={{ gap: 2, flexWrap: "wrap" }}>
					<Link component={RouterLink} to={homePath} underline="none" color="inherit" aria-label="Anime Tracker, current season" sx={{ borderRadius: 0.5 }}>
						<Typography variant="h5" component="span" sx={{ whiteSpace: "nowrap" }}>
							Anime Tracker
						</Typography>
					</Link>

					<SeasonSelector year={selected.year} season={selected.season} onChange={change} />

					<Stack direction="row" sx={{ ml: "auto", width: { xs: "100%", md: "auto" }, minWidth: 0, flexWrap: "wrap", gap: 1.5, alignItems: "center" }}>
						{/* Anonymous too: an empty grid is explained by whether a run has ever succeeded. */}
						<SyncIndicator runs={runs.data} onOpen={() => setRunsOpen(true)} />

						{/* Both act on a season, and a studio's page is not one: it carries its own refresh. */}
						{auth.isAuthenticated && !onStudio && (
							<Button startIcon={<RefreshIcon fontSize="small" />} onClick={runRefresh} loading={refresh.isPending} variant="outlined" size="small">
								{refreshLabel}
							</Button>
						)}

						{auth.isAuthenticated && !onStudio && (
							<Button onClick={() => setDubCasesOpen(true)} variant="outlined" size="small">
								Dub matches
							</Button>
						)}

						<ThemeToggle />

						{auth.isAuthenticated ? (
							<>
								<Divider orientation="vertical" flexItem sx={{ my: 1.25 }} />
								{auth.name && (
									<Stack direction="row" spacing={1} sx={{ alignItems: "center", minWidth: 0, maxWidth: "100%" }}>
										<UserAvatar name={auth.name} />
										<Typography variant="body2" noWrap sx={{ color: "text.secondary" }}>
											{auth.name}
										</Typography>
									</Stack>
								)}
								<Button size="small" onClick={auth.signOut}>
									Sign out
								</Button>
							</>
						) : (
							<Button size="small" onClick={auth.signIn}>
								Sign in
							</Button>
						)}
					</Stack>
				</Toolbar>
			</AppBar>

			<Container maxWidth="xl" component="main" sx={{ py: 3 }}>
				<Outlet context={context} />
			</Container>

			<DubCasesDrawer open={dubCasesOpen} onClose={() => setDubCasesOpen(false)} year={selected.year} season={selected.season} />

			<RefreshRunsDrawer open={runsOpen} onClose={() => setRunsOpen(false)} runs={runs.data} isPending={runs.isPending} error={runs.error} />

			{/* Back from a studio lands where the reader left the season, not at its top. */}
			<ScrollRestoration />
		</Box>
	);
}
