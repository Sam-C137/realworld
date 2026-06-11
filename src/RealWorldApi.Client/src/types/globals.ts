export interface Paginated<T> {
	data: T[];
	total: number;
}

export interface CursorPaginated<T> {
	data: T[];
	next: string | null;
	previous: string | null;
}

export interface PaginationRequestOptions
	extends Record<string, string | number | boolean | undefined> {
	page?: number;
	limit?: number;
}

export interface PaginationRequestOptionsWithSearch
	extends PaginationRequestOptions {
	search?: string;
}

export interface PaginationRequestOptionsWithSearchAndSort<
	TSort extends string = string,
> extends PaginationRequestOptionsWithSearch {
	order?: "asc" | "desc";
	sort?: TSort;
}

export interface CursorPaginationRequestOptions
	extends Record<string, string | number | boolean | undefined> {
	limit?: number;
	after?: string;
	before?: string;
}

export interface CursorPaginationRequestOptionsWithSearch
	extends CursorPaginationRequestOptions {
	search?: string;
}
