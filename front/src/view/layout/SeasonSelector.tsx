import { useState, type MouseEvent } from "react";
import {
	Box,
	ButtonBase,
	Divider,
	IconButton,
	Menu,
	MenuItem,
	Stack,
	Tooltip,
} from "@mui/material";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import RemoveIcon from "@mui/icons-material/Remove";
import AddIcon from "@mui/icons-material/Add";
import { animeSeasons, type AnimeSeason } from "@/core/api/types";
import { seasonLabels } from "@/core/binge";
import { Eyebrow } from "@/view/components/Eyebrow";
import { Mono } from "@/view/components/Mono";

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
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	const move = (offset: number) => {
		const next = step(year, season, offset);
		onChange(next.year, next.season);
	};

	const open = (event: MouseEvent<HTMLElement>) => setAnchor(event.currentTarget);
	const close = () => setAnchor(null);

	return (
		<>
			<Stack
				direction="row"
				sx={{
					alignItems: "center",
					gap: "2px",
					p: "3px",
					borderRadius: 1,
					border: 1,
					borderColor: "divider",
					bgcolor: "background.default",
				}}
			>
				<Tooltip title="Previous season">
					<IconButton size="small" onClick={() => move(-1)} aria-label="Previous season">
						<ChevronLeftIcon fontSize="small" />
					</IconButton>
				</Tooltip>

				<ButtonBase
					onClick={open}
					aria-label="Choose a season"
					sx={{
						minWidth: 140,
						justifyContent: "center",
						borderRadius: 0.75,
						px: 1,
						py: 0.5,
						"&:hover": { bgcolor: "background.paper" },
					}}
				>
					<Mono sx={{ fontSize: 12.5, color: "text.primary" }}>
						{seasonLabels[season]} {year}
					</Mono>
				</ButtonBase>

				<Tooltip title="Next season">
					<IconButton size="small" onClick={() => move(1)} aria-label="Next season">
						<ChevronRightIcon fontSize="small" />
					</IconButton>
				</Tooltip>
			</Stack>

			<Menu anchorEl={anchor} open={Boolean(anchor)} onClose={close}>
				{animeSeasons.map((value) => (
					<MenuItem
						key={value}
						selected={value === season}
						onClick={() => {
							onChange(year, value);
							close();
						}}
					>
						{seasonLabels[value]}
					</MenuItem>
				))}

				<Divider />

				{/* Stepping a year at a time: nothing further out than a few years is ever stored. */}
				<Stack
					direction="row"
					sx={{ alignItems: "center", justifyContent: "space-between", px: 1.5, py: 0.5 }}
				>
					<Eyebrow>Year</Eyebrow>
					<Stack direction="row" sx={{ alignItems: "center", gap: 0.5 }}>
						<IconButton
							size="small"
							onClick={() => onChange(year - 1, season)}
							aria-label="Previous year"
						>
							<RemoveIcon fontSize="small" />
						</IconButton>
						<Box sx={{ minWidth: 44, textAlign: "center" }}>
							<Mono variant="body2">{year}</Mono>
						</Box>
						<IconButton
							size="small"
							onClick={() => onChange(year + 1, season)}
							aria-label="Next year"
						>
							<AddIcon fontSize="small" />
						</IconButton>
					</Stack>
				</Stack>
			</Menu>
		</>
	);
}
