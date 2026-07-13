import type { QueryClient } from "@tanstack/solid-query";
import { createFileRoute, Navigate } from "@tanstack/solid-router";
import { LogoutOptions } from "~/queries/user.ts";

export const Route = createFileRoute("/logout")({
	beforeLoad: async ({ context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			await queryClient
				.getMutationCache()
				.build(queryClient, LogoutOptions)
				.execute();
			queryClient.clear();
		} catch (e) {
			void e; // noop on logout error
		}
	},
	component: () => <Navigate to="/" />,
});
