import { type QueryClient, useQuery } from "@tanstack/solid-query";
import { createFileRoute, Link } from "@tanstack/solid-router";
import { type } from "arktype";
import { For, Match, Show, Switch } from "solid-js";
import { ArticleItem, ArticleItemLoading } from "~/components/article-item.tsx";
import { PaginationBar } from "~/components/ui/pagination.tsx";
import { Skeleton } from "~/components/ui/skeleton.tsx";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { GetArticlesOptionsFn, GetFeedOptionsFn } from "~/queries/article.ts";
import { GetTagsOptionsFn } from "~/queries/tag.ts";
import { CurrentUserOptions } from "~/queries/user.ts";

const SearchSchema = type({
	"tag?": "string",
	"feed?": "true|undefined",
	order: "'asc' | 'desc' = 'desc'",
	page: "string.numeric.parse | number = 1",
	limit: "string.numeric.parse | number = 10",
});

export const Route = createFileRoute("/")({
	component: App,
	validateSearch: SearchSchema,
	loader: async ({ context }) => {
		const { queryClient } = context as { queryClient: QueryClient };
		void queryClient.prefetchQuery(CurrentUserOptions);
	},
});

function App() {
	const tags = useQuery(() =>
		GetTagsOptionsFn({ limit: -1, sort: "popularity", order: "desc" }),
	);
	const user = useQuery(() => CurrentUserOptions);
	const search = Route.useSearch();
	const articles = useQuery(() => GetArticlesOptionsFn(search()));
	const feed = useQuery(() => ({
		...GetFeedOptionsFn(search()),
		enabled: user.isSuccess,
	}));

	return (
		<main class="min-h-dvh text-foreground space-y-2">
			<section class="grid place-items-center gap-y-4 w-full p-12 bg-primary text-primary-foreground mx-auto max-w-full">
				<p class="text-6xl font-bold text-center">conduit</p>
				<span class="text-2xl font-semibold text-center text-balance">
					A place to share your knowledge
				</span>
			</section>
			<section class="py-4 px-4 sm:px-10 flex justify-between gap-8 mx-auto max-w-7xl">
				<Tabs
					defaultValue="global"
					class="w-full"
					value={search().feed ? "feed" : "global"}
				>
					<TabsList class="border-b bg-transparent p-0 w-full justify-start">
						<TabsTrigger
							value="global"
							class="-mb-1 cursor-pointer text-foreground data-[selected]:text-primary"
							as={Link}
							search={{
								...search(),
								//@ts-expect-error `as` destroys typing
								feed: undefined,
							}}
						>
							Global Feed
						</TabsTrigger>
						<Show when={user.isSuccess}>
							<TabsTrigger
								value="feed"
								class="-mb-1 cursor-pointer text-foreground data-[selected]:text-primary"
								as={Link}
								search={{
									...search(),
									//@ts-expect-error `as` destroys typing
									feed: true,
								}}
							>
								My Feed
							</TabsTrigger>
						</Show>
					</TabsList>
					<TabsContent value="global">
						<Switch>
							<Match when={articles.isPending}>
								<ul>
									<For each={Array(10).fill(null)}>{ArticleItemLoading}</For>
								</ul>
							</Match>
							<Match when={articles.isError}>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-destructive text-center">
										Error: {articles.error?.message}
									</p>
								</div>
							</Match>
							<Match
								when={!articles.isPending && articles.data?.data.length === 0}
							>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-muted-foreground text-center">
										No articles are here... yet.
									</p>
								</div>
							</Match>
							<Match when={articles.isSuccess}>
								<ul>
									<For each={articles.data?.data}>
										{(article) => (
											<ArticleItem
												article={article}
												user={user.data?.user ?? null}
											/>
										)}
									</For>
								</ul>
								<Show when={!!articles.data?.total}>
									<PaginationBar total={articles.data?.total} class="mx-0" />
								</Show>
							</Match>
						</Switch>
					</TabsContent>
					<TabsContent value="feed">
						<Switch>
							<Match when={feed.isPending}>
								<ul>
									<For each={Array(10).fill(null)}>{ArticleItemLoading}</For>
								</ul>
							</Match>
							<Match when={feed.isError}>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-destructive text-center">
										Error: {feed.error?.message}
									</p>
								</div>
							</Match>
							<Match when={!feed.isPending && feed.data?.data.length === 0}>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-muted-foreground text-center">
										No articles are here... yet. Follow someone to see their
										articles on your feed
									</p>
								</div>
							</Match>
							<Match when={feed.isSuccess}>
								<ul>
									<For each={feed.data?.data}>
										{(article) => (
											<ArticleItem
												article={article}
												user={user.data?.user ?? null}
											/>
										)}
									</For>
								</ul>
								<Show when={!!feed.data?.total}>
									<PaginationBar total={feed.data?.total} class="mx-0" />
								</Show>
							</Match>
						</Switch>
					</TabsContent>
				</Tabs>
				<div class="hidden sm:flex flex-col gap-3 max-w-72 min-w-72">
					<p>Popular tags</p>
					<div class="flex gap-1 flex-wrap">
						<Switch>
							<Match when={tags.isPending}>
								<For each={Array(15).fill(null)}>
									{(_, index) => (
										<Skeleton
											class="rounded-full h-6! inline"
											style={{ width: `${2 + (index() % 5)}rem` }}
										/>
									)}
								</For>
							</Match>
							<Match when={tags.isError}>
								<p class="text-destructive">Error: {tags.error?.message}</p>
							</Match>
							<Match when={tags.isSuccess}>
								<For each={tags.data?.data}>
									{(tag) => (
										<Link
											to="."
											search={{
												tag: search().tag === tag ? undefined : tag,
											}}
											class="rounded-full px-2 py-1 pb-1.25 text-sm leading-3 text-primary-foreground bg-[#818a91] hover:bg-[#687077] transition-colors"
											activeProps={{ class: "bg-[#687077]!" }}
										>
											{tag}
										</Link>
									)}
								</For>
							</Match>
						</Switch>
					</div>
				</div>
			</section>
		</main>
	);
}
