import { type QueryClient, useMutation, useQuery } from "@tanstack/solid-query";
import { Await, createFileRoute, defer, Link } from "@tanstack/solid-router";
import { type } from "arktype";
import { format } from "date-fns";
import { ErrorBoundary, For, Match, Show, Suspense, Switch } from "solid-js";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button.tsx";
import { PaginationBar } from "~/components/ui/pagination.tsx";
import { Skeleton } from "~/components/ui/skeleton.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { cn } from "~/lib/utils.ts";
import {
	FavoriteArticleOptions,
	GetArticlesOptionsFn,
	GetFeedOptionsFn,
	UnfavoriteArticleOptions,
} from "~/queries/article.ts";
import { GetTagsOptionsFn } from "~/queries/tag.ts";
import { CurrentUserOptionsFn } from "~/queries/user.ts";
import type { Article } from "~/types/article.ts";
import type { User } from "~/types/user.ts";

const SearchSchema = type({
	"tag?": "string",
	"feed?": "true|undefined",
	page: "string.numeric.parse | number = 1",
	limit: "string.numeric.parse | number = 10",
});

export const Route = createFileRoute("/")({
	component: App,
	validateSearch: SearchSchema,
	loader: async ({ context }) => {
		const { queryClient } = context as { queryClient: QueryClient };
		const user = defer(queryClient.ensureQueryData(CurrentUserOptionsFn()));
		return { user };
	},
});

function App() {
	const loaderData = Route.useLoaderData();
	const tags = useQuery(() =>
		GetTagsOptionsFn({ limit: -1, sort: "popularity", order: "desc" }),
	);
	const search = Route.useSearch();
	const articles = useQuery(() => GetArticlesOptionsFn(search()));
	const feed = useQuery(() => GetFeedOptionsFn(search()));

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
						<ErrorBoundary fallback={null}>
							<Suspense fallback={null}>
								<Await promise={loaderData().user}>
									{() => (
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
									)}
								</Await>
							</Suspense>
						</ErrorBoundary>
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
											<ErrorBoundary
												fallback={<ArticleItem article={article} user={null} />}
											>
												<Suspense
													fallback={
														<ArticleItem article={article} user={null} />
													}
												>
													<Await promise={loaderData().user}>
														{(user) => (
															<ArticleItem article={article} user={user.user} />
														)}
													</Await>
												</Suspense>
											</ErrorBoundary>
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
											<ErrorBoundary
												fallback={<ArticleItem article={article} user={null} />}
											>
												<Suspense
													fallback={
														<ArticleItem article={article} user={null} />
													}
												>
													<Await promise={loaderData().user}>
														{(user) => (
															<ArticleItem article={article} user={user.user} />
														)}
													</Await>
												</Suspense>
											</ErrorBoundary>
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

interface ArticleItemProps {
	article: Article;
	user: User | null;
}

function ArticleItem(props: ArticleItemProps) {
	const navigate = Route.useNavigate();
	const favorite = useMutation(() => FavoriteArticleOptions);
	const unfavorite = useMutation(() => UnfavoriteArticleOptions);

	return (
		<li class="space-y-2 w-full py-6 not-last:border-b border-border relative">
			<div class="flex items-center justify-between relative z-2">
				<div class="flex gap-2 items-center">
					<Avatar>
						<AvatarImage
							src={props.article.article.author.image ?? undefined}
						/>
						<AvatarFallback class="capitalize">
							{props.article.article.author.username.charAt(0)}
						</AvatarFallback>
					</Avatar>
					<div class="flex flex-col gap-1">
						<Link
							to="/profile/username"
							params={{
								username: props.article.article.author.username,
							}}
							class="text-primary leading-none font-medium hover:underline"
						>
							{props.article.article.author.username}
						</Link>
						<span class="text-muted-foreground text-xs leading-none">
							{format(
								new Date(props.article.article.createdAt),
								"MMM dd, yyyy",
							)}
						</span>
					</div>
				</div>
				<Button
					variant="outline"
					class={cn(
						"gap-0.5 px-2 py-1 h-max w-max rounded-[3px] border-primary text-primary hover:bg-primary hover:text-primary-foreground transition-colors cursor-pointer",
						props.article.article.favorited &&
							"bg-primary text-primary-foreground",
					)}
					disabled={favorite.isPending || unfavorite.isPending}
					onclick={() => {
						if (!props.user) {
							navigate({ to: "/login" });
							return;
						}
						if (props.article.article.favorited) {
							unfavorite.mutate({ slug: props.article.article.slug });
						} else {
							favorite.mutate({ slug: props.article.article.slug });
						}
					}}
				>
					<Show
						when={unfavorite.isPending || favorite.isPending}
						fallback={<i class="ri-heart-fill text-base" />}
					>
						<Swirling class="size-4" />
					</Show>
					<span class="tabular-nums">
						{props.article.article.favoritesCount}
					</span>
				</Button>
			</div>
			<Link
				to="/article/$slug"
				params={{
					slug: props.article.article.slug,
				}}
				class="text-xl truncate font-medium max-w-full before:absolute before:content-[''] before:inset-0 before:z-1 before:size-full"
			>
				{props.article.article.title}
			</Link>
			<p class="text-muted-foreground">{props.article.article.description}</p>
			<div class="flex items-center justify-between">
				<span class="text-xs text-muted-foreground">Read more...</span>
				<Show when={props.article.article.tagList.length > 0}>
					<div class="space-x-1 relative z-2">
						<For each={props.article.article.tagList}>
							{(tag) => (
								<span class="rounded-full border border-border px-2 py-1 pb-1.25 text-sm leading-3 text-muted-foreground">
									{tag}
								</span>
							)}
						</For>
					</div>
				</Show>
			</div>
		</li>
	);
}

function ArticleItemLoading() {
	return (
		<li class="space-y-2 w-full py-6 not-last:border-b border-border">
			<div class="flex items-center justify-between">
				<div class="flex gap-2 items-center">
					<Skeleton class="rounded-full size-10!" />
					<div class="flex flex-col gap-1">
						<Skeleton class="h-4! w-20!" />
						<Skeleton class="h-3! w-10!" />
					</div>
				</div>
				<Skeleton class="h-6! w-10!" />
			</div>
			<Skeleton class="h-8! w-3/4!" />
			<Skeleton class="h-6! w-1/2!" />
			<div class="flex items-center justify-between">
				<Skeleton class="h-4! w-20!" />
				<div class="space-x-1">
					<For each={Array(2).fill(null)}>
						{() => <Skeleton class="rounded-full h-6! w-12! inline-block" />}
					</For>
				</div>
			</div>
		</li>
	);
}
