import { describe, expect, it } from "vitest";
import {
	collectTags,
	currentSeason,
	daysUntil,
	formatBingeChip,
	matchesStatus,
	matchesTags,
} from "./binge";
import type { Anime, BingePrediction } from "@/core/api/types";

const now = new Date(2026, 1, 1); // 1 February 2026

function prediction(overrides: Partial<BingePrediction> = {}): BingePrediction {
	return {
		status: "Estimated",
		bingeableAt: "2026-03-22",
		releasedEpisodes: 4,
		totalEpisodes: 12,
		...overrides,
	};
}

function anime(overrides: Partial<Anime> = {}): Anime {
	return {
		id: "1",
		date: { year: 2026, season: "Winter" },
		title: "Titre",
		studio: "Studio",
		description: "",
		imageUrl: "",
		url: "",
		score: 8,
		popularity: 100,
		episodesCount: 12,
		episodes: [],
		tags: [],
		binge: prediction(),
		...overrides,
	};
}

describe("daysUntil", () => {
	it("counts whole days from today", () => {
		expect(daysUntil("2026-02-08", now)).toBe(7);
	});

	it("is zero for today itself", () => {
		expect(daysUntil("2026-02-01", now)).toBe(0);
	});

	it("is negative for a past date", () => {
		expect(daysUntil("2026-01-25", now)).toBe(-7);
	});
});

describe("formatBingeChip", () => {
	it("shows weeks for anything more than a week out", () => {
		// 2026-03-22 is 49 days away — seven whole weeks.
		expect(formatBingeChip(prediction(), now).label).toBe("7 sem.");
	});

	it("shows days inside the last week, where the exact wait matters", () => {
		expect(formatBingeChip(prediction({ bingeableAt: "2026-02-04" }), now).label).toBe("3 j");
	});

	it("marks a finished season as bingeable, in the reserved success tone", () => {
		const chip = formatBingeChip(
			prediction({ status: "BingeableNow", bingeableAt: "2026-01-20" }),
			now,
		);

		expect(chip.label).toBe("Bingeable");
		expect(chip.tone).toBe("success");
	});

	it("treats an estimate that has come due as bingeable", () => {
		expect(formatBingeChip(prediction({ bingeableAt: "2026-01-30" }), now).label).toBe(
			"Bingeable",
		);
	});

	it("says the end is unknown rather than inventing a number", () => {
		const chip = formatBingeChip(
			prediction({ status: "UnknownEnd", bingeableAt: null, totalEpisodes: null }),
			now,
		);

		expect(chip.label).toBe("Fin inconnue");
		expect(chip.tone).toBe("default");
	});
});

describe("matchesStatus", () => {
	const bingeable = anime({ binge: prediction({ status: "BingeableNow" }) });
	const soon = anime({ binge: prediction({ bingeableAt: "2026-02-20" }) });
	const later = anime({ binge: prediction({ bingeableAt: "2026-06-01" }) });
	const unknown = anime({ binge: prediction({ status: "UnknownEnd", bingeableAt: null }) });

	it("keeps everything on 'all'", () => {
		expect(
			[bingeable, soon, later, unknown].every((item) => matchesStatus(item, "all", now)),
		).toBe(true);
	});

	it("selects only finished seasons on 'bingeable'", () => {
		expect(matchesStatus(bingeable, "bingeable", now)).toBe(true);
		expect(matchesStatus(soon, "bingeable", now)).toBe(false);
	});

	it("selects estimates within the month on 'soon', and excludes distant ones", () => {
		expect(matchesStatus(soon, "soon", now)).toBe(true);
		expect(matchesStatus(later, "soon", now)).toBe(false);
		expect(matchesStatus(unknown, "soon", now)).toBe(false);
	});
});

describe("matchesTags", () => {
	const shonen = anime({
		tags: [
			{ name: "Action", url: "" },
			{ name: "Shōnen", url: "" },
		],
	});

	it("keeps everything when no tag is selected", () => {
		expect(matchesTags(shonen, [])).toBe(true);
	});

	it("requires every selected tag, not just one", () => {
		expect(matchesTags(shonen, ["Action"])).toBe(true);
		expect(matchesTags(shonen, ["Action", "Shōnen"])).toBe(true);
		expect(matchesTags(shonen, ["Action", "Romance"])).toBe(false);
	});
});

describe("collectTags", () => {
	it("deduplicates and sorts the tags of the loaded season", () => {
		const animes = [
			anime({
				tags: [
					{ name: "Shōnen", url: "" },
					{ name: "Action", url: "" },
				],
			}),
			anime({
				tags: [
					{ name: "Action", url: "" },
					{ name: "Comédie", url: "" },
				],
			}),
		];

		expect(collectTags(animes)).toEqual(["Action", "Comédie", "Shōnen"]);
	});
});

describe("currentSeason", () => {
	it("maps a month to its broadcast season", () => {
		expect(currentSeason(new Date(2026, 0, 15)).season).toBe("Winter");
		expect(currentSeason(new Date(2026, 4, 15)).season).toBe("Spring");
		expect(currentSeason(new Date(2026, 7, 15)).season).toBe("Summer");
		expect(currentSeason(new Date(2026, 11, 15)).season).toBe("Fall");
	});

	it("keeps December in the autumn of its own year, matching the backend", () => {
		expect(currentSeason(new Date(2026, 11, 31))).toEqual({ year: 2026, season: "Fall" });
	});
});
