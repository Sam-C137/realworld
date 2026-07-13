import { type QueryClient, useMutation } from "@tanstack/solid-query";
import {
	Await,
	createFileRoute,
	defer,
	Link,
	notFound,
} from "@tanstack/solid-router";
import { format } from "date-fns";
import { ErrorBoundary, For, Show, Suspense } from "solid-js";
import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "~/components/ui/avatar.tsx";
import { Separator } from "~/components/ui/separator.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { cn } from "~/lib/utils.ts";
import {
	FavoriteArticleOptions,
	GetArticleOptionsFn,
	UnfavoriteArticleOptions,
} from "~/queries/article.ts";
import { CurrentUserOptionsFn } from "~/queries/user.ts";
import type { User } from "~/types/user.ts";

export const Route = createFileRoute("/article/$slug")({
	loader: async ({ params: { slug }, context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			const article = await queryClient.fetchQuery(GetArticleOptionsFn(slug));
			const user = defer(queryClient.ensureQueryData(CurrentUserOptionsFn()));
			return { article, user };
		} catch (e) {
			if (e instanceof Error && e.message.includes("404")) {
				throw notFound();
			}
			return {
				article: null,
				user: defer(Promise.resolve(undefined)),
			};
		}
	},
	component: ArticlePage,
	notFoundComponent: () => (
		<div class="w-full py-20">
			<p class="text-lg font-medium text-center">Article Not found</p>
			<p class="text-center">This article does not exist or has been deleted</p>
		</div>
	),
});

function ArticlePage() {
	const loaderData = Route.useLoaderData();
	const routeContext = Route.useRouteContext();

	return (
		<main class="min-h-dvh text-foreground">
			<section class="space-y-4 w-full px-4 sm:px-10 py-8 bg-[#3a4551] dark:bg-[#1a1f2e] text-gray-100 mx-auto max-w-full">
				<p class="text-4xl font-bold">{loaderData().article?.article.title}</p>
				<ErrorBoundary fallback={<ArticleAuthor user={null} />}>
					<Suspense fallback={<ArticleAuthor user={null} />}>
						<Await promise={loaderData().user}>
							{(user) => <ArticleAuthor user={user?.user ?? null} />}
						</Await>
					</Suspense>
				</ErrorBoundary>
			</section>
			<section class="px-4 sm:px-10 py-6">
				<div class="space-y-8 py-10">
					<p class="text-xl">{loaderData().article?.article.description}</p>
					<Show when={(loaderData().article?.article.tagList.length ?? 0) > 0}>
						<div class="space-x-1 relative z-2">
							<For each={loaderData().article?.article.tagList}>
								{(tag) => (
									<span class="rounded-full border border-border px-2 py-1 pb-1.25 text-sm leading-3 text-muted-foreground">
										{tag}
									</span>
								)}
							</For>
						</div>
					</Show>
				</div>
			</section>
			<div class="px-4 sm:px-10">
				<Separator />
			</div>
			<section class="px-4 sm:px-10 py-6 flex justify-center">
				<ErrorBoundary fallback={<ArticleAuthor user={null} />}>
					<Suspense fallback={<ArticleAuthor user={null} />}>
						<Await promise={loaderData().user}>
							{(user) => <ArticleAuthor user={user?.user ?? null} />}
						</Await>
					</Suspense>
				</ErrorBoundary>
			</section>
		</main>
	);
}

function ArticleAuthor(props: Record<"user", User | null>) {
	const navigate = Route.useNavigate();
	const loaderData = Route.useLoaderData();
	const favorite = useMutation(() => FavoriteArticleOptions);
	const unfavorite = useMutation(() => UnfavoriteArticleOptions);

	return (
		<div class="flex items-end gap-4">
			<div class="flex gap-2 items-center">
				<Avatar>
					<AvatarImage
						src={loaderData().article?.article.author.image ?? undefined}
					/>
					<AvatarFallback class="capitalize text-[#3a4551] darK:text-[#1a1f2e]">
						{loaderData().article?.article.author.username.charAt(0)}
					</AvatarFallback>
				</Avatar>
				<div class="flex flex-col gap-1">
					<Link
						to="/profile/username"
						params={{
							username: loaderData().article?.article.author.username,
						}}
						class="leading-none font-medium hover:underline"
					>
						{loaderData().article?.article.author.username}
					</Link>
					<span class="text-[#dad8d8] text-xs leading-none">
						{format(
							new Date(loaderData().article?.article.createdAt ?? 0),
							"MMM dd, yyyy",
						)}
					</span>
				</div>
			</div>
			<ErrorBoundary fallback={null}>
				<Suspense fallback={null}>
					<Await promise={loaderData().user}>
						{(user) => (
							<div class="space-x-2">
								<Show
									when={
										user?.user.username !==
										loaderData().article?.article.author.username
									}
								>
									<button
										type="button"
										class="text-sm px-2 cursor-pointer py-1 border border-[#6c757d] text-[#6c757d] hover:bg-[#6c757d] hover:text-white transition-colors"
									>
										<i class="ri-add-line text-base mr-1" />
										Follow {loaderData().article?.article.author.username}
									</button>
								</Show>
								<Show when={user}>
									<button
										type="button"
										class={cn(
											"text-sm px-2 cursor-pointer py-1 border border-primary text-primary hover:bg-primary hover:text-primary-foreground transition-colors",
											loaderData().article?.article.favorited &&
												"bg-primary text-primary-foreground",
										)}
										disabled={favorite.isPending || unfavorite.isPending}
										onclick={() => {
											if (!props.user) {
												navigate({ to: "/login" });
												return;
											}
											if (loaderData().article?.article.favorited) {
												unfavorite.mutate({
													slug: loaderData().article?.article.slug as string,
												});
											} else {
												favorite.mutate({
													slug: loaderData().article?.article.slug as string,
												});
											}
										}}
									>
										<Show
											when={unfavorite.isPending || favorite.isPending}
											fallback={<i class="ri-heart-fill text-base mr-1" />}
										>
											<Swirling class="size-4 mr-1" />
										</Show>
										{loaderData().article?.article.favorited
											? "Unfavorite"
											: "Favorite"}{" "}
										Article
										<span class="tabular-nums">
											({loaderData().article?.article.favoritesCount})
										</span>
									</button>
								</Show>
							</div>
						)}
					</Await>
				</Suspense>
			</ErrorBoundary>
		</div>
	);
}
