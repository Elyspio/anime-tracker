import { describe, expect, it } from "vitest";
import { captureReturnTo, resolveReturnTo } from "@/core/auth/returnTo";

describe("captureReturnTo", () => {
	it("keeps the path, query and hash of the page the user left", () => {
		expect(
			captureReturnTo({ pathname: "/", search: "?season=Summer2026", hash: "#top" }),
		).toBe("/?season=Summer2026#top");
	});

	it("does not capture the callback route itself", () => {
		expect(captureReturnTo({ pathname: "/login/callback", search: "", hash: "" })).toBe("/");
	});
});

describe("resolveReturnTo", () => {
	it("restores a captured path", () => {
		expect(resolveReturnTo({ returnTo: "/?season=Summer2026" })).toBe("/?season=Summer2026");
	});

	it("falls back to the root when no state came back", () => {
		expect(resolveReturnTo(undefined)).toBe("/");
		expect(resolveReturnTo({})).toBe("/");
		expect(resolveReturnTo("/somewhere")).toBe("/");
	});

	it("refuses a value that would leave the origin", () => {
		expect(resolveReturnTo({ returnTo: "https://evil.example/" })).toBe("/");
		expect(resolveReturnTo({ returnTo: "//evil.example/" })).toBe("/");
	});

	it("refuses to land back on the callback route", () => {
		expect(resolveReturnTo({ returnTo: "/login/callback?code=abc" })).toBe("/");
	});
});
