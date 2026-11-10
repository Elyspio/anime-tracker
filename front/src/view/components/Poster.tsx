import { Box, type SxProps, type Theme } from "@mui/material";
import { useState } from "react";

interface Props {
	src: string;
	sx?: SxProps<Theme>;
}

/**
 * A cover at 2:3, or the well it would have sat in. A source entry with no cover stores an empty
 * string, and `<img src="">` resolves to the current document — a broken image behind a src that
 * looks fine — so the fallback is explicit, and a cover that fails to load gets it too.
 */
export function Poster({ src, sx }: Props) {
	const [failed, setFailed] = useState(false);

	return (
		<Box sx={[{ aspectRatio: "2 / 3", overflow: "hidden", bgcolor: "action.hover" }, ...(Array.isArray(sx) ? sx : [sx])]}>
			{src !== "" && !failed && (
				<Box
					component="img"
					src={src}
					alt=""
					loading="lazy"
					decoding="async"
					onError={() => setFailed(true)}
					sx={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
				/>
			)}
		</Box>
	);
}
