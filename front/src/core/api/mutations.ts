import { useMutation, useQueryClient } from "@tanstack/react-query";
import { http } from "./client";
import { qk } from "./queries";
import type { AnimeSeason, RefreshQueued } from "./types";

/**
 * Queues a full re-scrape of a season. The API answers 202 as soon as the job is on the queue —
 * the walk itself takes tens of minutes — so success here means "started", not "done".
 * Rejected with 403 for anyone without the admin role.
 */
export function useRefreshSeason() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: async ({ year, season }: { year: number; season: AnimeSeason }) => {
			const { data } = await http.post<RefreshQueued>("/api/animes/refresh", null, {
				params: { year, season },
			});
			return data;
		},
		onSuccess: (_data, { year, season }) => {
			void queryClient.invalidateQueries({ queryKey: qk.animes(year, season) });
		},
	});
}
