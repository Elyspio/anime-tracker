import { createBrowserRouter, Navigate } from "react-router";
import { homePath } from "@/core/routes";
import { SeasonRoute } from "@/view/animes/SeasonRoute";
import { AppLayout } from "@/view/layout/AppLayout";
import { StudioPage } from "@/view/studio/StudioPage";

/**
 * Three places: the current season, a season by name, a studio. The season is in the address so a
 * link to "autumn 2026" means that season next year too, while `/` always means "now".
 */
export const router = createBrowserRouter([
	{
		element: <AppLayout />,
		children: [
			{ index: true, element: <SeasonRoute /> },
			// Where the identity provider sends the reader back to. It has no view of its own: the
			// current season is shown for the instant it takes the sign-in to restore the real path.
			// It must not redirect — the one-time code is still in the query string at that point.
			{ path: "login/callback", element: <SeasonRoute /> },
			{ path: ":year/:season", element: <SeasonRoute /> },
			{ path: "studios/:studioId", element: <StudioPage /> },
			// An anime stored without its studio's id: the page names the studio, then moves to it.
			{ path: "animes/:sourceId/studio", element: <StudioPage /> },
			{ path: "*", element: <Navigate to={homePath} replace /> },
		],
	},
]);
