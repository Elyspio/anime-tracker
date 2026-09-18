import { useMemo, useState } from "react";
import ClearIcon from "@mui/icons-material/Clear";
import {
	Alert,
	Autocomplete,
	Box,
	Chip,
	CircularProgress,
	Divider,
	FormControlLabel,
	IconButton,
	InputAdornment,
	MenuItem,
	Select,
	Stack,
	Switch,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import { useAnimes } from "@/core/api/queries";
import { collectGenres, matchesGenres, matchesStatus, type StatusFilter } from "@/core/binge";
import {
	collectStudios,
	episodicFormats,
	formatLabels,
	matchesAdult,
	matchesFormat,
	matchesRange,
	matchesStudio,
	matchesTitle,
	noRangeFilter,
	sortAnimes,
	sortLabels,
	type SortKey,
} from "@/core/ranking";
import { useViewMode } from "@/config/viewMode";
import { AnimeCardGrid } from "@/view/animes/AnimeCardGrid";
import { AnimeTable } from "@/view/animes/AnimeTable";
import { FilterField } from "@/view/components/FilterField";
import { Mono } from "@/view/components/Mono";
import { fonts } from "@/config/tokens";
import { animeFormats, type AnimeFormat, type AnimeSeason } from "@/core/api/types";

const statusFilters: { value: StatusFilter; label: string }[] = [
	{ value: "all", label: "All" },
	{ value: "bingeable", label: "Bingeable" },
	{ value: "soon", label: "Soon" },
	{ value: "unknown", label: "No end" },
];

const sortKeys: SortKey[] = ["score", "votes", "binge"];

/** Thresholds worth offering. Finer steps would be precision nobody triages on. */
const minScoreOptions = [0, 5, 6, 7, 8, 9];
const minVotesOptions = [0, 10, 50, 100, 500, 1000];

/** Borderless: the FilterField around it already draws the box. */
const bareSelect = {
	"& .MuiSelect-select": { py: 0, pl: 0 },
	"& .MuiOutlinedInput-notchedOutline": { border: 0 },
	"&:hover .MuiOutlinedInput-notchedOutline": { border: 0 },
	"&.Mui-focused .MuiOutlinedInput-notchedOutline": { border: 0 },
};

const bareInput = {
	"& .MuiOutlinedInput-root": { p: 0, minHeight: 32 },
	"& .MuiOutlinedInput-notchedOutline": { border: 0 },
	"&:hover .MuiOutlinedInput-notchedOutline": { border: 0 },
	"& .MuiOutlinedInput-root.Mui-focused .MuiOutlinedInput-notchedOutline": { border: 0 },
};

interface Props {
	year: number;
	season: AnimeSeason;
}

export function AnimesPage({ year, season }: Props) {
	const { data, isPending, error } = useAnimes(year, season);
	const [viewMode, setViewMode] = useViewMode();
	const [title, setTitle] = useState("");
	const [status, setStatus] = useState<StatusFilter>("all");
	const [genres, setGenres] = useState<string[]>([]);
	const [studio, setStudio] = useState("");
	const [range, setRange] = useState(noRangeFilter);
	const [sort, setSort] = useState<SortKey>("score");
	const [formats, setFormats] = useState<AnimeFormat[]>([...episodicFormats]);
	const [includeAdult, setIncludeAdult] = useState(false);

	// Pinned per render pass so every countdown in the list is measured from the same instant.
	const now = useMemo(() => new Date(), [data]);

	const animes = data ?? [];
	const availableGenres = useMemo(() => collectGenres(animes), [animes]);
	const availableStudios = useMemo(() => collectStudios(animes), [animes]);
	const visible = useMemo(
		() =>
			sortAnimes(
				animes.filter(
					(anime) =>
						matchesTitle(anime, title) &&
						matchesStatus(anime, status, now) &&
						matchesGenres(anime, genres) &&
						matchesStudio(anime, studio) &&
						matchesRange(anime, range) &&
						matchesFormat(anime, formats) &&
						matchesAdult(anime, includeAdult),
				),
				sort,
			),
		[animes, title, status, genres, studio, range, sort, formats, includeAdult, now],
	);

	if (isPending) {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
				<CircularProgress size={28} />
			</Box>
		);
	}

	if (error) {
		return <Alert severity="error">Could not load the season: {error.message}</Alert>;
	}

	return (
		<Stack sx={{ gap: 2.5 }}>
			<Stack direction="row" sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
				<FilterField label="Title" sx={{ minWidth: 220 }}>
					<TextField
						size="small"
						value={title}
						onChange={(event) => setTitle(event.target.value)}
						placeholder="Search…"
						sx={{ flex: 1, ...bareInput }}
						slotProps={{
							htmlInput: { "aria-label": "Search by title" },
							input: {
								endAdornment: title && (
									<InputAdornment position="end">
										<IconButton
											size="small"
											aria-label="Clear title search"
											onClick={() => setTitle("")}
										>
											<ClearIcon fontSize="small" />
										</IconButton>
									</InputAdornment>
								),
							},
						}}
					/>
				</FilterField>

				<Divider orientation="vertical" flexItem sx={{ mx: 0.5, my: 0.5 }} />

				{statusFilters.map((filter) => {
					const selected = status === filter.value;

					return (
						<Chip
							key={filter.value}
							label={filter.label}
							onClick={() => setStatus(filter.value)}
							sx={{
								height: 30,
								fontFamily: fonts.sans,
								fontSize: 13,
								bgcolor: selected ? "text.primary" : "background.paper",
								color: selected ? "background.paper" : "text.secondary",
								borderColor: selected ? "text.primary" : "divider",
								"&:hover": {
									bgcolor: selected ? "text.primary" : "background.paper",
									borderColor: selected ? "text.primary" : "text.disabled",
									color: selected ? "background.paper" : "text.primary",
								},
							}}
						/>
					);
				})}

				<Divider orientation="vertical" flexItem sx={{ mx: 0.5, my: 0.5 }} />

				<FilterField label="Sort">
					<Select
						value={sort}
						onChange={(event) => setSort(event.target.value as SortKey)}
						variant="outlined"
						sx={bareSelect}
					>
						{sortKeys.map((key) => (
							<MenuItem key={key} value={key}>
								{sortLabels[key]}
							</MenuItem>
						))}
					</Select>
				</FilterField>

				<FilterField label="Rating">
					<Select
						value={range.minScore}
						onChange={(event) =>
							setRange((previous) => ({
								...previous,
								minScore: Number(event.target.value),
							}))
						}
						sx={{ ...bareSelect, fontFamily: fonts.mono }}
					>
						{minScoreOptions.map((value) => (
							<MenuItem key={value} value={value}>
								{value === 0 ? "Any" : `${value}+`}
							</MenuItem>
						))}
					</Select>
				</FilterField>

				<FilterField label="Ratings">
					<Select
						value={range.minVotes}
						onChange={(event) =>
							setRange((previous) => ({
								...previous,
								minVotes: Number(event.target.value),
							}))
						}
						sx={{ ...bareSelect, fontFamily: fonts.mono }}
					>
						{minVotesOptions.map((value) => (
							<MenuItem key={value} value={value}>
								{value === 0 ? "Any" : `${value.toLocaleString()}+`}
							</MenuItem>
						))}
					</Select>
				</FilterField>

				<FilterField label="Studio">
					<Select
						value={studio}
						onChange={(event) => setStudio(event.target.value)}
						sx={{ ...bareSelect, maxWidth: 170 }}
					>
						<MenuItem value="">All</MenuItem>
						{availableStudios.map((name) => (
							<MenuItem key={name} value={name}>
								{name}
							</MenuItem>
						))}
					</Select>
				</FilterField>

				<FilterField label="Formats" sx={{ minWidth: 210 }}>
					<Autocomplete
						multiple
						size="small"
						disableCloseOnSelect
						options={[...animeFormats]}
						value={formats}
						onChange={(_event, next) => setFormats(next)}
						getOptionLabel={(option) => formatLabels[option]}
						sx={{ flex: 1, ...bareInput }}
						renderInput={(params) => <TextField {...params} placeholder="All" />}
					/>
				</FilterField>

				<FilterField label="Genres" sx={{ minWidth: 210 }}>
					<Autocomplete
						multiple
						size="small"
						options={availableGenres}
						value={genres}
						onChange={(_event, next) => setGenres(next)}
						sx={{ flex: 1, ...bareInput }}
						renderInput={(params) => <TextField {...params} placeholder="All" />}
					/>
				</FilterField>

				<FormControlLabel
					sx={{ ml: 0.5, mr: 0 }}
					control={
						<Switch
							checked={includeAdult}
							onChange={(event) => setIncludeAdult(event.target.checked)}
							sx={{ mr: 1 }}
						/>
					}
					label={
						<Typography variant="body2" sx={{ color: "text.secondary" }}>
							Adult
						</Typography>
					}
				/>

				<ToggleButtonGroup
					exclusive
					value={viewMode}
					onChange={(_event, next) => next && setViewMode(next)}
					sx={{ ml: "auto" }}
				>
					<ToggleButton value="cards" aria-label="Card view">
						Grid
					</ToggleButton>
					<ToggleButton value="table" aria-label="Table view">
						List
					</ToggleButton>
				</ToggleButtonGroup>
			</Stack>

			{animes.length === 0 ? (
				<Alert severity="info">
					Nothing stored for this season yet. Sign in and start a refresh to fetch it.
				</Alert>
			) : (
				<>
					<Stack
						direction="row"
						sx={{
							alignItems: "baseline",
							gap: 1.25,
							pb: 0.75,
							borderBottom: 1,
							borderColor: "divider",
						}}
					>
						<Mono sx={{ fontSize: 19, color: "text.primary" }}>{visible.length}</Mono>
						<Typography variant="body2" sx={{ color: "text.disabled" }}>
							of {animes.length} titles this season
						</Typography>
					</Stack>

					{visible.length === 0 ? (
						<Alert severity="info">No anime matches these filters.</Alert>
					) : viewMode === "cards" ? (
						<AnimeCardGrid animes={visible} now={now} />
					) : (
						<AnimeTable animes={visible} now={now} />
					)}
				</>
			)}
		</Stack>
	);
}
