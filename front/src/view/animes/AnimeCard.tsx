import {
	Box,
	Card,
	CardActionArea,
	CardContent,
	LinearProgress,
	Stack,
	Typography,
} from "@mui/material";
import StarIcon from "@mui/icons-material/Star";
import PersonIcon from "@mui/icons-material/Person";
import { BingeBadge } from "@/view/animes/BingeBadge";
import type { Anime } from "@/core/api/types";

interface Props {
	anime: Anime;
	now: Date;
}

export function AnimeCard({ anime, now }: Props) {
	const total = anime.binge.totalEpisodes;
	const released = anime.binge.releasedEpisodes;
	const progress = total && total > 0 ? Math.min(100, (released / total) * 100) : 0;

	return (
		<Card sx={{ height: "100%", display: "flex", flexDirection: "column" }}>
			<CardActionArea
				href={anime.url}
				target="_blank"
				rel="noopener"
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "stretch",
					height: "100%",
				}}
			>
				<Box sx={{ position: "relative" }}>
					<Box
						component="img"
						src={anime.imageUrl}
						alt=""
						loading="lazy"
						sx={{
							width: "100%",
							aspectRatio: "2 / 3",
							objectFit: "cover",
							display: "block",
						}}
					/>
					{/* The countdown sits on the art: it is the reason to look at the card at all. */}
					<Box sx={{ position: "absolute", top: 8, left: 8 }}>
						<BingeBadge binge={anime.binge} now={now} size="small" />
					</Box>
				</Box>

				<CardContent
					sx={{
						flexGrow: 1,
						display: "flex",
						flexDirection: "column",
						gap: 1,
						width: "100%",
					}}
				>
					<Typography variant="subtitle1" sx={{ fontWeight: 600, lineHeight: 1.3 }}>
						{anime.title}
					</Typography>

					<Typography variant="caption" color="text.secondary" noWrap>
						{anime.studio || "Studio inconnu"}
					</Typography>

					<Box sx={{ mt: "auto" }}>
						<Stack direction="row" sx={{ justifyContent: "space-between", mb: 0.5 }}>
							<Typography variant="caption" color="text.secondary">
								{released} / {total ?? "?"} ép.
							</Typography>
							<Stack direction="row" spacing={1.5}>
								{anime.score !== null && (
									<Stack
										direction="row"
										spacing={0.5}
										sx={{ alignItems: "center" }}
									>
										<StarIcon sx={{ fontSize: 14, color: "warning.main" }} />
										<Typography variant="caption">
											{anime.score.toFixed(1)}
										</Typography>
									</Stack>
								)}
								<Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
									<PersonIcon sx={{ fontSize: 14, color: "text.secondary" }} />
									<Typography variant="caption">{anime.popularity}</Typography>
								</Stack>
							</Stack>
						</Stack>

						<LinearProgress
							variant="determinate"
							value={progress}
							color={anime.binge.status === "BingeableNow" ? "success" : "primary"}
							sx={{ height: 4, borderRadius: 2 }}
						/>
					</Box>
				</CardContent>
			</CardActionArea>
		</Card>
	);
}
