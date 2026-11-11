import { useEffect, useRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { http } from "./client";
import { isRunActive } from "@/core/refreshRuns";
import type { Anime, AnimeSeason, DubCase, RefreshRun, StudioFilmography } from "./types";

/** Centralised query keys, so mutations can invalidate precisely. */
export const qk = {
	animes: (year: number, season: AnimeSeason) => ["animes", year, season] as const,
	refreshRuns: () => ["refresh-runs"] as const,
	dubCases: (year: number, season: AnimeSeason) => ["dub-cases", year, season] as const,
	studio: (studioId: number) => ["studio", studioId] as const,
	studioOfAnime: (sourceId: number) => ["studio-of-anime", sourceId] as const,
};

export function useAnimes(year: number, season: AnimeSeason) {
	return useQuery({
		queryKey: qk.animes(year, season),
		queryFn: async () => (await http.get<Anime[]>("/api/animes", { params: { year, season } })).data,
		// A season's schedule changes at most once a day, when the refresh job runs.
		staleTime: 5 * 60_000,
	});
}

/**
 * A studio's filmography. The API serves it from its own store, and nothing changes it there but an
 * admin's refresh — which writes the answer straight into this cache — so it never goes stale on its
 * own. The first read of a studio is the slow one: the API fetches it from AniList before answering.
 */
export function useStudio(studioId: number | null) {
	return useQuery({
		queryKey: qk.studio(studioId ?? 0),
		queryFn: async () => (await http.get<StudioFilmography>(`/api/studios/${studioId}`)).data,
		enabled: studioId !== null,
		staleTime: Infinity,
		// A 404 is an answer: asking again would only ask AniList again.
		retry: false,
	});
}

/**
 * The filmography of the studio an anime credits, for an anime stored without its studio's id. The
 * API names the studio and keeps it on the anime, so the answer also goes into the studio's own cache
 * entry — the page moves to the studio's address without asking twice — and the seasons are marked
 * stale, since one of their animes just gained an id.
 */
export function useStudioOfAnime(sourceId: number | null) {
	const queryClient = useQueryClient();

	return useQuery({
		queryKey: qk.studioOfAnime(sourceId ?? 0),
		queryFn: async () => {
			const studio = (await http.get<StudioFilmography>(`/api/animes/${sourceId}/studio`)).data;

			queryClient.setQueryData(qk.studio(studio.studioId), studio);
			void queryClient.invalidateQueries({ queryKey: ["animes"] });

			return studio;
		},
		enabled: sourceId !== null,
		staleTime: Infinity,
		retry: false,
	});
}

/**
 * The season's dub matches worth an admin's look. Public like the season, but only the admin drawer
 * reads it, so it is only fetched while that drawer is open.
 */
export function useDubCases(year: number, season: AnimeSeason, enabled: boolean) {
	return useQuery({
		queryKey: qk.dubCases(year, season),
		queryFn: async () => (await http.get<DubCase[]>("/api/dubs/cases", { params: { year, season } })).data,
		enabled,
	});
}

/** A refresh lands in about a second; this is only about noticing that it has. */
const runPollInterval = 5_000;

/**
 * Recent refresh runs, newest first. Polls only while a run is actually in flight —
 * an idle dashboard should cost nothing.
 */
export function useRefreshRuns() {
	return useQuery({
		queryKey: qk.refreshRuns(),
		queryFn: async () => (await http.get<RefreshRun[]>("/api/animes/refreshes")).data,
		refetchInterval: (query) => ((query.state.data ?? []).some(isRunActive) ? runPollInterval : false),
		staleTime: 0,
	});
}

/**
 * Refetches a season once the run refreshing it stops. This is what makes the grid fill in on its
 * own: the 202 that queued the job carried no new data, and the fetch lands after it.
 */
export function useSeasonRefetchOnRunCompletion(runs: RefreshRun[] | undefined) {
	const queryClient = useQueryClient();
	const active = useRef(new Map<string, RefreshRun>());

	useEffect(() => {
		if (!runs) return;

		const previous = active.current;
		const current = new Map<string, RefreshRun>();

		for (const run of runs) {
			if (isRunActive(run)) current.set(run.runId, run);
			// Interrupted runs are worth refetching too: they may have written before dying.
			else if (previous.has(run.runId)) {
				void queryClient.invalidateQueries({
					queryKey: qk.animes(run.date.year, run.date.season),
				});
				// A dub sync that just finished changed which matches need a look.
				void queryClient.invalidateQueries({
					queryKey: qk.dubCases(run.date.year, run.date.season),
				});
			}
		}

		active.current = current;
	}, [runs, queryClient]);
}
