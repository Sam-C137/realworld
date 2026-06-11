// biome-ignore-all lint/suspicious/noExplicitAny: TanstackMeta has <any> params to allow type annotations

import {
	type Mutation,
	MutationCache,
	QueryClient,
	type QueryKey,
} from "@tanstack/solid-query";
import { time } from "~/lib/constants.ts";

export interface TanstackMeta<
	TData = any,
	TError = Error,
	TVariables = any,
	TContext = any,
> extends Record<string, unknown> {
	invalidateQueries?:
		| QueryKey[]
		| ((
				data: TData | undefined,
				error: TError | null,
				variables: TVariables,
				context: TContext | undefined,
				mutation: Mutation<TData, TError, TVariables, TContext>,
		  ) => QueryKey[]);
	messages?: Partial<
		Record<
			"error" | "success",
			{
				title: string;
				description?: string;
			}
		>
	>;
}

declare module "@tanstack/solid-query" {
	interface Register {
		queryMeta: TanstackMeta;
		mutationMeta: TanstackMeta;
	}
}

export const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			experimental_prefetchInRender: true,
			staleTime: time.Minute * 2,
			refetchOnWindowFocus: false,
		},
		mutations: {
			onError(e, _v, _c, mutation) {
				const title = mutation.meta?.messages?.error?.title;
				const description = mutation.meta?.messages?.error?.description;
				void { title, description, e };
				// toast.add({
				//     title: title ?? "Something went wrong",
				//     description: description ?? e.message,
				//     type: "error",
				// });
			},
			onSuccess(_d, _v, _c, mutation) {
				const title = mutation.meta?.messages?.success?.title;
				const description = mutation.meta?.messages?.success?.description;
				void { title, description };
				// if (title)
				//     toast.add({
				//         title,
				//         description,
				//         type: "success",
				//     });
			},
		},
	},
	mutationCache: new MutationCache({
		onSettled: async (data, error, variables, context, mutation) => {
			if (mutation.meta?.invalidateQueries) {
				const keys =
					typeof mutation.meta.invalidateQueries === "function"
						? mutation.meta.invalidateQueries(
								data,
								error,
								variables,
								context,
								mutation as Mutation<unknown, Error, unknown, unknown>,
							)
						: mutation.meta.invalidateQueries;

				if (keys && Array.isArray(keys)) {
					await Promise.all(
						keys.map((queryKey) =>
							queryClient.invalidateQueries({
								queryKey,
							}),
						),
					);
				}
			}
		},
	}),
});
