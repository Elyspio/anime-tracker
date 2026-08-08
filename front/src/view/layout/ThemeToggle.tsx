import { useState, type MouseEvent, type ReactNode } from "react";
import { IconButton, ListItemIcon, ListItemText, Menu, MenuItem, Tooltip } from "@mui/material";
import CheckIcon from "@mui/icons-material/Check";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import ContrastIcon from "@mui/icons-material/Contrast";
import { useThemeMode, type ThemeMode } from "@/config/themeMode";

const options: { mode: ThemeMode; label: string; icon: ReactNode }[] = [
	{ mode: "system", label: "System", icon: <ContrastIcon fontSize="small" /> },
	{ mode: "light", label: "Light", icon: <LightModeIcon fontSize="small" /> },
	{ mode: "dark", label: "Dark", icon: <DarkModeIcon fontSize="small" /> },
];

export function ThemeToggle() {
	const { mode, setMode } = useThemeMode();
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	const current = options.find((option) => option.mode === mode) ?? options[0];

	const open = (event: MouseEvent<HTMLElement>) => setAnchor(event.currentTarget);
	const close = () => setAnchor(null);
	const choose = (next: ThemeMode) => {
		setMode(next);
		close();
	};

	return (
		<>
			<Tooltip title="Theme">
				<IconButton size="small" onClick={open} aria-label="Change theme">
					{current.icon}
				</IconButton>
			</Tooltip>
			<Menu anchorEl={anchor} open={Boolean(anchor)} onClose={close}>
				{options.map((option) => (
					<MenuItem
						key={option.mode}
						selected={option.mode === mode}
						onClick={() => choose(option.mode)}
					>
						<ListItemIcon>{option.icon}</ListItemIcon>
						<ListItemText>{option.label}</ListItemText>
						{option.mode === mode && <CheckIcon fontSize="small" sx={{ ml: 3 }} />}
					</MenuItem>
				))}
			</Menu>
		</>
	);
}
