import React from "react";
import { Card, CardActions, CardContent, CardHeader, CardMedia, Stack, Typography } from "@mui/material";
import IconButton from "@mui/material/IconButton";
import { Anime } from "@apis/backend/generated";

import FavoriteIcon from "@mui/icons-material/Favorite";
import ShareIcon from "@mui/icons-material/Share";
import { Person } from "@mui/icons-material";

type AnimeCardProps = {
	data: Anime;
};

export const AnimeCard = AnimeCardFn;

function AnimeCardFn({ data }: AnimeCardProps) {
	console.count("AnimeCard " + data.id);
	return (
		<Stack height={"100%"}>
			<Card sx={{ height: "100%" }}>
				<CardHeader
					title={
						<Typography noWrap sx={{ textOverflow: "ellipsis", maxWidth: "100%" }}>
							{data.title}
						</Typography>
					}
					subheader={`${data.episodes.length}/${data.episodesCount ?? data.episodes.length > data.episodes.length ? data.episodesCount : data.episodes.length} ${data.studio}`}
				/>
				<CardMedia component="img" height={300} image={data.imageUrl} alt="Paella dish" />
				<CardActions disableSpacing>
					<Stack direction={"row"} justifyContent={"space-between"}>
						<Typography>{data.score} / 10</Typography>
						<Stack spacing={2} direction={"row"}>
							<Person />
							<Typography>{data.popularity}</Typography>
						</Stack>
					</Stack>
				</CardActions>
			</Card>
		</Stack>
	);
}
