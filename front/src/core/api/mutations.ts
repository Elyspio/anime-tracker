import { useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { http } from "./client";
import { qk } from "./queries";
import type { AnimeSeason, RefreshRun } from "./types";

/** What the refresh button gets back: the run to follow, and whether it is the one it just started. */
export interface RefreshOutcome {
	alreadyRunning: boolean;
	run: RefreshRun;
}

/**
 * Queues a refresh of a season. The API answers 202 as soon as the job is on the queue, so success
 * here means "started", not "done" — the fetch lands a moment later and the run reports it.
 * A season already being refreshed comes back as 409 carrying that run, which is an answer rather
 * than a failure: the caller follows it instead of queuing a duplicate.
 * Rejected with 403 for anyone without the admin role.
 */
export function useRefreshSeason() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: async ({
			year,
			season,
		}: {
			year: number;
			season: AnimeSeason;
		}): Promise<RefreshOutcome> => {
			try {
				const { data } = await http.post<RefreshRun>("/api/animes/refresh", null, {
					params: { year, season },
				});
				return { alreadyRunning: false, run: data };
			} catch (error) {
				if (
					isAxiosError<RefreshRun>(error) &&
					error.response?.status === 409 &&
					error.response.data
				) {
					return { alreadyRunning: true, run: error.response.data };
				}
				throw error;
			}
		},
		onSuccess: () => {
			// Not the season: at 202 the fetch has not run, so the data on screen is still the
			// freshest there is. The season is invalidated when the run reports it finished.
			void queryClient.invalidateQueries({ queryKey: qk.refreshRuns() });
		},
	});
}
