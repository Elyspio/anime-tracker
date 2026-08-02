import { useState } from "react";
import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import { useSnackbar } from "notistack";
import { useAppAuth } from "@/core/auth/AuthProvider";
import { useRefreshSeason } from "@/core/api/mutations";
import { currentSeason } from "@/core/binge";
import { AnimesPage } from "@/view/animes/AnimesPage";
import { SeasonSelector } from "@/view/layout/SeasonSelector";
import { ThemeToggle } from "@/view/layout/ThemeToggle";
import type { AnimeSeason } from "@/core/api/types";

export function AppLayout() {
	const auth = useAppAuth();
	const refresh = useRefreshSeason();
	const { enqueueSnackbar } = useSnackbar();

	const [selected, setSelected] = useState(() => currentSeason(new Date()));

	const runRefresh = () =>
		refresh.mutate(selected, {
			// 202: the scrape has been queued, not finished. Saying otherwise would have the user
			// reload an unchanged grid and conclude the refresh is broken.
			onSuccess: () =>
				enqueueSnackbar(
					"Rafraîchissement lancé — la saison se remplira au fil du scraping",
					{
						variant: "info",
					},
				),
			// The API is the authority on who may refresh: render its refusal rather than
			// deciding from the token what to show.
			onError: (error) =>
				enqueueSnackbar(
					error instanceof Error ? error.message : "Échec du rafraîchissement",
					{
						variant: "error",
					},
				),
		});

	const change = (year: number, season: AnimeSeason) => setSelected({ year, season });

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
								Rafraîchir
							</Button>
						)}
						<ThemeToggle />
						{auth.isAuthenticated ? (
							<>
								{auth.name && (
									<Typography variant="body2" color="text.secondary">
										{auth.name}
									</Typography>
								)}
								<Button size="small" onClick={auth.signOut}>
									Déconnexion
								</Button>
							</>
						) : (
							<Button size="small" onClick={auth.signIn}>
								Connexion
							</Button>
						)}
					</Stack>
				</Toolbar>
			</AppBar>

			<Container maxWidth="xl" sx={{ py: 4 }}>
				<AnimesPage year={selected.year} season={selected.season} />
			</Container>
		</Box>
	);
}
