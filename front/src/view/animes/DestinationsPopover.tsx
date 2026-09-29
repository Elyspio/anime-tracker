import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import { List, ListItemButton, ListItemIcon, ListItemText, Popover, Typography } from "@mui/material";
import { destinationsOf } from "@/core/destinations";
import { PlatformLogo } from "@/view/components/PlatformLogo";
import type { Anime } from "@/core/api/types";

export interface DestinationsTarget {
	anime: Anime;
	anchor: HTMLElement;
}

interface Props {
	target: DestinationsTarget | null;
	onClose: () => void;
}

/**
 * What a click on an anime opens. A card used to be one link to the source; it is now a choice
 * between the source and each platform the anime can be watched on, so the choice sits here.
 */
export function DestinationsPopover({ target, onClose }: Props) {
	const destinations = target ? destinationsOf(target.anime) : [];

	return (
		<Popover
			open={target !== null}
			anchorEl={target?.anchor}
			onClose={onClose}
			anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
			slotProps={{ paper: { sx: { minWidth: 220, maxWidth: 320 } } }}
		>
			<Typography variant="caption" sx={{ display: "block", px: 2, pt: 1.5, pb: 0.5, color: "text.disabled" }}>
				Open on
			</Typography>
			{destinations.length === 0 ? (
				<Typography variant="body2" sx={{ px: 2, pb: 1.5, color: "text.secondary" }}>
					No page known for this title.
				</Typography>
			) : (
				<List dense disablePadding sx={{ pb: 0.5 }}>
					{destinations.map((destination) => (
						<ListItemButton key={destination.site} href={destination.url} target="_blank" rel="noopener noreferrer" onClick={onClose}>
							<ListItemIcon sx={{ minWidth: 32 }}>
								<PlatformLogo site={destination.site} />
							</ListItemIcon>
							<ListItemText primary={destination.site} />
							<OpenInNewIcon sx={{ fontSize: 14, color: "text.disabled", ml: 1 }} />
						</ListItemButton>
					))}
				</List>
			)}
		</Popover>
	);
}
