import { Box, ButtonBase, Chip, LinearProgress, Stack, Typography } from "@mui/material";
import { useState } from "react";
import { shadows } from "@/config/tokens";
import { BingeBadge } from "@/view/animes/BingeBadge";
import { Mono } from "@/view/components/Mono";
import type { Anime } from "@/core/api/types";

interface Props {
	anime: Anime;
	now: Date;
}

export function AnimeCard({ anime, now }: Props) {
	const total = anime.binge.totalEpisodes;
	const released = anime.binge.releasedEpisodes;
	const progress = total && total > 0 ? Math.min(100, (released / total) * 100) : 100;
	const bingeable = anime.binge.status === "BingeableNow";

	// A source entry with no cover stores an empty string, and <img src=""> resolves to the current
	// document — a broken image behind a src that looks fine. Fall back explicitly.
	const [imageFailed, setImageFailed] = useState(false);
	const showImage = anime.imageUrl !== "" && !imageFailed;

	return (
		<ButtonBase
			href={anime.url}
			target="_blank"
			rel="noopener"
			focusRipple
			sx={{
				display: "flex",
				flexDirection: "column",
				alignItems: "stretch",
				gap: 1.5,
				textAlign: "left",
				borderRadius: 1.5,
				"&:hover .cover": {
					boxShadow: (theme) =>
						theme.palette.mode === "dark" ? shadows.dark.md : shadows.light.md,
				},
			}}
		>
			<Box
				className="cover"
				sx={{
					position: "relative",
					width: "100%",
					aspectRatio: "2 / 3",
					borderRadius: 1.5,
					overflow: "hidden",
					bgcolor: "action.hover",
					transition: (theme) => theme.transitions.create("box-shadow"),
				}}
			>
				{showImage && (
					<Box
						component="img"
						src={anime.imageUrl}
						alt=""
						loading="lazy"
						onError={() => setImageFailed(true)}
						sx={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
					/>
				)}

				{/* The countdown sits on the art: it is the reason to look at the card at all. */}
				<Box sx={{ position: "absolute", top: 12, left: 12 }}>
					<BingeBadge binge={anime.binge} now={now} size="small" />
				</Box>

				{anime.score !== null && (
					<Chip
						label={anime.score.toFixed(1)}
						size="small"
						sx={{
							position: "absolute",
							top: 12,
							right: 12,
							bgcolor: "background.paper",
							color: "text.primary",
						}}
					/>
				)}
			</Box>

			<Stack sx={{ gap: 0.5, width: "100%", minWidth: 0 }}>
				<Typography variant="subtitle1" title={anime.title} noWrap>
					{anime.title}
				</Typography>
				<Typography variant="body2" sx={{ color: "text.disabled" }} noWrap>
					{anime.studio || "Unknown studio"}
				</Typography>
			</Stack>

			<Stack direction="row" sx={{ gap: 1.25, alignItems: "center", width: "100%" }}>
				<LinearProgress
					variant="determinate"
					value={progress}
					sx={{
						flex: 1,
						// An unannounced total has no fraction to draw, so the bar stays a rule.
						"& .MuiLinearProgress-bar": {
							bgcolor:
								total === null
									? "divider"
									: bingeable
										? "success.main"
										: "text.primary",
						},
					}}
				/>
				<Mono sx={{ color: "text.disabled" }}>
					{released} / {total ?? "?"}
				</Mono>
			</Stack>
		</ButtonBase>
	);
}
