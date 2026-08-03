import { useMemo, useState } from "react";
import {
	Alert,
	Autocomplete,
	Box,
	Chip,
	CircularProgress,
	FormControlLabel,
	MenuItem,
	Stack,
	Switch,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import GridViewIcon from "@mui/icons-material/GridView";
import TableRowsIcon from "@mui/icons-material/TableRows";
import { useAnimes } from "@/core/api/queries";
import { collectGenres, matchesGenres, matchesStatus, type StatusFilter } from "@/core/binge";
import {
	episodicFormats,
	formatLabels,
	matchesAdult,
	matchesFormat,
	matchesRange,
	noRangeFilter,
	sortAnimes,
	sortLabels,
	type SortKey,
} from "@/core/ranking";
import { useViewMode } from "@/config/viewMode";
import { AnimeCardGrid } from "@/view/animes/AnimeCardGrid";
import { AnimeTable } from "@/view/animes/AnimeTable";
import { animeFormats, type AnimeFormat, type AnimeSeason } from "@/core/api/types";

const statusFilters: { value: StatusFilter; label: string }[] = [
	{ value: "all", label: "All" },
	{ value: "bingeable", label: "Bingeable" },
	{ value: "soon", label: "Soon" },
	{ value: "unknown", label: "No end announced" },
];

const sortKeys: SortKey[] = ["score", "votes", "binge"];

/** Thresholds worth offering. Finer steps would be precision nobody triages on. */
const minScoreOptions = [0, 5, 6, 7, 8, 9];
const minVotesOptions = [0, 10, 50, 100, 500, 1000];

interface Props {
	year: number;
	season: AnimeSeason;
}

export function AnimesPage({ year, season }: Props) {
	const { data, isPending, error } = useAnimes(year, season);
	const [viewMode, setViewMode] = useViewMode();
	const [status, setStatus] = useState<StatusFilter>("all");
	const [genres, setGenres] = useState<string[]>([]);
	const [range, setRange] = useState(noRangeFilter);
	const [sort, setSort] = useState<SortKey>("score");
	const [formats, setFormats] = useState<AnimeFormat[]>([...episodicFormats]);
	const [includeAdult, setIncludeAdult] = useState(false);

	// Pinned per render pass so every countdown in the list is measured from the same instant.
	const now = useMemo(() => new Date(), [data]);

	const animes = data ?? [];
	const availableGenres = useMemo(() => collectGenres(animes), [animes]);
	const visible = useMemo(
		() =>
			sortAnimes(
				animes.filter(
					(anime) =>
						matchesStatus(anime, status, now) &&
						matchesGenres(anime, genres) &&
						matchesRange(anime, range) &&
						matchesFormat(anime, formats) &&
						matchesAdult(anime, includeAdult),
				),
				sort,
			),
		[animes, status, genres, range, sort, formats, includeAdult, now],
	);

	if (isPending) {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
				<CircularProgress />
			</Box>
		);
	}

	if (error) {
		return <Alert severity="error">Could not load the season: {error.message}</Alert>;
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

				<Stack
					direction="row"
					spacing={2}
					sx={{ alignItems: "center", flexWrap: "wrap", gap: 2 }}
				>
					<TextField
						select
						size="small"
						label="Sort by"
						value={sort}
						onChange={(event) => setSort(event.target.value as SortKey)}
						sx={{ minWidth: 190 }}
					>
						{sortKeys.map((key) => (
							<MenuItem key={key} value={key}>
								{sortLabels[key]}
							</MenuItem>
						))}
					</TextField>

					<TextField
						select
						size="small"
						label="Min rating"
						value={range.minScore}
						onChange={(event) =>
							setRange((previous) => ({
								...previous,
								minScore: Number(event.target.value),
							}))
						}
						sx={{ minWidth: 120 }}
					>
						{minScoreOptions.map((value) => (
							<MenuItem key={value} value={value}>
								{value === 0 ? "Any" : `${value}/10`}
							</MenuItem>
						))}
					</TextField>

					<TextField
						select
						size="small"
						label="Min ratings"
						value={range.minVotes}
						onChange={(event) =>
							setRange((previous) => ({
								...previous,
								minVotes: Number(event.target.value),
							}))
						}
						sx={{ minWidth: 120 }}
					>
						{minVotesOptions.map((value) => (
							<MenuItem key={value} value={value}>
								{value === 0 ? "Any" : `${value}+`}
							</MenuItem>
						))}
					</TextField>

					<Autocomplete
						multiple
						size="small"
						disableCloseOnSelect
						options={[...animeFormats]}
						value={formats}
						onChange={(_event, next) => setFormats(next)}
						getOptionLabel={(option) => formatLabels[option]}
						sx={{ minWidth: 220 }}
						renderInput={(params) => <TextField {...params} label="Formats" />}
					/>

					<Autocomplete
						multiple
						size="small"
						options={availableGenres}
						value={genres}
						onChange={(_event, next) => setGenres(next)}
						sx={{ minWidth: 240 }}
						renderInput={(params) => (
							<TextField {...params} label="Genres" placeholder="Filter" />
						)}
					/>

					<FormControlLabel
						control={
							<Switch
								size="small"
								checked={includeAdult}
								onChange={(event) => setIncludeAdult(event.target.checked)}
							/>
						}
						label="Adult"
					/>

					<ToggleButtonGroup
						exclusive
						size="small"
						value={viewMode}
						onChange={(_event, next) => next && setViewMode(next)}
					>
						<ToggleButton value="cards" aria-label="Card view">
							<GridViewIcon fontSize="small" />
						</ToggleButton>
						<ToggleButton value="table" aria-label="Table view">
							<TableRowsIcon fontSize="small" />
						</ToggleButton>
					</ToggleButtonGroup>
				</Stack>
			</Stack>

			{animes.length === 0 ? (
				<Alert severity="info">
					Nothing stored for this season yet. Sign in and start a refresh to fetch it.
				</Alert>
			) : visible.length === 0 ? (
				<Alert severity="info">No anime matches these filters.</Alert>
			) : (
				<>
					<Typography variant="body2" color="text.secondary">
						{visible.length} of {animes.length} anime{animes.length > 1 ? "s" : ""}
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
