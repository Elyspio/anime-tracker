import { useMemo, useState } from "react";
import {
	Box,
	Link,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	TableSortLabel,
	Tooltip,
} from "@mui/material";
import { formatBingeChip } from "@/core/binge";
import { bingeDotColor } from "@/view/animes/BingeBadge";
import { Mono } from "@/view/components/Mono";
import type { Anime } from "@/core/api/types";

type SortKey = "binge" | "title" | "studio" | "score" | "votes" | "episodes" | "popularity";

interface Column {
	key: SortKey;
	label: string;
	numeric: boolean;
}

const columns: Column[] = [
	{ key: "binge", label: "Bingeable", numeric: false },
	{ key: "title", label: "Title", numeric: false },
	{ key: "studio", label: "Studio", numeric: false },
	{ key: "episodes", label: "Episodes", numeric: false },
	{ key: "score", label: "Rating", numeric: true },
	{ key: "votes", label: "Ratings", numeric: true },
	{ key: "popularity", label: "Popularity", numeric: true },
];

/** Sorts undefined-last regardless of direction, so "no end announced" never crowds the top. */
function compare(a: Anime, b: Anime, key: SortKey): number {
	switch (key) {
		case "binge": {
			const left = a.binge.bingeableAt;
			const right = b.binge.bingeableAt;
			if (left === right) return 0;
			if (left === null) return 1;
			if (right === null) return -1;
			return left.localeCompare(right);
		}
		case "title":
			return a.title.localeCompare(b.title);
		case "studio":
			return a.studio.localeCompare(b.studio);
		case "score":
			return (b.score ?? -1) - (a.score ?? -1);
		case "votes":
			return (b.votesCount ?? -1) - (a.votesCount ?? -1);
		case "episodes":
			return b.binge.releasedEpisodes - a.binge.releasedEpisodes;
		case "popularity":
			return b.popularity - a.popularity;
	}
}

function Poster({ anime }: { anime: Anime }) {
	return (
		<Box
			sx={{
				width: 40,
				height: 56,
				borderRadius: 0.75,
				overflow: "hidden",
				bgcolor: "action.hover",
			}}
		>
			{anime.imageUrl !== "" && (
				<Box
					component="img"
					src={anime.imageUrl}
					alt=""
					loading="lazy"
					sx={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
				/>
			)}
		</Box>
	);
}

/** The same rule the card draws, at row scale. */
function EpisodeProgress({ anime }: { anime: Anime }) {
	const total = anime.binge.totalEpisodes;
	const released = anime.binge.releasedEpisodes;
	const filled = total && total > 0 ? Math.min(100, (released / total) * 100) : 100;

	return (
		<Stack direction="row" sx={{ alignItems: "center", gap: 1, minWidth: 130 }}>
			<Box sx={{ flex: 1, height: 2, bgcolor: "divider" }}>
				<Box
					sx={{
						height: "100%",
						width: `${filled}%`,
						bgcolor:
							total === null
								? "divider"
								: anime.binge.status === "BingeableNow"
									? "success.main"
									: "text.primary",
					}}
				/>
			</Box>
			<Mono sx={{ color: "text.disabled" }}>
				{released} / {total ?? "?"}
			</Mono>
		</Stack>
	);
}

interface Props {
	animes: readonly Anime[];
	now: Date;
}

export function AnimeTable({ animes, now }: Props) {
	// Opens on the rating, like the grid: a season is triaged by reputation before it is by date.
	const [sortKey, setSortKey] = useState<SortKey>("score");
	const [descending, setDescending] = useState(false);

	const sorted = useMemo(() => {
		const rows = [...animes].sort((a, b) => compare(a, b, sortKey));
		return descending ? rows.reverse() : rows;
	}, [animes, sortKey, descending]);

	const toggle = (key: SortKey) => {
		if (key === sortKey) {
			setDescending((previous) => !previous);
			return;
		}
		setSortKey(key);
		setDescending(false);
	};

	return (
		<TableContainer sx={{ overflowX: "auto" }}>
			<Table size="small" stickyHeader>
				<TableHead>
					<TableRow>
						<TableCell sx={{ width: 44 }} />
						<TableCell sx={{ width: 56 }} />
						{columns.map((column) => (
							<TableCell
								key={column.key}
								align={column.numeric ? "right" : "left"}
								sortDirection={
									sortKey === column.key ? (descending ? "desc" : "asc") : false
								}
							>
								<TableSortLabel
									active={sortKey === column.key}
									direction={descending ? "desc" : "asc"}
									onClick={() => toggle(column.key)}
								>
									{column.label}
								</TableSortLabel>
							</TableCell>
						))}
					</TableRow>
				</TableHead>
				<TableBody>
					{sorted.map((anime, index) => {
						const chip = formatBingeChip(anime.binge, now);

						return (
							<TableRow key={anime.id} hover>
								<TableCell>
									<Mono sx={{ color: "text.disabled" }}>
										{String(index + 1).padStart(2, "0")}
									</Mono>
								</TableCell>
								<TableCell sx={{ py: 1 }}>
									<Poster anime={anime} />
								</TableCell>
								<TableCell>
									<Tooltip title={chip.title}>
										<Stack
											direction="row"
											sx={{ alignItems: "center", gap: 0.75 }}
										>
											<Box
												sx={{
													width: 5,
													height: 5,
													borderRadius: "50%",
													bgcolor: bingeDotColor(chip.tone),
													flexShrink: 0,
												}}
											/>
											<Mono sx={{ color: "text.secondary" }}>
												{chip.label}
											</Mono>
										</Stack>
									</Tooltip>
								</TableCell>
								<TableCell sx={{ fontWeight: 500 }}>
									<Link
										href={anime.url}
										target="_blank"
										rel="noopener"
										underline="hover"
										color="inherit"
									>
										{anime.title}
									</Link>
								</TableCell>
								<TableCell sx={{ color: "text.disabled" }}>
									{anime.studio || "—"}
								</TableCell>
								<TableCell>
									<EpisodeProgress anime={anime} />
								</TableCell>
								<TableCell align="right">
									<Mono variant="body2">{anime.score?.toFixed(1) ?? "—"}</Mono>
								</TableCell>
								<TableCell align="right">
									<Mono sx={{ color: "text.disabled" }}>
										{anime.votesCount?.toLocaleString() ?? "—"}
									</Mono>
								</TableCell>
								<TableCell align="right">
									<Mono sx={{ color: "text.disabled" }}>
										{anime.popularity.toLocaleString()}
									</Mono>
								</TableCell>
							</TableRow>
						);
					})}
				</TableBody>
			</Table>
		</TableContainer>
	);
}
