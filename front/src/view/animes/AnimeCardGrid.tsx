import { Box } from "@mui/material";
import { AnimeCard } from "@/view/animes/AnimeCard";
import type { Anime } from "@/core/api/types";

interface Props {
	animes: readonly Anime[];
	now: Date;
}

export function AnimeCardGrid({ animes, now }: Props) {
	return (
		<Box
			sx={{
				display: "grid",
				gap: 2,
				gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))",
			}}
		>
			{animes.map((anime) => (
				<AnimeCard key={anime.id} anime={anime} now={now} />
			))}
		</Box>
	);
}
