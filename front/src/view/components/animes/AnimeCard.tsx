import React from "react";
import { Card, CardActions, CardContent, CardHeader, CardMedia, Stack, Typography } from "@mui/material";
import IconButton from "@mui/material/IconButton";
import { Anime } from "@apis/backend/generated";

import FavoriteIcon from "@mui/icons-material/Favorite";
import ShareIcon from "@mui/icons-material/Share";

type AnimeCardProps = {
	data: Anime;
};

export const AnimeCard = AnimeCardFn;

function AnimeCardFn({ data }: AnimeCardProps) {
	console.count("AnimeCard " + data.id);
	return (
		<Stack>
			<Card>
				<CardHeader
					title={
						<Typography noWrap sx={{ textOverflow: "ellipsis", maxWidth: "100%" }}>
							{data.title}
						</Typography>
					}
					subheader={`${data.episodes.length}/${data.episodesCount ?? data.episodes.length > data.episodes.length ? data.episodesCount : data.episodes.length} ${data.studio}`}
				/>
				<CardMedia component="img" height="194" image={data.imageUrl} alt="Paella dish" />
				<CardContent>
					<Typography variant="body2" color="text.secondary">
						{data.description} {data.score} {data.popularity}
					</Typography>
				</CardContent>
				<CardActions disableSpacing>
					<IconButton aria-label="add to favorites">
						<FavoriteIcon />
					</IconButton>
					<IconButton aria-label="share">
						<ShareIcon />
					</IconButton>
				</CardActions>
			</Card>
		</Stack>
	);
}
