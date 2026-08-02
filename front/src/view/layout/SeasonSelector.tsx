import { IconButton, MenuItem, Stack, TextField, Tooltip } from "@mui/material";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { animeSeasons, type AnimeSeason } from "@/core/api/types";
import { seasonLabels } from "@/core/binge";

interface Props {
	year: number;
	season: AnimeSeason;
	onChange: (year: number, season: AnimeSeason) => void;
}

/** Seasons are consecutive, so the arrows step across the year boundary rather than clamping. */
function step(
	year: number,
	season: AnimeSeason,
	offset: number,
): { year: number; season: AnimeSeason } {
	const index = animeSeasons.indexOf(season) + offset;

	if (index < 0) return { year: year - 1, season: animeSeasons[animeSeasons.length - 1] };
	if (index >= animeSeasons.length) return { year: year + 1, season: animeSeasons[0] };

	return { year, season: animeSeasons[index] };
}

export function SeasonSelector({ year, season, onChange }: Props) {
	const move = (offset: number) => {
		const next = step(year, season, offset);
		onChange(next.year, next.season);
	};

	return (
		<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
			<Tooltip title="Saison précédente">
				<IconButton size="small" onClick={() => move(-1)} aria-label="Saison précédente">
					<ChevronLeftIcon />
				</IconButton>
			</Tooltip>

			<TextField
				select
				size="small"
				value={season}
				onChange={(event) => onChange(year, event.target.value as AnimeSeason)}
				sx={{ minWidth: 130 }}
			>
				{animeSeasons.map((value) => (
					<MenuItem key={value} value={value}>
						{seasonLabels[value]}
					</MenuItem>
				))}
			</TextField>

			<TextField
				type="number"
				size="small"
				value={year}
				onChange={(event) => {
					const parsed = Number.parseInt(event.target.value, 10);
					if (Number.isFinite(parsed)) onChange(parsed, season);
				}}
				sx={{ width: 100 }}
			/>

			<Tooltip title="Saison suivante">
				<IconButton size="small" onClick={() => move(1)} aria-label="Saison suivante">
					<ChevronRightIcon />
				</IconButton>
			</Tooltip>
		</Stack>
	);
}
