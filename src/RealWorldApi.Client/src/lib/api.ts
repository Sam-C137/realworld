import { type } from "arktype";
import ky, {
	type AfterResponseState,
	type BeforeRequestState,
	HTTPError,
	type KyInstance,
	type NormalizedOptions,
} from "ky";
import { keys, time } from "~/lib/constants.ts";
import { ProblemDetails, ValidationProblemDetails } from "~/lib/validation.ts";

const refreshPath = "/api/v1/users/refresh";
let refreshPromise: Promise<string> | undefined;

/**
 * Controls how an API request participates in authentication.
 * - `required` (default): sends available credentials and redirects to login if
 *   the refresh session is invalid.
 * - `optional`: sends available credentials, but clears stale auth and continues
 *   anonymously instead of redirecting.
 * - `none`: does not attach stored auth headers or attempt token refresh;
 *   browser-managed cookies still follow normal fetch credential rules.
 */
type AuthMode = "required" | "optional" | "none";

function getAuthMode(options: NormalizedOptions): AuthMode {
	const mode = options.context.authMode;
	return mode === "optional" || mode === "none" ? mode : "required";
}

function parseTokenExpiresAt(expiresAt: string) {
	const parsedMs = Date.parse(expiresAt);
	return Number.isNaN(parsedMs) ? undefined : parsedMs;
}

function clearAuth() {
	localStorage.removeItem(keys.LocalStorage.AccessToken);
	localStorage.removeItem(keys.LocalStorage.TokenExpiresAt);
	localStorage.removeItem(keys.LocalStorage.CsrfToken);
}

function handleInvalidSession(authMode: AuthMode, request?: Request) {
	clearAuth();
	if (authMode === "required") {
		window.location.href = "/login";
		return;
	}

	request?.headers.delete("Authorization");
	request?.headers.delete("X-CSRF-Token");
}

function isInvalidRefreshToken(error: unknown) {
	return error instanceof HTTPError && error.response.status === 401;
}

function refreshAccessToken() {
	refreshPromise ??= ky
		.post(refreshPath)
		.json<{ token: string }>()
		.then(({ token }) => {
			localStorage.setItem(keys.LocalStorage.AccessToken, token);
			localStorage.setItem(
				keys.LocalStorage.TokenExpiresAt,
				new Date(Date.now() + 15 * time.Minute).toISOString(),
			);
			return token;
		})
		.finally(() => {
			refreshPromise = undefined;
		});

	return refreshPromise;
}

/**
 * @doc Token hook: attaches the access token to outgoing requests
 * - If token exists in localStorage, adds Authorization header
 * - If no token, request proceeds without auth header
 */
function tokenHook({ request, options }: BeforeRequestState) {
	if (getAuthMode(options) === "none") return;

	const accessToken = localStorage.getItem(keys.LocalStorage.AccessToken);
	if (accessToken) {
		request.headers.set("Authorization", `Bearer ${accessToken}`);
	}
}

function csrfHook({ request, options }: BeforeRequestState) {
	if (getAuthMode(options) === "none") return;

	const csrfToken = localStorage.getItem(keys.LocalStorage.CsrfToken);
	if (csrfToken) {
		request.headers.set("X-CSRF-Token", csrfToken);
	}
}

/**
 * @doc Refresh hook: proactively refreshes tokens before expiry
 * - Skips refresh endpoint to avoid infinite loops
 * - If within 5 minutes of expiry or already expired, refreshes before the request
 * - Concurrent requests wait for the same refresh operation
 * - Optional requests continue anonymously when the refresh session is invalid
 */
async function refreshHook({ request, options }: BeforeRequestState) {
	const authMode = getAuthMode(options);
	if (authMode === "none" || request.url.includes(refreshPath)) {
		return;
	}
	if (!request.headers.has("Authorization")) return;

	if (refreshPromise) {
		try {
			const token = await refreshPromise;
			request.headers.set("Authorization", `Bearer ${token}`);
		} catch (error) {
			if (isInvalidRefreshToken(error)) {
				handleInvalidSession(authMode, request);
			}
		}
		return;
	}

	const expiresAt = localStorage.getItem(keys.LocalStorage.TokenExpiresAt);
	if (!expiresAt) return;

	const expiresAtMs = parseTokenExpiresAt(expiresAt);
	if (expiresAtMs === undefined) return;

	const timeUntilExpiry = expiresAtMs - Date.now();
	if (timeUntilExpiry <= 5 * time.Minute) {
		try {
			const token = await refreshAccessToken();
			request.headers.set("Authorization", `Bearer ${token}`);
		} catch (error) {
			if (isInvalidRefreshToken(error)) {
				handleInvalidSession(authMode, request);
			}
		}
	}
}

/**
 * @doc Retries an authenticated request once after refreshing its access token
 * - Handles the backend's 401 response and the previously supported 440 response
 * - Requests with auth mode `none` never participate in refresh handling
 * - Optional requests retry anonymously when their refresh session is invalid
 * - Preserves auth on transient refresh failures
 */
async function retryAfterRefresh({
	request,
	options,
	response,
	retryCount,
}: AfterResponseState) {
	const authMode = getAuthMode(options);
	const isExpiredResponse = response.status === 401 || response.status === 440;
	if (
		authMode === "none" ||
		!isExpiredResponse ||
		retryCount > 0 ||
		request.url.includes(refreshPath) ||
		(authMode === "optional" && !request.headers.has("Authorization"))
	) {
		return;
	}

	try {
		const token = await refreshAccessToken();
		const headers = new Headers(request.headers);
		headers.set("Authorization", `Bearer ${token}`);

		return ky.retry({
			request: new Request(request, { headers }),
			delay: 0,
			code: "TOKEN_REFRESHED",
		});
	} catch (error) {
		if (isInvalidRefreshToken(error)) {
			handleInvalidSession(authMode);
			if (authMode === "optional") {
				const headers = new Headers(request.headers);
				headers.delete("Authorization");
				headers.delete("X-CSRF-Token");

				return ky.retry({
					request: new Request(request, { headers }),
					delay: 0,
					code: "ANONYMOUS_FALLBACK",
				});
			}
		}
	}
}

/**
 * @doc Error transformer: normalizes API errors into user-friendly messages
 * - Extracts message from various Validation problem details and problem details response formats
 * - Falls back to generic message if format is unrecognized
 */
async function errorTransformer(error: unknown) {
	if (error instanceof HTTPError) {
		const body = error.data ?? {};
		error.message = type
			.match({})
			.case({ message: "string" }, ({ message }) => message)
			.case(ValidationProblemDetails, ({ errors }) =>
				Object.values(errors)
					.flat()
					.map((msg) => msg.toLowerCase())
					.join(",\n"),
			)
			.case(
				ProblemDetails,
				({ detail, title, status }) =>
					`${title ? `${title}: ` : ""}${detail ?? ""} status: ${status}`,
			)
			.default(() => "An error occurred, please try again later")(body);
		return;
	}
}

const api: KyInstance = ky.create({
	hooks: {
		beforeRequest: [tokenHook, refreshHook, csrfHook],
		afterResponse: [retryAfterRefresh],
		beforeError: [
			async ({ error }) => {
				await errorTransformer(error);
				return error;
			},
		],
	},
});

export { api, clearAuth };
