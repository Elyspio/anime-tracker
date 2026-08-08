import { Box } from "@mui/material";
import { darkTokens, fonts, lightTokens } from "@/config/tokens";

interface Props {
	name: string;
}

/** Same name, same colour, every session — no state to store and no palette to run out of. */
function pick(name: string): number {
	let hash = 0;
	for (const character of name) hash = (hash * 31 + character.codePointAt(0)!) % 997;

	return hash % lightTokens.avatars.length;
}

function initials(name: string): string {
	return name
		.split(/\s+/)
		.filter(Boolean)
		.slice(0, 2)
		.map((part) => part[0]!.toUpperCase())
		.join("");
}

export function UserAvatar({ name }: Props) {
	const index = pick(name);

	return (
		<Box
			aria-hidden
			sx={{
				width: 26,
				height: 26,
				borderRadius: "50%",
				display: "grid",
				placeItems: "center",
				flexShrink: 0,
				color: "#FFFFFF",
				fontFamily: fonts.mono,
				fontSize: 11,
				bgcolor: (theme) =>
					theme.palette.mode === "dark"
						? darkTokens.avatars[index]
						: lightTokens.avatars[index],
			}}
		>
			{initials(name)}
		</Box>
	);
}
