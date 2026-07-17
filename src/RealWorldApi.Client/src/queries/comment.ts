import type {
	DefaultError,
	InfiniteQueryOptions,
	MutationOptions,
	QueryKey,
} from "@tanstack/solid-query";
import { api } from "~/lib/api.ts";
import { keys, time } from "~/lib/constants.ts";
import { sleep } from "~/lib/utils.ts";
import type { Comment } from "~/types/comment.ts";
import type {
	CursorPaginated,
	CursorPaginationRequestOptionsWithSearch,
} from "~/types/globals.ts";

interface GetCommentsRequestOptions
	extends CursorPaginationRequestOptionsWithSearch {
	slug: string;
}

export function GetCommentsOptionsFn({
	slug,
	...searchParams
}: GetCommentsRequestOptions) {
	return {
		queryKey: [keys.Query.Comments, slug, searchParams],
		queryFn: ({ pageParam }) =>
			api
				.get<CursorPaginated<Comment>>(`/api/v1/articles/${slug}/comments`, {
					context: { authMode: "optional" },
					searchParams: {
						...searchParams,
						after: pageParam,
					},
				})
				.json(),
		initialPageParam: undefined,
		getPreviousPageParam: (page) => page.previous,
		getNextPageParam: (page) => page.next,
	} satisfies InfiniteQueryOptions<
		CursorPaginated<Comment>,
		DefaultError,
		CursorPaginated<Comment>,
		QueryKey,
		string | undefined
	>;
}

export const CreateCommentOptions: MutationOptions<
	Comment,
	DefaultError,
	{ slug: string; body: string },
	unknown
> = {
	mutationFn: async ({ slug, body }) => {
		const response = api.post<Comment>(`/api/v1/articles/${slug}/comments`, {
			json: {
				comment: {
					body,
				},
			},
		});
		const [data] = await Promise.all([response.json(), sleep(time.Second * 2)]);
		return data;
	},
	meta: {
		invalidateQueries: (_, _e, { slug }: Record<"slug", string>) => [
			[keys.Query.Comments, slug],
		],
	},
};

export const DeleteCommentOptions: MutationOptions<
	unknown,
	DefaultError,
	{ slug: string; id: string },
	unknown
> = {
	mutationFn: async ({ slug, id }) => {
		await Promise.all([
			api.delete(`/api/v1/articles/${slug}/comments/${id}`),
			sleep(time.Second * 2),
		]);
	},
	meta: {
		invalidateQueries: (_, _e, { slug }: Record<"slug", string>) => [
			[keys.Query.Comments, slug],
		],
	},
};
