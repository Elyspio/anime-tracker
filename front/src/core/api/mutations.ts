import { useMutation, useQueryClient } from "@tanstack/react-query";
import { http } from "./client";
import { qk } from "./queries";
import type { AnimeSeason } from "./types";

/**
 * Triggers a full re-scrape of a season. Slow on purpose — the backend walks Nautiljon one page
 * at a time — and rejected with a 403 for anyone without the admin role.
 */
export function useRefreshSeason() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: async ({ year, season }: { year: number; season: AnimeSeason }) => {
			await http.post("/api/animes/refresh", null, { params: { year, season } });
		},
		onSuccess: (_data, { year, season }) => {
			void queryClient.invalidateQueries({ queryKey: qk.animes(year, season) });
		},
	});
}
