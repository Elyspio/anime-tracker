import { useMemo, useState } from "react";
import {
	Alert,
	Autocomplete,
	Box,
	Chip,
	CircularProgress,
	Stack,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import GridViewIcon from "@mui/icons-material/GridView";
import TableRowsIcon from "@mui/icons-material/TableRows";
import { useAnimes } from "@/core/api/queries";
import { collectTags, matchesStatus, matchesTags, type StatusFilter } from "@/core/binge";
import { useViewMode } from "@/config/viewMode";
import { AnimeCardGrid } from "@/view/animes/AnimeCardGrid";
import { AnimeTable } from "@/view/animes/AnimeTable";
import type { AnimeSeason } from "@/core/api/types";

const statusFilters: { value: StatusFilter; label: string }[] = [
	{ value: "all", label: "Tous" },
	{ value: "bingeable", label: "Bingeables" },
	{ value: "soon", label: "Bientôt" },
	{ value: "unknown", label: "Fin inconnue" },
];

interface Props {
	year: number;
	season: AnimeSeason;
}

export function AnimesPage({ year, season }: Props) {
	const { data, isPending, error } = useAnimes(year, season);
	const [viewMode, setViewMode] = useViewMode();
	const [status, setStatus] = useState<StatusFilter>("all");
	const [tags, setTags] = useState<string[]>([]);

	// Pinned per render pass so every countdown in the list is measured from the same instant.
	const now = useMemo(() => new Date(), [data]);

	const animes = data ?? [];
	const availableTags = useMemo(() => collectTags(animes), [animes]);
	const visible = useMemo(
		() =>
			animes.filter((anime) => matchesStatus(anime, status, now) && matchesTags(anime, tags)),
		[animes, status, tags, now],
	);

	if (isPending) {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
				<CircularProgress />
			</Box>
		);
	}

	if (error) {
		return <Alert severity="error">Impossible de charger la saison : {error.message}</Alert>;
	}

	return (
		<Stack spacing={3}>
			<Stack
				direction={{ xs: "column", md: "row" }}
				spacing={2}
				sx={{ alignItems: { md: "center" }, justifyContent: "space-between" }}
			>
				<Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
					{statusFilters.map((filter) => (
						<Chip
							key={filter.value}
							label={filter.label}
							onClick={() => setStatus(filter.value)}
							color={status === filter.value ? "primary" : "default"}
							variant={status === filter.value ? "filled" : "outlined"}
						/>
					))}
				</Stack>

				<Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
					<Autocomplete
						multiple
						size="small"
						options={availableTags}
						value={tags}
						onChange={(_event, next) => setTags(next)}
						sx={{ minWidth: 260 }}
						renderInput={(params) => (
							<TextField {...params} label="Genres" placeholder="Filtrer" />
						)}
					/>

					<ToggleButtonGroup
						exclusive
						size="small"
						value={viewMode}
						onChange={(_event, next) => next && setViewMode(next)}
					>
						<ToggleButton value="cards" aria-label="Vue en cartes">
							<GridViewIcon fontSize="small" />
						</ToggleButton>
						<ToggleButton value="table" aria-label="Vue en tableau">
							<TableRowsIcon fontSize="small" />
						</ToggleButton>
					</ToggleButtonGroup>
				</Stack>
			</Stack>

			{animes.length === 0 ? (
				<Alert severity="info">
					Aucune donnée pour cette saison. Connectez-vous et lancez un rafraîchissement
					pour la récupérer depuis Nautiljon.
				</Alert>
			) : visible.length === 0 ? (
				<Alert severity="info">Aucun anime ne correspond à ces filtres.</Alert>
			) : (
				<>
					<Typography variant="body2" color="text.secondary">
						{visible.length} anime{visible.length > 1 ? "s" : ""} sur {animes.length}
					</Typography>

					{viewMode === "cards" ? (
						<AnimeCardGrid animes={visible} now={now} />
					) : (
						<AnimeTable animes={visible} now={now} />
					)}
				</>
			)}
		</Stack>
	);
}
