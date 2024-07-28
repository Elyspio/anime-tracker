import "./Appbar.scss";
import React from "react";
import { AppBar, Toolbar, Typography } from "@mui/material";

interface Props {
	appName: string;
}

export function Appbar(props: Props) {
	return (
		<AppBar position="static">
			<Toolbar>
				<Typography variant="h6">{props.appName}</Typography>
			</Toolbar>
		</AppBar>
	);
}
