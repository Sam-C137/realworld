import type { MutationOptions, QueryOptions } from "@tanstack/solid-query";
import { api } from "~/lib/api.ts";
import { keys, time } from "~/lib/constants.ts";
import { sleep } from "~/lib/utils.ts";
import type { CreateArticleSchema } from "~/lib/validation.ts";
import type { Article } from "~/types/article.ts";
import type {
	Paginated,
	PaginationRequestOptionsWithSearchAndSort,
} from "~/types/globals.ts";

interface GetArticlesRequestOptions
	extends PaginationRequestOptionsWithSearchAndSort<"createdAt" | "title"> {
	tag?: string;
	author?: string;
	favorited?: string;
}
export function GetArticlesOptionsFn(searchParams: GetArticlesRequestOptions) {
	return {
		queryKey: [keys.Query.Articles, searchParams],
		queryFn: async () => {
			const response = api.get<Paginated<Article>>("/api/v1/articles", {
				context: { authMode: "optional" },
				searchParams,
			});
			const [data] = await Promise.all([
				response.json(),
				sleep(time.Second * 0.5),
			]);
			return data;
		},
	} satisfies QueryOptions<Paginated<Article>>;
}

export function GetFeedOptionsFn(searchParams: GetArticlesRequestOptions) {
	return {
		queryKey: [keys.Query.Feed, searchParams],
		queryFn: async () => {
			const response = api.get<Paginated<Article>>("/api/v1/articles/feed", {
				searchParams,
			});
			const [data] = await Promise.all([
				response.json(),
				sleep(time.Second * 0.5),
			]);
			return data;
		},
	} satisfies QueryOptions<Paginated<Article>>;
}

export function GetArticleOptionsFn(slug: string) {
	return {
		queryKey: [keys.Query.Article, slug],
		queryFn: async () =>
			api
				.get<Article>(`/api/v1/articles/${slug}`, {
					context: { authMode: "optional" },
				})
				.json(),
	} satisfies QueryOptions<Article>;
}

export const FavoriteArticleOptions: MutationOptions<
	Article,
	unknown,
	Record<"slug", string>
> = {
	mutationFn: async ({ slug }) => {
		return api.post<Article>(`/api/v1/articles/${slug}/favorite`).json();
	},
	meta: {
		invalidateQueries: (_, _e, { slug }: Record<"slug", string>) => [
			[keys.Query.Article, slug],
			[keys.Query.Articles],
			[keys.Query.Feed],
		],
	},
};

export const UnfavoriteArticleOptions: MutationOptions<
	Article,
	unknown,
	Record<"slug", string>
> = {
	mutationFn: async ({ slug }) => {
		return api.delete<Article>(`/api/v1/articles/${slug}/favorite`).json();
	},
	meta: {
		invalidateQueries: (_, _e, { slug }: Record<"slug", string>) => [
			[keys.Query.Article, slug],
			[keys.Query.Articles],
			[keys.Query.Feed],
		],
	},
};

export const CreateArticleOptions: MutationOptions<
	Article,
	unknown,
	typeof CreateArticleSchema.inferOut
> = {
	mutationFn: async (data) => {
		return api
			.post<Article>("/api/v1/articles", {
				json: {
					article: data,
				},
			})
			.json();
	},
	meta: {
		invalidateQueries: [[keys.Query.Articles]],
	},
};

export const EditArticleOptions: MutationOptions<
	Article,
	unknown,
	[string, typeof CreateArticleSchema.inferOut]
> = {
	mutationFn: async ([slug, data]) => {
		return api
			.put<Article>(`/api/v1/articles/${slug}`, {
				json: {
					article: data,
				},
			})
			.json();
	},
	meta: {
		invalidateQueries: (_, _e, [slug]: [string]) => [
			[keys.Query.Article, slug],
			[keys.Query.Articles],
			[keys.Query.Feed],
		],
	},
};

export const DeleteArticleOptions: MutationOptions<unknown, unknown, string> = {
	mutationFn: async (slug) => {
		return Promise.all([
			api.delete(`/api/v1/articles/${slug}`),
			sleep(time.Second * 2),
		]);
	},
	meta: {
		invalidateQueries: (_, _e, slug: string) => [
			[keys.Query.Article, slug],
			[keys.Query.Articles],
			[keys.Query.Feed],
		],
	},
};
