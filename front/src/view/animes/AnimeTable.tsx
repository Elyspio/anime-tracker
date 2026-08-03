import { useMemo, useState } from "react";
import {
	Link,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	TableSortLabel,
} from "@mui/material";
import { BingeBadge } from "@/view/animes/BingeBadge";
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
	{ key: "episodes", label: "Episodes", numeric: true },
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

interface Props {
	animes: readonly Anime[];
	now: Date;
}

export function AnimeTable({ animes, now }: Props) {
	const [sortKey, setSortKey] = useState<SortKey>("binge");
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
					{sorted.map((anime) => (
						<TableRow key={anime.id} hover>
							<TableCell>
								<BingeBadge binge={anime.binge} now={now} size="small" />
							</TableCell>
							<TableCell>
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
							<TableCell>{anime.studio || "—"}</TableCell>
							<TableCell align="right">
								{anime.binge.releasedEpisodes} / {anime.binge.totalEpisodes ?? "?"}
							</TableCell>
							<TableCell align="right">{anime.score?.toFixed(1) ?? "—"}</TableCell>
							<TableCell align="right">{anime.votesCount ?? "—"}</TableCell>
							<TableCell align="right">{anime.popularity}</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</TableContainer>
	);
}
