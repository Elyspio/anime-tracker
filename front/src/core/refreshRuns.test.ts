import { describe, expect, it } from "vite-plus/test";
import { findRunFor, formatDuration, groupPasses, isRunActive } from "@/core/refreshRuns";
import type { RefreshRun, RefreshStatus } from "@/core/api/types";

function run(overrides: Partial<RefreshRun> = {}): RefreshRun {
	return {
		id: "1",
		runId: "run-1",
		date: { year: 2026, season: "Summer" },
		kind: "Season",
		status: "Running",
		total: 0,
		startedAt: "2026-08-03T12:00:00Z",
		updatedAt: "2026-08-03T12:00:02Z",
		finishedAt: null,
		error: null,
		...overrides,
	};
}

describe("isRunActive", () => {
	it.each<[RefreshStatus, boolean]>([
		["Queued", true],
		["Running", true],
		["Succeeded", false],
		["Failed", false],
		["Interrupted", false],
	])("treats %s as active=%s", (status, expected) => {
		expect(isRunActive(run({ status }))).toBe(expected);
	});
});

describe("findRunFor", () => {
	it("finds the run refreshing the season being looked at", () => {
		const other = run({ runId: "other", date: { year: 2026, season: "Fall" } });

		expect(findRunFor([other, run()], 2026, "Summer")?.runId).toBe("run-1");
	});

	it("ignores the season's dub sync", () => {
		expect(findRunFor([run({ kind: "Dub" })], 2026, "Summer")).toBeUndefined();
	});

	it("ignores a run that has already stopped", () => {
		expect(findRunFor([run({ status: "Succeeded" })], 2026, "Summer")).toBeUndefined();
	});

	it("copes with runs it has not loaded yet", () => {
		expect(findRunFor(undefined, 2026, "Summer")).toBeUndefined();
	});
});

describe("formatDuration", () => {
	it("reports a normal refresh in seconds", () => {
		// A single fetch lands in about a second; rounded to minutes it would always read as zero.
		expect(formatDuration(run({ finishedAt: "2026-08-03T12:00:02Z" }))).toBe("2s");
	});

	it("measures a running run up to its last sign of life", () => {
		expect(formatDuration(run({ updatedAt: "2026-08-03T12:00:09Z" }))).toBe("9s");
	});

	it("switches to minutes once a run drags", () => {
		expect(formatDuration(run({ finishedAt: "2026-08-03T12:03:00Z" }))).toBe("3 min");
	});

	it("does not round a very fast run down to zero", () => {
		expect(formatDuration(run({ finishedAt: "2026-08-03T12:00:00.200Z" }))).toBe("under a second");
	});
});

describe("groupPasses", () => {
	const season = (id: string, startedAt: string, year = 2026) => run({ runId: id, kind: "Season", startedAt, date: { year, season: "Summer" } });
	const dub = (id: string, startedAt: string, year = 2026) => run({ runId: id, kind: "Dub", startedAt, date: { year, season: "Summer" } });

	it("pairs a dub sync with the refresh that queued it", () => {
		const passes = groupPasses([dub("d1", "2026-08-03T12:00:03Z"), season("s1", "2026-08-03T12:00:00Z")]);

		expect(passes.map((pass) => [pass.season?.runId, pass.dub?.runId])).toEqual([["s1", "d1"]]);
	});

	it("leaves a refresh that queued nothing on its own", () => {
		// A second refresh while the first one's dub sync still runs queues no dub of its own.
		const passes = groupPasses([season("s2", "2026-08-03T12:01:00Z"), dub("d1", "2026-08-03T12:00:03Z"), season("s1", "2026-08-03T12:00:00Z")]);

		expect(passes.map((pass) => [pass.season?.runId, pass.dub?.runId])).toEqual([
			["s2", undefined],
			["s1", "d1"],
		]);
	});

	it("never pairs runs of different seasons", () => {
		const passes = groupPasses([dub("d1", "2026-08-03T12:00:03Z", 2027), season("s1", "2026-08-03T12:00:00Z")]);

		expect(passes.map((pass) => [pass.season?.runId, pass.dub?.runId])).toEqual([
			[undefined, "d1"],
			["s1", undefined],
		]);
	});

	it("keeps a dub sync whose refresh fell past the page", () => {
		expect(groupPasses([dub("d1", "2026-08-03T12:00:03Z")]).map((pass) => pass.dub?.runId)).toEqual(["d1"]);
	});
});
