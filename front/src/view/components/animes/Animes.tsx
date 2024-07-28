import * as React from "react";
import { AnimeCard } from "./AnimeCard";
import { StoreState, useAppSelector } from "@store";
import Grid from "@mui/material/Unstable_Grid2";
import { createSelector } from "@reduxjs/toolkit";

const animesSelector = createSelector([(s: StoreState) => s.animes.data], (animes) => {
	return { animes: [...animes].sort((a, b) => (b.popularity < a.popularity ? -1 : 1)) };
});

export const Animes = () => {
	const { animes } = useAppSelector(animesSelector);
	return (
		<Grid container spacing={2} height={"100%"} overflow={"auto"}>
			{animes.map((anime) => (
				<Grid key={anime.id} md={3} xl={2}>
					<AnimeCard data={anime} />
				</Grid>
			))}
		</Grid>
	);
};
