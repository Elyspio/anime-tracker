import { useEffect, useMemo, useState } from "react";
import ClearIcon from "@mui/icons-material/Clear";
import FilterListIcon from "@mui/icons-material/FilterList";
import {
	Alert,
	Autocomplete,
	Box,
	Button,
	Chip,
	CircularProgress,
	Divider,
	Dialog,
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
	useMediaQuery,
	useTheme,
} from "@mui/material";
import { useAnimes } from "@/core/api/queries";
import { collectGenres, matchesGenres, matchesStatus, type StatusFilter } from "@/core/binge";
import { dubFilterLabels, matchesDub, type DubFilter } from "@/core/dub";
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

const dubFilters: DubFilter[] = ["any", "upToDate", "complete"];

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
	const [dub, setDub] = useState<DubFilter>("any");
	const [filtersOpen, setFiltersOpen] = useState(false);
	const isMobile = useMediaQuery(useTheme().breakpoints.down("md"));

	useEffect(() => {
		if (!filtersOpen) return;
		const onBack = () => setFiltersOpen(false);
		window.addEventListener("popstate", onBack);
		return () => window.removeEventListener("popstate", onBack);
	}, [filtersOpen]);

	function openFilters() {
		window.history.pushState({ ...window.history.state, animeFilters: true }, "");
		setFiltersOpen(true);
	}

	function closeFilters() {
		if (window.history.state?.animeFilters) window.history.back();
		else setFiltersOpen(false);
	}

	function resetFilters() {
		setTitle("");
		setStatus("all");
		setGenres([]);
		setStudio("");
		setRange(noRangeFilter);
		setSort("score");
		setFormats([...episodicFormats]);
		setIncludeAdult(false);
		setDub("any");
	}

	const activeFilters = [
		title !== "",
		status !== "all",
		genres.length > 0,
		studio !== "",
		range.minScore !== 0,
		range.minVotes !== 0,
		formats.length !== episodicFormats.length || episodicFormats.some((format) => !formats.includes(format)),
		includeAdult,
		dub !== "any",
	].filter(Boolean).length;

	// Pin the countdown time and the empty list until the season data changes.
	const { animes, now } = useMemo(() => ({ animes: data ?? [], now: new Date() }), [data]);
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
						matchesAdult(anime, includeAdult) &&
						matchesDub(anime, dub)
				),
				sort
			),
		[animes, title, status, genres, studio, range, sort, formats, includeAdult, dub, now]
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

	const filterControls = (
		<Stack
			direction={isMobile ? "column" : "row"}
			sx={isMobile ? { alignItems: "stretch", gap: 1.5, "& > *": { width: "100%" } } : { alignItems: "center", flexWrap: "wrap", gap: 1 }}
		>
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
									<IconButton size="small" aria-label="Clear title search" onClick={() => setTitle("")}>
										<ClearIcon fontSize="small" />
									</IconButton>
								</InputAdornment>
							),
						},
					}}
				/>
			</FilterField>

			<Divider orientation={isMobile ? "horizontal" : "vertical"} flexItem sx={{ mx: isMobile ? 0 : 0.5, my: 0.5 }} />

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

			<Divider orientation={isMobile ? "horizontal" : "vertical"} flexItem sx={{ mx: isMobile ? 0 : 0.5, my: 0.5 }} />

			<FilterField label="Sort">
				<Select value={sort} onChange={(event) => setSort(event.target.value as SortKey)} variant="outlined" sx={bareSelect}>
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

			<FilterField label="French dub">
				<Select value={dub} onChange={(event) => setDub(event.target.value as DubFilter)} sx={bareSelect}>
					{dubFilters.map((filter) => (
						<MenuItem key={filter} value={filter}>
							{dubFilterLabels[filter]}
						</MenuItem>
					))}
				</Select>
			</FilterField>

			<FilterField label="Studio">
				<Select value={studio} onChange={(event) => setStudio(event.target.value)} sx={{ ...bareSelect, maxWidth: 170 }}>
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
				control={<Switch checked={includeAdult} onChange={(event) => setIncludeAdult(event.target.checked)} sx={{ mr: 1 }} />}
				label={
					<Typography variant="body2" sx={{ color: "text.secondary" }}>
						Adult
					</Typography>
				}
			/>
		</Stack>
	);

	return (
		<Stack sx={{ gap: 2.5 }}>
			{isMobile ? (
				<>
					<Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
						<Button variant="outlined" startIcon={<FilterListIcon />} onClick={openFilters}>
							Filters{activeFilters > 0 ? ` (${activeFilters})` : ""}
						</Button>
						<ToggleButtonGroup exclusive value={viewMode} onChange={(_event, next) => next && setViewMode(next)} sx={{ ml: "auto" }}>
							<ToggleButton value="cards" aria-label="Card view">
								Grid
							</ToggleButton>
							<ToggleButton value="table" aria-label="Table view">
								List
							</ToggleButton>
						</ToggleButtonGroup>
					</Stack>
					<Dialog fullScreen open={filtersOpen} onClose={closeFilters} aria-labelledby="filters-title">
						<Stack direction="row" sx={{ alignItems: "center", p: 2, borderBottom: 1, borderColor: "divider" }}>
							<Typography id="filters-title" variant="h6" sx={{ flex: 1 }}>
								Filters
							</Typography>
							<IconButton aria-label="Close filters" onClick={closeFilters}>
								<ClearIcon />
							</IconButton>
						</Stack>
						<Box sx={{ flex: 1, overflowY: "auto", p: 2 }}>{filterControls}</Box>
						<Stack direction="row" sx={{ gap: 1, p: 2, pb: "max(16px, env(safe-area-inset-bottom))", borderTop: 1, borderColor: "divider" }}>
							<Button onClick={resetFilters}>Reset filters</Button>
							<Button variant="contained" sx={{ flex: 1 }} onClick={closeFilters}>
								Show {visible.length} results
							</Button>
						</Stack>
					</Dialog>
				</>
			) : (
				<Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
					<Box sx={{ flex: 1 }}>{filterControls}</Box>
					<ToggleButtonGroup exclusive value={viewMode} onChange={(_event, next) => next && setViewMode(next)} sx={{ ml: "auto" }}>
						<ToggleButton value="cards" aria-label="Card view">
							Grid
						</ToggleButton>
						<ToggleButton value="table" aria-label="Table view">
							List
						</ToggleButton>
					</ToggleButtonGroup>
				</Stack>
			)}

			{animes.length === 0 ? (
				<Alert severity="info">Nothing stored for this season yet. Sign in and start a refresh to fetch it.</Alert>
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
