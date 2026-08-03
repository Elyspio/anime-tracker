import { describe, expect, it } from "vitest";
import { findRunFor, formatDuration, isRunActive } from "@/core/refreshRuns";
import type { RefreshRun, RefreshStatus } from "@/core/api/types";

function run(overrides: Partial<RefreshRun> = {}): RefreshRun {
	return {
		id: "1",
		runId: "run-1",
		date: { year: 2026, season: "Summer" },
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
		expect(formatDuration(run({ finishedAt: "2026-08-03T12:00:00.200Z" }))).toBe(
			"under a second",
		);
	});
});
