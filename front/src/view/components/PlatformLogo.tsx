import { Box, SvgIcon } from "@mui/material";
import { fonts } from "@/config/tokens";

/**
 * Marks from the Simple Icons set (CC0 1.0), embedded so a reader's browser asks no third party for
 * them. The marks themselves belong to their owners and stand here only to name where a link leads.
 * A platform absent from the table falls back to a lettered tile: the set has no ADN or HIDIVE.
 */
const paths: Record<string, string> = {
	anilist:
		"M24 17.53v2.421c0 .71-.391 1.101-1.1 1.101h-5l-.057-.165L11.84 3.736c.106-.502.46-.788 1.053-.788h2.422c.71 0 1.1.391 1.1 1.1v12.38H22.9c.71 0 1.1.392 1.1 1.101zM11.034 2.947l6.337 18.104h-4.918l-1.052-3.131H6.019l-1.077 3.131H0L6.361 2.948h4.673zm-.66 10.96-1.69-5.014-1.541 5.015h3.23z",
	crunchyroll:
		"M2.909 13.436C2.914 7.61 7.642 2.893 13.468 2.898c5.576.005 10.137 4.339 10.51 9.819q.021-.351.022-.706C24.007 5.385 18.64.006 12.012 0S.007 5.36 0 11.988 5.36 23.994 11.988 24q.412 0 .815-.027c-5.526-.338-9.9-4.928-9.894-10.538Zm16.284.155a4.1 4.1 0 0 1-4.095-4.103 4.1 4.1 0 0 1 2.712-3.855 8.95 8.95 0 0 0-4.187-1.037 9.007 9.007 0 1 0 8.997 9.016q-.001-.847-.15-1.651a4.1 4.1 0 0 1-3.278 1.63Z",
	netflix:
		"m5.398 0 8.348 23.602c2.346.059 4.856.398 4.856.398L10.113 0H5.398zm8.489 0v9.172l4.715 13.33V0h-4.715zM5.398 1.5V24c1.873-.225 2.81-.312 4.715-.398V14.83L5.398 1.5z",
	youtube:
		"M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z",
};

/** "Bilibili TV" and "bilibili tv" are one platform; so are "Disney Plus" and "DisneyPlus". */
function keyOf(site: string): string {
	return site.toLowerCase().replace(/[^a-z0-9]/g, "");
}

interface Props {
	site: string;
	size?: number;
}

export function PlatformLogo({ site, size = 20 }: Props) {
	const path = paths[keyOf(site)];

	if (path) {
		return (
			<SvgIcon sx={{ fontSize: size, color: "text.primary" }}>
				<path d={path} />
			</SvgIcon>
		);
	}

	return (
		<Box
			aria-hidden
			sx={{
				width: size,
				height: size,
				display: "grid",
				placeItems: "center",
				border: 1,
				borderColor: "divider",
				borderRadius: 0.75,
				color: "text.secondary",
				fontFamily: fonts.sans,
				fontSize: size * 0.55,
				fontWeight: 600,
				lineHeight: 1,
			}}
		>
			{site.trim().charAt(0).toUpperCase()}
		</Box>
	);
}
