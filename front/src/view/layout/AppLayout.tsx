import { useState } from "react";
import {
	AppBar,
	Badge,
	Box,
	Button,
	Container,
	IconButton,
	Stack,
	Toolbar,
	Tooltip,
	Typography,
} from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import HistoryIcon from "@mui/icons-material/History";
import { useSnackbar } from "notistack";
import { useAppAuth } from "@/core/auth/AuthProvider";
import { useRefreshSeason } from "@/core/api/mutations";
import { useRefreshRuns, useSeasonRefetchOnRunCompletion } from "@/core/api/queries";
import { findRunFor, isRunActive } from "@/core/refreshRuns";
import { currentSeason } from "@/core/binge";
import { AnimesPage } from "@/view/animes/AnimesPage";
import { RefreshRunsDrawer } from "@/view/layout/RefreshRunsDrawer";
import { SeasonSelector } from "@/view/layout/SeasonSelector";
import { ThemeToggle } from "@/view/layout/ThemeToggle";
import type { AnimeSeason } from "@/core/api/types";

export function AppLayout() {
	const auth = useAppAuth();
	const refresh = useRefreshSeason();
	const { enqueueSnackbar } = useSnackbar();
	const runs = useRefreshRuns();

	const [selected, setSelected] = useState(() => currentSeason(new Date()));
	const [runsOpen, setRunsOpen] = useState(false);

	// The 202 carries no new data — the grid fills in when the fetch itself lands, a moment later.
	useSeasonRefetchOnRunCompletion(runs.data);

	const activeRuns = (runs.data ?? []).filter(isRunActive);
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

				enqueueSnackbar("Refresh queued — the season will update in a moment", {
					variant: "info",
				});
			},
			// The API is the authority on who may refresh: render its refusal rather than
			// deciding from the token what to show.
			onError: (error) =>
				enqueueSnackbar(error instanceof Error ? error.message : "Refresh failed", {
					variant: "error",
				}),
		});

	const change = (year: number, season: AnimeSeason) => setSelected({ year, season });

	// No N-of-M any more: one fetch has no halfway point, so the button just says it is busy.
	const refreshLabel = currentRun ? "Refreshing…" : "Refresh";

	return (
		<Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
			<AppBar
				position="sticky"
				color="transparent"
				elevation={0}
				sx={{ backdropFilter: "blur(12px)", borderBottom: 1, borderColor: "divider" }}
			>
				<Toolbar sx={{ gap: 2, flexWrap: "wrap" }}>
					<Typography variant="h6" sx={{ mr: 2 }}>
						Anime Tracker
					</Typography>

					<SeasonSelector
						year={selected.year}
						season={selected.season}
						onChange={change}
					/>

					<Stack direction="row" spacing={1} sx={{ ml: "auto", alignItems: "center" }}>
						{auth.isAuthenticated && (
							<Button
								startIcon={<RefreshIcon />}
								onClick={runRefresh}
								loading={refresh.isPending}
								variant="outlined"
								size="small"
							>
								{refreshLabel}
							</Button>
						)}

						{/* Anonymous too: an empty grid is explained by whether a run has ever succeeded. */}
						<Tooltip title="Refreshes">
							<IconButton size="small" onClick={() => setRunsOpen(true)}>
								<Badge badgeContent={activeRuns.length} color="info">
									<HistoryIcon fontSize="small" />
								</Badge>
							</IconButton>
						</Tooltip>

						<ThemeToggle />
						{auth.isAuthenticated ? (
							<>
								{auth.name && (
									<Typography variant="body2" color="text.secondary">
										{auth.name}
									</Typography>
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

			<Container maxWidth="xl" sx={{ py: 4 }}>
				<AnimesPage year={selected.year} season={selected.season} />
			</Container>

			<RefreshRunsDrawer
				open={runsOpen}
				onClose={() => setRunsOpen(false)}
				runs={runs.data}
				isPending={runs.isPending}
				error={runs.error}
			/>
		</Box>
	);
}
