import type { QueryOptions } from "@tanstack/solid-query";
import { api } from "~/lib/api.ts";
import { keys, time } from "~/lib/constants.ts";
import { sleep } from "~/lib/utils.ts";
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
