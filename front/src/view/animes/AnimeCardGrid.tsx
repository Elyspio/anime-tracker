import { Box } from "@mui/material";
import { AnimeCard } from "@/view/animes/AnimeCard";
import { useDestinationsPopover } from "@/view/animes/useDestinationsPopover";
import type { Anime } from "@/core/api/types";

interface Props {
	animes: readonly Anime[];
	now: Date;
}

export function AnimeCardGrid({ animes, now }: Props) {
	const destinations = useDestinationsPopover();

	return (
		<>
			<Box
				sx={{
					display: "grid",
					// Four columns at the width the design was drawn for, and it reflows below that.
					columnGap: 3,
					rowGap: 3.5,
					gridTemplateColumns: "repeat(auto-fill, minmax(228px, 1fr))",
				}}
			>
				{animes.map((anime) => (
					<AnimeCard key={anime.id} anime={anime} now={now} onOpen={destinations.open} />
				))}
			</Box>
			{destinations.popover}
		</>
	);
}
