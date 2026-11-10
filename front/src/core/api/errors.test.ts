import { AxiosError, type AxiosResponse } from "axios";
import { describe, expect, it } from "vite-plus/test";
import { apiErrorMessage } from "@/core/api/errors";

function refusal(status: number, data: unknown): AxiosError {
	return new AxiosError(`Request failed with status code ${status}`, "ERR_BAD_REQUEST", undefined, undefined, { status, data } as AxiosResponse);
}

describe("apiErrorMessage", () => {
	it("prefers what the API said over what axios says", () => {
		expect(apiErrorMessage(refusal(404, { status: 404, title: "No studio is known with id 3." }))).toBe("No studio is known with id 3.");
	});

	it("names a refusal the API left untitled", () => {
		expect(apiErrorMessage(refusal(401, ""))).toBe("Sign in to do this.");
		expect(apiErrorMessage(refusal(403, undefined))).toBe("Your account is not allowed to do this.");
	});

	it("falls back to the error's own message", () => {
		expect(apiErrorMessage(refusal(500, { title: "   " }))).toBe("Request failed with status code 500");
		expect(apiErrorMessage(new Error("Network Error"))).toBe("Network Error");
	});

	it("falls back to the caller's words for something that is not an error", () => {
		expect(apiErrorMessage("boom", "Refresh failed")).toBe("Refresh failed");
		expect(apiErrorMessage(undefined)).toBe("The request failed.");
	});
});
