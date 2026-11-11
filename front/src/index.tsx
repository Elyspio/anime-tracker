import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "react-router/dom";
import { SnackbarProvider } from "notistack";
import "@fontsource-variable/geist";
import "@fontsource-variable/geist-mono";

import { ThemeModeProvider } from "@/config/themeMode";
import { AppAuthProvider } from "@/core/auth/AuthProvider";
import { router } from "@/view/router";

const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			retry: 1,
			refetchOnWindowFocus: false,
		},
	},
});

createRoot(document.getElementById("root")!).render(
	<StrictMode>
		<ThemeModeProvider>
			<SnackbarProvider maxSnack={3} anchorOrigin={{ vertical: "bottom", horizontal: "right" }}>
				<QueryClientProvider client={queryClient}>
					<AppAuthProvider onSignedIn={(returnTo) => void router.navigate(returnTo, { replace: true })}>
						<RouterProvider router={router} />
					</AppAuthProvider>
				</QueryClientProvider>
			</SnackbarProvider>
		</ThemeModeProvider>
	</StrictMode>
);
