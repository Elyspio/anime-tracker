import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SnackbarProvider } from "notistack";
import "@fontsource-variable/space-grotesk";

import { ThemeModeProvider } from "@/config/themeMode";
import { AppAuthProvider } from "@/core/auth/AuthProvider";
import { AppLayout } from "@/view/layout/AppLayout";

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
			<SnackbarProvider
				maxSnack={3}
				anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
			>
				<QueryClientProvider client={queryClient}>
					<AppAuthProvider>
						<AppLayout />
					</AppAuthProvider>
				</QueryClientProvider>
			</SnackbarProvider>
		</ThemeModeProvider>
	</StrictMode>,
);
