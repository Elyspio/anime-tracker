import { Chip } from "@mui/material";
import type { ReactNode } from "react";
import { fonts } from "@/config/tokens";

interface Props {
	label: ReactNode;
	selected: boolean;
	onClick: () => void;
}

/**
 * One of a row of mutually exclusive choices. The selected one is solid ink — never the emerald,
 * which means "bingeable" and nothing else.
 */
export function ChoiceChip({ label, selected, onClick }: Props) {
	return (
		<Chip
			label={label}
			onClick={onClick}
			aria-pressed={selected}
			sx={{
				height: 30,
				fontFamily: fonts.sans,
				fontSize: 13,
				bgcolor: selected ? "text.primary" : "background.paper",
				color: selected ? "background.paper" : "text.secondary",
				borderColor: selected ? "text.primary" : "divider",
				"&:hover": {
					bgcolor: selected ? "text.primary" : "background.paper",
					borderColor: selected ? "text.primary" : "text.disabled",
					color: selected ? "background.paper" : "text.primary",
				},
			}}
		/>
	);
}
