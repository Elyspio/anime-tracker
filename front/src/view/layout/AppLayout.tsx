import { useState } from "react";
import { AppBar, Box, Button, Container, Divider, Stack, Toolbar, Typography } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import { useSnackbar } from "notistack";
import { useAppAuth } from "@/core/auth/AuthProvider";
import { useRefreshSeason } from "@/core/api/mutations";
import { useRefreshRuns, useSeasonRefetchOnRunCompletion } from "@/core/api/queries";
import { findRunFor } from "@/core/refreshRuns";
import { currentSeason } from "@/core/binge";
import { AnimesPage } from "@/view/animes/AnimesPage";
import { RefreshRunsDrawer } from "@/view/layout/RefreshRunsDrawer";
import { SeasonSelector } from "@/view/layout/SeasonSelector";
import { SyncIndicator } from "@/view/layout/SyncIndicator";
import { ThemeToggle } from "@/view/layout/ThemeToggle";
import { UserAvatar } from "@/view/layout/UserAvatar";
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
			<AppBar position="sticky">
				<Toolbar sx={{ gap: 2, flexWrap: "wrap" }}>
					<Typography variant="h5" sx={{ whiteSpace: "nowrap" }}>
						Anime Tracker
					</Typography>

					<SeasonSelector
						year={selected.year}
						season={selected.season}
						onChange={change}
					/>

					<Stack direction="row" spacing={1.5} sx={{ ml: "auto", alignItems: "center" }}>
						{/* Anonymous too: an empty grid is explained by whether a run has ever succeeded. */}
						<SyncIndicator runs={runs.data} onOpen={() => setRunsOpen(true)} />

						{auth.isAuthenticated && (
							<Button
								startIcon={<RefreshIcon fontSize="small" />}
								onClick={runRefresh}
								loading={refresh.isPending}
								variant="outlined"
								size="small"
							>
								{refreshLabel}
							</Button>
						)}

						<ThemeToggle />

						{auth.isAuthenticated ? (
							<>
								<Divider orientation="vertical" flexItem sx={{ my: 1.25 }} />
								{auth.name && (
									<Stack
										direction="row"
										spacing={1}
										sx={{ alignItems: "center", minWidth: 0 }}
									>
										<UserAvatar name={auth.name} />
										<Typography variant="body2" color="text.secondary" noWrap>
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

			<Container maxWidth="xl" sx={{ py: 3 }}>
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
