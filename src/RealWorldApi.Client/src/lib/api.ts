import { type } from "arktype";
import ky, {
	type BeforeRequestState,
	HTTPError,
	type KyInstance,
	type Options,
} from "ky";
import { keys, time } from "~/lib/constants.ts";
import { ProblemDetails, ValidationProblemDetails } from "~/lib/validation.ts";

let isRefreshing = false;

/**
 * @doc Token hook: attaches the access token to outgoing requests
 * - If token exists in localStorage, adds Authorization header
 * - If no token, request proceeds without auth header
 */
function tokenHook({ request }: BeforeRequestState) {
	const accessToken = localStorage.getItem(keys.LocalStorage.AccessToken);
	if (accessToken) {
		request.headers.set("Authorization", `Bearer ${accessToken}`);
	}
}

function csrfHook({ request }: BeforeRequestState) {
	const csrfToken = localStorage.getItem(keys.LocalStorage.CsrfToken);
	if (csrfToken) {
		request.headers.set("X-CSRF-Token", csrfToken);
	}
}

/**
 * @doc Refresh hook: proactively refreshes tokens before expiry
 * - Skips refresh endpoint to avoid infinite loops
 * - If no expiry tracked or token already expired, lets request proceed
 * - If within 5 minutes of expiry, attempts refresh and updates stored credentials
 * - On refresh failure, continues with old token (backend will reject if invalid)
 */
async function refreshHook({ request }: BeforeRequestState) {
	if (request.url.includes("/api/v1/users/refresh")) {
		return;
	}

	const expiresAt = localStorage.getItem(keys.LocalStorage.TokenExpiresAt);
	if (!expiresAt) return;

	const expiresAtMs = Number(expiresAt);
	const timeUntilExpiry = expiresAtMs - Date.now();

	if (timeUntilExpiry <= 0) return;

	if (timeUntilExpiry <= 5 * time.Minute && !isRefreshing) {
		isRefreshing = true;

		try {
			const data = await ky
				.post("/api/v1/users/refresh", {
					headers: {
						Authorization: `Bearer ${localStorage.getItem(keys.LocalStorage.AccessToken)}`,
					},
				})
				.json<{ token: string }>();

			localStorage.setItem(keys.LocalStorage.AccessToken, data.token);
			localStorage.setItem(
				keys.LocalStorage.TokenExpiresAt,
				String(Date.now() + 15 * time.Minute),
			);
			request.headers.set("Authorization", `Bearer ${data.token}`);
		} catch (error) {
			// On refresh failure, continue with old token (backend will reject if invalid)
			void error;
		} finally {
			isRefreshing = false;
		}
	}
}

/**
 * @doc Redirect hook: handles expired token responses (440)
 * - Only handles status 440 (custom code for expired JWT from backend)
 * - Clears all auth data from localStorage and redirects to login
 * - Route protection for 401 is handled by router beforeLoad guards
 */
async function _redirectHook(
	_request: Request,
	_options: Options,
	response: Response,
) {
	if (response.status === 440) {
		localStorage.removeItem(keys.LocalStorage.AccessToken);
		localStorage.removeItem(keys.LocalStorage.TokenExpiresAt);
		localStorage.removeItem(keys.LocalStorage.CsrfToken);
		window.location.href = "/login";
	}
}

/**
 * @doc Error transformer: normalizes API errors into user-friendly messages
 * - Extracts message from various Validation problem details and problem details response formats
 * - Falls back to generic message if format is unrecognized
 */
async function errorTransformer(error: unknown) {
	if (error instanceof HTTPError) {
		const body = await error.response.json().catch(() => ({}));
		const message = type
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
				({ detail, title }) => `${title ? `${title}: ` : ""}${detail ?? ""}`,
			)
			.default(() => "An error occured, please try again later")(body);

		throw new Error(message);
	}
	throw error;
}

const api: KyInstance = ky.create({
	hooks: {
		beforeRequest: [tokenHook, refreshHook, csrfHook],
		// afterResponse: [redirectHook],
		beforeError: [
			async ({ error }) => {
				await errorTransformer(error).catch((transformed) => {
					// Re-attach the normalized message onto the HTTPError
					// so callers still receive an Error with `.message`
					error.message = (transformed as Error).message;
				});
				return error;
			},
		],
	},
});

export { api };
