import type {
	DefaultError,
	MutationOptions,
	QueryOptions,
} from "@tanstack/solid-query";
import { api, clearAuth } from "~/lib/api.ts";
import { keys, time } from "~/lib/constants.ts";
import type { LoginSchema } from "~/lib/validation.ts";
import type { User } from "~/types/user.ts";

export const LoginOptions: MutationOptions<
	Record<"user", User>,
	DefaultError,
	Record<"user", typeof LoginSchema.inferIn>
> = {
	onMutate: () => {
		localStorage.removeItem(keys.LocalStorage.AccessToken);
	},
	mutationFn: async (data) => {
		return api
			.post<Record<"user", User>>("/api/v1/users/login", {
				context: { authMode: "none" },
				json: data,
			})
			.json();
	},
	onSuccess: ({ user }) => {
		localStorage.setItem(keys.LocalStorage.AccessToken, user.token);
		localStorage.setItem(
			keys.LocalStorage.CsrfToken,
			user.csrfToken ?? "<nil>",
		);
		localStorage.setItem(
			keys.LocalStorage.TokenExpiresAt,
			new Date(Date.now() + 15 * time.Minute).toISOString(),
		);
	},
};

export const RegisterOptions: MutationOptions<
	Record<"user", User>,
	DefaultError,
	Record<"user", typeof LoginSchema.inferIn>
> = {
	onMutate: () => {
		localStorage.removeItem(keys.LocalStorage.AccessToken);
	},
	mutationFn: async (data) => {
		return api
			.post<Record<"user", User>>("/api/v1/users", {
				context: { authMode: "none" },
				json: data,
			})
			.json();
	},
	onSuccess: ({ user }) => {
		localStorage.setItem(keys.LocalStorage.AccessToken, user.token);
		localStorage.setItem(
			keys.LocalStorage.CsrfToken,
			user.csrfToken ?? "<nil>",
		);
		localStorage.setItem(
			keys.LocalStorage.TokenExpiresAt,
			new Date(Date.now() + 15 * time.Minute).toISOString(),
		);
	},
};

export const LogoutOptions: MutationOptions<unknown> = {
	mutationFn: () => api.delete("/api/v1/users/logout"),
	onSuccess: () => {
		clearAuth();
	},
};

export function CurrentUserOptionsFn(...key: unknown[]) {
	return {
		queryKey: [keys.Query.CurrentUser, ...key],
		queryFn: async () =>
			api
				.get<Record<"user", User>>("/api/v1/user", {
					context: { authMode: "optional" },
				})
				.json(),
	} satisfies QueryOptions<Record<"user", User>>;
}
