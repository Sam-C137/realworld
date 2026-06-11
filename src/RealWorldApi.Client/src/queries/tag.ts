import type { QueryOptions } from "@tanstack/solid-query";
import { api } from "~/lib/api.ts";
import { keys } from "~/lib/constants.ts";
import type {
	Paginated,
	PaginationRequestOptionsWithSearchAndSort,
} from "~/types/globals.ts";

interface GetTagsRequestOptions
	extends PaginationRequestOptionsWithSearchAndSort<"createdAt" | "title"> {}

export function GetTagsOptionsFn(searchParams: GetTagsRequestOptions) {
	return {
		queryKey: [keys.Query.Tags, searchParams],
		queryFn: async () => {
			const response = api.get<Paginated<string>>("/api/v1/tags", {
				searchParams,
			});
			return await response.json();
		},
	} satisfies QueryOptions<Paginated<string>>;
}
