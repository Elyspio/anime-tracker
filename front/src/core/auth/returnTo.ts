/** Shape of the app state handed to the identity provider and returned by the callback. */
export interface SigninState {
	returnTo: string;
}

const callbackPath = "/login/callback";

/** Captures where the user was before the redirect, so the callback can send them back. */
export function captureReturnTo(location: Pick<Location, "pathname" | "search" | "hash">): string {
	const path = `${location.pathname}${location.search}${location.hash}`;
	return path.startsWith(callbackPath) ? "/" : path;
}

/**
 * Reads back what `captureReturnTo` stored. The value round-trips through the identity provider,
 * so it is treated as untrusted: only a same-origin, non-protocol-relative path is honoured, and
 * the callback route itself never is — landing back on it is the bug this exists to fix.
 */
export function resolveReturnTo(state: unknown): string {
	const candidate =
		typeof state === "object" && state !== null
			? (state as Partial<SigninState>).returnTo
			: undefined;

	if (typeof candidate !== "string") return "/";
	if (!candidate.startsWith("/") || candidate.startsWith("//")) return "/";
	if (candidate.startsWith(callbackPath)) return "/";

	return candidate;
}
