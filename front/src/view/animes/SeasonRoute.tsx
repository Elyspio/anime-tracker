import { Navigate, useParams } from "react-router";
import { seasonLabels } from "@/core/binge";
import { homePath, parseSeasonPath } from "@/core/routes";
import { AnimesPage } from "@/view/animes/AnimesPage";
import { useDocumentTitle } from "@/view/components/useDocumentTitle";
import { useLayoutContext } from "@/view/layout/layoutContext";

/** The season the address names, or the current one at `/`. An address that names none goes home. */
export function SeasonRoute() {
	const { year, season } = useParams();
	const layout = useLayoutContext();

	const named = year !== undefined || season !== undefined;
	const valid = !named || parseSeasonPath(year, season) !== null;

	useDocumentTitle(named && valid ? `${seasonLabels[layout.season.season]} ${layout.season.year}` : null);

	if (!valid) return <Navigate to={homePath} replace />;

	return <AnimesPage year={layout.season.year} season={layout.season.season} />;
}
