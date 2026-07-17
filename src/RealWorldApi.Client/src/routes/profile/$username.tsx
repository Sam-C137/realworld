import { type QueryClient, useQuery } from "@tanstack/solid-query";
import { createFileRoute, Link, notFound } from "@tanstack/solid-router";
import { type } from "arktype";
import { For, Match, Show, Switch } from "solid-js";
import { ArticleItem, ArticleItemLoading } from "~/components/article-item.tsx";
import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "~/components/ui/avatar.tsx";
import { PaginationBar } from "~/components/ui/pagination.tsx";
import {
	Tabs,
	TabsContent,
	TabsList,
	TabsTrigger,
} from "~/components/ui/tabs.tsx";
import { GetArticlesOptionsFn } from "~/queries/article.ts";
import { GetProfileOptionsFn } from "~/queries/profile.ts";
import { CurrentUserOptions } from "~/queries/user.ts";

const SearchSchema = type({
	tab: "'articles' | 'favorites' = 'articles'",
	order: "'asc' | 'desc' = 'desc'",
	page: "string.numeric.parse | number = 1",
	limit: "string.numeric.parse | number = 10",
});

export const Route = createFileRoute("/profile/$username")({
	loader: async ({ params: { username }, context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			const profile = await queryClient.fetchQuery(
				GetProfileOptionsFn(username),
			);
			return { profile };
		} catch (e) {
			if (e instanceof Error && e.message.includes("404")) {
				throw notFound();
			}
			throw e;
		}
	},
	component: ProfilePage,
	validateSearch: SearchSchema,
	notFoundComponent: () => (
		<div class="w-full py-20">
			<p class="text-lg font-medium text-center">Profile Not found</p>
			<p class="text-center">
				This user profile does not exist or has been deleted
			</p>
		</div>
	),
});

function ProfilePage() {
	const search = Route.useSearch();
	const loaderDater = Route.useLoaderData();
	const articles = useQuery(() =>
		GetArticlesOptionsFn({
			...search(),
			author: loaderDater().profile.profile.username,
		}),
	);
	const favorited = useQuery(() =>
		GetArticlesOptionsFn({
			...search(),
			favorited: loaderDater().profile.profile.username,
		}),
	);
	const user = useQuery(() => ({
		...CurrentUserOptions,
		retry: false,
	}));

	return (
		<main class="min-h-dvh text-foreground space-y-2">
			<section class="grid place-items-center gap-y-4 w-full p-12 bg-[#f5f5f5] text-foreground dark:bg-[#1a1a1a] dark:text-foreground mx-auto max-w-full">
				<Avatar class="size-28 border-2 border-input transition group-hover:border-primary group-focus-within:ring-3 group-focus-within:ring-ring/50">
					<AvatarImage
						src={loaderDater().profile.profile.image ?? undefined}
						alt={`${loaderDater().profile.profile.username}'s profile image`}
					/>
					<AvatarFallback class="text-3xl capitalize">
						{loaderDater().profile.profile.username.charAt(0)}
					</AvatarFallback>
				</Avatar>
				<p class="text-3xl font-bold text-center">
					{loaderDater().profile.profile.username}
				</p>
				<div class="w-full flex justify-end">
					<Show
						when={
							user.isSuccess &&
							user.data.user.username === loaderDater().profile.profile.username
						}
						fallback={
							<button
								type="button"
								class="text-sm px-2 cursor-pointer py-1 border border-[#6c757d] text-[#6c757d] hover:bg-[#6c757d] hover:text-white transition-colors"
							>
								<i class="ri-add-line text-base mr-1" />
								Follow {loaderDater().profile.profile.username}
							</button>
						}
					>
						<Link to="/settings">
							<button
								type="button"
								class="text-sm px-2 cursor-pointer py-1 border border-[#6c757d] text-[#6c757d] hover:bg-[#6c757d] hover:text-white transition-colors"
							>
								<i class="ri-settings-5-line text-base mr-1" />
								Edit Profile Settings
							</button>
						</Link>
					</Show>
				</div>
			</section>
			<section class="py-4 px-4 sm:px-10 flex justify-between gap-8 mx-auto max-w-5xl">
				<Tabs defaultValue="global" class="w-full" value={search().tab}>
					<TabsList class="border-b bg-transparent p-0 w-full justify-start">
						<TabsTrigger
							value="articles"
							class="-mb-1 cursor-pointer text-foreground data-[selected]:text-primary"
							as={Link}
							search={{
								...search(),
								//@ts-expect-error `as` destroys typing
								tab: "articles",
							}}
						>
							<Show
								when={
									user.isSuccess &&
									user.data.user.username ===
										loaderDater().profile.profile.username
								}
								fallback={
									<>Articles by {loaderDater().profile.profile.username}</>
								}
							>
								My Articles
							</Show>
						</TabsTrigger>
						<TabsTrigger
							value="favorites"
							class="-mb-1 cursor-pointer text-foreground data-[selected]:text-primary"
							as={Link}
							search={{
								...search(),
								//@ts-expect-error `as` destroys typing
								tab: "favorites",
							}}
						>
							Favorited Articles
						</TabsTrigger>
					</TabsList>
					<TabsContent value="articles">
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
										No articles created by this user... yet.
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
					<TabsContent value="favorites">
						<Switch>
							<Match when={favorited.isPending}>
								<ul>
									<For each={Array(10).fill(null)}>{ArticleItemLoading}</For>
								</ul>
							</Match>
							<Match when={favorited.isError}>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-destructive text-center">
										Error: {favorited.error?.message}
									</p>
								</div>
							</Match>
							<Match
								when={!favorited.isPending && favorited.data?.data.length === 0}
							>
								<div class="w-full min-h-40 grid place-items-center">
									<p class="text-muted-foreground text-center">
										No articles are here... yet.
									</p>
								</div>
							</Match>
							<Match when={favorited.isSuccess}>
								<ul>
									<For each={favorited.data?.data}>
										{(article) => (
											<ArticleItem
												article={article}
												user={user.data?.user ?? null}
											/>
										)}
									</For>
								</ul>
								<Show when={!!favorited.data?.total}>
									<PaginationBar total={favorited.data?.total} class="mx-0" />
								</Show>
							</Match>
						</Switch>
					</TabsContent>
				</Tabs>
			</section>
		</main>
	);
}
