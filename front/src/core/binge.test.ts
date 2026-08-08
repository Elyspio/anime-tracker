import { describe, expect, it } from "vitest";
import {
	collectGenres,
	currentSeason,
	daysUntil,
	formatBingeChip,
	matchesGenres,
	matchesStatus,
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
		sourceId: 1,
		date: { year: 2026, season: "Winter" },
		title: "Title",
		description: "",
		studio: "Studio",
		imageUrl: "",
		url: "",
		format: "Tv",
		isAdult: false,
		score: 8,
		popularity: 100,
		votesCount: 100,
		episodesCount: 12,
		genres: [],
		episodes: [],
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
		expect(formatBingeChip(prediction(), now).label).toBe("Binge in 7 weeks");
	});

	it("shows days inside the last week, where the exact wait matters", () => {
		expect(formatBingeChip(prediction({ bingeableAt: "2026-02-04" }), now).label).toBe(
			"Binge in 3 days",
		);
	});

	it("keeps the unit singular when only one is left", () => {
		expect(formatBingeChip(prediction({ bingeableAt: "2026-02-02" }), now).label).toBe(
			"Binge in 1 day",
		);
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

		expect(chip.label).toBe("No end announced");
		expect(chip.tone).toBe("default");
	});

	it("says an announced date is scheduled, not estimated", () => {
		// The whole point of the Announced status: the reader can tell a broadcaster's date from
		// one this app worked out.
		const announced = formatBingeChip(prediction({ status: "Announced" }), now);
		const estimated = formatBingeChip(prediction({ status: "Estimated" }), now);

		expect(announced.label).toBe("Binge in 7 weeks");
		expect(announced.title).toContain("scheduled");
		expect(estimated.title).toContain("estimated");
	});
});

describe("matchesStatus", () => {
	const bingeable = anime({ binge: prediction({ status: "BingeableNow" }) });
	const soon = anime({ binge: prediction({ bingeableAt: "2026-02-20" }) });
	const announcedSoon = anime({
		binge: prediction({ status: "Announced", bingeableAt: "2026-02-20" }),
	});
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

	it("selects anything ending within the month on 'soon', announced or estimated", () => {
		expect(matchesStatus(soon, "soon", now)).toBe(true);
		expect(matchesStatus(announcedSoon, "soon", now)).toBe(true);
		expect(matchesStatus(later, "soon", now)).toBe(false);
		expect(matchesStatus(unknown, "soon", now)).toBe(false);
	});
});

describe("matchesGenres", () => {
	const shonen = anime({ genres: ["Action", "Adventure"] });

	it("keeps everything when no genre is selected", () => {
		expect(matchesGenres(shonen, [])).toBe(true);
	});

	it("requires every selected genre, not just one", () => {
		expect(matchesGenres(shonen, ["Action"])).toBe(true);
		expect(matchesGenres(shonen, ["Action", "Adventure"])).toBe(true);
		expect(matchesGenres(shonen, ["Action", "Romance"])).toBe(false);
	});
});

describe("collectGenres", () => {
	it("deduplicates and sorts the genres of the loaded season", () => {
		const animes = [
			anime({ genres: ["Adventure", "Action"] }),
			anime({ genres: ["Action", "Comedy"] }),
		];

		expect(collectGenres(animes)).toEqual(["Action", "Adventure", "Comedy"]);
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
