import { isAxiosError } from "axios";

/**
 * What to tell the reader when a request fails. The API answers a refusal with a ProblemDetails
 * whose `title` says what happened in the product's own words — "No studio is known with id 3" —
 * and that beats axios's "Request failed with status code 404" every time it is there.
 */
export function apiErrorMessage(error: unknown, fallback = "The request failed."): string {
	if (isAxiosError(error)) {
		const title: unknown = (error.response?.data as { title?: unknown } | undefined)?.title;
		if (typeof title === "string" && title.trim() !== "") return title;

		if (error.response?.status === 401) return "Sign in to do this.";
		if (error.response?.status === 403) return "Your account is not allowed to do this.";
	}

	return error instanceof Error && error.message !== "" ? error.message : fallback;
}
