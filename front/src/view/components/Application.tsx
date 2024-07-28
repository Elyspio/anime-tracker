import * as React from "react";
import { useEffect } from "react";
import { useAppDispatch } from "@store";
import { Stack } from "@mui/material";
import { initApp } from "@store/module/workflow/workflow.async.actions";
import { Animes } from "@components/animes/Animes";
import { Appbar } from "@components/utils/appbar/Appbar";

function Application() {
	const dispatch = useAppDispatch();

	useEffect(() => {
		dispatch(initApp());
	}, [dispatch]);

	return (
		<Stack alignItems={"center"} justifyContent={"center"} spacing={2} height={"100vh"} width={"100vw"} bgcolor={"background.default"}>
			<Appbar appName={"Animes"} />
			<Animes />
		</Stack>
	);
}

export default Application;
