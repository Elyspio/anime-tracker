import { useQuery } from "@tanstack/react-query";
import { http } from "./client";
import type { Anime, AnimeSeason } from "./types";

/** Centralised query keys, so mutations can invalidate precisely. */
export const qk = {
	animes: (year: number, season: AnimeSeason) => ["animes", year, season] as const,
};

export function useAnimes(year: number, season: AnimeSeason) {
	return useQuery({
		queryKey: qk.animes(year, season),
		queryFn: async () =>
			(await http.get<Anime[]>("/api/animes", { params: { year, season } })).data,
		// A season's schedule changes at most once a day, when the refresh job runs.
		staleTime: 5 * 60_000,
	});
}
