import {
	type QueryClient,
	useInfiniteQuery,
	useMutation,
} from "@tanstack/solid-query";
import {
	Await,
	createFileRoute,
	defer,
	Link,
	notFound,
} from "@tanstack/solid-router";
import { format } from "date-fns";
import {
	createMemo,
	createSignal,
	ErrorBoundary,
	For,
	Match,
	Show,
	Suspense,
	Switch,
} from "solid-js";
import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "~/components/ui/avatar.tsx";
import { Button } from "~/components/ui/button.tsx";
import { InfiniteScrollContainer } from "~/components/ui/infinite-scroll-container.tsx";
import { Separator } from "~/components/ui/separator.tsx";
import { Skeleton } from "~/components/ui/skeleton.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { cn } from "~/lib/utils.ts";
import {
	DeleteArticleOptions,
	FavoriteArticleOptions,
	GetArticleOptionsFn,
	UnfavoriteArticleOptions,
} from "~/queries/article.ts";
import {
	CreateCommentOptions,
	DeleteCommentOptions,
	GetCommentsOptionsFn,
} from "~/queries/comment.ts";
import { CurrentUserOptions } from "~/queries/user.ts";
import type { Comment } from "~/types/comment.ts";
import type { User } from "~/types/user.ts";

export const Route = createFileRoute("/article/$slug")({
	loader: async ({ params: { slug }, context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			const article = await queryClient.fetchQuery(GetArticleOptionsFn(slug));
			const user = defer(
				queryClient.ensureQueryData({
					...CurrentUserOptions,
					retry: false,
				}),
			);
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
	const params = Route.useParams();
	const loaderData = Route.useLoaderData();
	const commentsQuery = useInfiniteQuery(() =>
		GetCommentsOptionsFn({
			slug: params().slug,
		}),
	);
	const comments = createMemo(
		() => commentsQuery.data?.pages.flatMap((page) => page.data) ?? [],
	);

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
					<p class="text-xl">{loaderData().article?.article.body}</p>
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
			<section class="px-4 sm:px-10 py-6 flex flex-col items-center justify-center gap-6">
				<ErrorBoundary fallback={<ArticleAuthor user={null} />}>
					<Suspense fallback={<ArticleAuthor user={null} />}>
						<Await promise={loaderData().user}>
							{(user) => <ArticleAuthor user={user?.user ?? null} />}
						</Await>
					</Suspense>
				</ErrorBoundary>
				<div class="mx-auto w-[min(100%,var(--container-3xl))]">
					<ErrorBoundary
						fallback={
							<p class="w-full *:[a]:text-primary *:[a]:hover:underline *:[a]:underline-offset-4">
								<Link to="/login">Sign in</Link> or{" "}
								<Link to="/register">sign up</Link> to add comments on this
								article.
							</p>
						}
					>
						<Suspense fallback={<CommentItemLoading />}>
							<Await promise={loaderData().user}>
								{(user) => <CommentBox user={user?.user ?? null} />}
							</Await>
						</Suspense>
					</ErrorBoundary>
					<InfiniteScrollContainer
						class="mt-2 flex flex-col gap-2"
						isLoading={commentsQuery.isFetching}
						hasMore={commentsQuery.hasNextPage}
						next={commentsQuery.fetchNextPage}
					>
						<Switch>
							<Match when={commentsQuery.isSuccess && comments().length < 1}>
								<p class="my-6 text-sm text-muted-foreground mx-auto w-fit">
									No comments have been added on this post yet. Be the first to
									comment!
								</p>
							</Match>
							<Match when={commentsQuery.isSuccess && comments().length > 0}>
								<For each={comments()}>
									{(comment) => (
										<ErrorBoundary
											fallback={<CommentItem comment={comment} user={null} />}
										>
											<Suspense
												fallback={<CommentItem comment={comment} user={null} />}
											>
												<Await promise={loaderData().user}>
													{(user) => (
														<CommentItem
															comment={comment}
															user={user?.user ?? null}
														/>
													)}
												</Await>
											</Suspense>
										</ErrorBoundary>
									)}
								</For>
							</Match>
							<Match when={commentsQuery.isError}>
								<p class="my-6 text-sm text-destructive mx-auto w-fit">
									Failed to load comments. Please try again later.
								</p>
							</Match>
						</Switch>
						<Switch>
							<Match when={commentsQuery.isPending}>
								<For each={Array(4).fill(null)}>{CommentItemLoading}</For>
							</Match>
							<Match when={commentsQuery.isFetchingNextPage}>
								<For each={Array(2).fill(null)}>{CommentItemLoading}</For>
							</Match>
						</Switch>
					</InfiniteScrollContainer>
				</div>
			</section>
		</main>
	);
}

function ArticleAuthor(props: Record<"user", User | null>) {
	const navigate = Route.useNavigate();
	const loaderData = Route.useLoaderData();
	const article = createMemo(() => loaderData().article?.article);
	const favorite = useMutation(() => FavoriteArticleOptions);
	const unfavorite = useMutation(() => UnfavoriteArticleOptions);
	const deleteArticle = useMutation(() => DeleteArticleOptions);

	return (
		<div class="flex items-end gap-4">
			<div class="flex gap-2 items-center">
				<Avatar>
					<AvatarImage src={article()?.author.image ?? undefined} />
					<AvatarFallback class="capitalize text-[#3a4551] darK:text-[#1a1f2e]">
						{article()?.author.username.charAt(0)}
					</AvatarFallback>
				</Avatar>
				<div class="flex flex-col gap-1">
					<Link
						to="/profile/$username"
						params={{
							username: article()?.author.username as string,
						}}
						class="leading-none font-medium hover:underline"
					>
						{article()?.author.username}
					</Link>
					<span class="text-[#dad8d8] text-xs leading-none">
						{format(new Date(article()?.createdAt ?? 0), "MMM dd, yyyy")}
					</span>
				</div>
			</div>
			<div class="space-x-2">
				<Show
					when={props.user?.username !== article()?.author.username}
					fallback={
						<>
							<Link
								to="/editor"
								search={{
									slug: article()?.slug,
								}}
							>
								<button
									type="button"
									class="text-sm px-2 cursor-pointer py-1 border border-[#6c757d] text-[#6c757d] hover:bg-[#6c757d] hover:text-white transition-colors"
								>
									<i class="ri-edit-2-line text-base mr-1" />
									Edit Article
								</button>
							</Link>{" "}
							<button
								type="button"
								class="inline-flex items-center text-sm px-2 cursor-pointer py-1 border border-destructive text-destructive hover:bg-destructive hover:text-white transition-colors"
								disabled={deleteArticle.isPending}
								onclick={() =>
									deleteArticle.mutate(article()?.slug as string, {
										onSuccess() {
											navigate({
												to: "/",
											});
										},
									})
								}
							>
								<Show
									when={deleteArticle.isPending}
									fallback={<i class="ri-delete-bin-line text-base mr-1" />}
								>
									<Swirling class="size-4 mr-1" />
								</Show>
								Delete Article
							</button>
						</>
					}
				>
					<button
						type="button"
						class="text-sm px-2 cursor-pointer py-1 border border-[#6c757d] text-[#6c757d] hover:bg-[#6c757d] hover:text-white transition-colors"
					>
						<i class="ri-add-line text-base mr-1" />
						Follow {article()?.author.username}
					</button>
					<button
						type="button"
						class={cn(
							"text-sm px-2 cursor-pointer py-1 border border-primary text-primary hover:bg-primary hover:text-primary-foreground transition-colors",
							article()?.favorited && "bg-primary text-primary-foreground",
						)}
						disabled={favorite.isPending || unfavorite.isPending}
						onclick={() => {
							if (!props.user) {
								navigate({ to: "/login" });
								return;
							}
							if (article()?.favorited) {
								unfavorite.mutate({
									slug: article()?.slug as string,
								});
							} else {
								favorite.mutate({
									slug: article()?.slug as string,
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
						{article()?.favorited ? "Unfavorite" : "Favorite"} Article
						<span class="tabular-nums">({article()?.favoritesCount})</span>
					</button>
				</Show>
			</div>
		</div>
	);
}

interface CommentItemProps {
	comment: Comment;
	user: User | null;
}

function CommentItem(props: CommentItemProps) {
	const params = Route.useParams();
	const deleteComment = useMutation(() => DeleteCommentOptions);

	return (
		<article class="w-full rounded-xs border border-border">
			<p class="p-4 whitespace-pre-wrap">{props.comment.body}</p>
			<footer class="flex items-center justify-between border-t border-border px-4 py-2 bg-[#f5f5f5] dark:bg-[#1a1f2e]">
				<div class="flex items-center gap-2">
					<Avatar class="size-8">
						<AvatarImage src={props.comment.author.image ?? undefined} />
						<AvatarFallback class="capitalize bg-background">
							{props.comment.author.username.charAt(0)}
						</AvatarFallback>
					</Avatar>
					<Link
						to="/profile/$username"
						params={{ username: props.comment.author.username }}
						class="text-sm text-primary hover:underline"
					>
						{props.comment.author.username}
					</Link>
					<time class="text-xs text-muted-foreground">
						{format(new Date(props.comment.createdAt), "MMM dd, yyyy")}
					</time>
				</div>
				<Show when={props.user?.username === props.comment.author.username}>
					<Button
						variant="ghost"
						size="icon"
						class="size-8 hover:text-destructive cursor-pointer"
						onclick={() => {
							if (deleteComment.isPending) return;
							deleteComment.mutate({
								slug: params().slug,
								id: props.comment.id,
							});
						}}
					>
						<Show
							when={deleteComment.isPending}
							fallback={<i class="ri-delete-bin-line text-base" />}
						>
							<Swirling class="size-4" />
						</Show>
					</Button>
				</Show>
			</footer>
		</article>
	);
}

function CommentItemLoading() {
	return (
		<div class="w-full rounded-xs border border-border">
			<div class="p-4 space-y-1">
				<Skeleton class="h-4! w-full! max-w-lg!" />
				<Skeleton class="h-4! w-full! max-w-md!" />
			</div>
			<div class="flex items-center gap-2 border-t border-border px-4 py-2 bg-[#f5f5f5] dark:bg-[#1a1f2e]">
				<Skeleton class="rounded-full size-10!" />
				<Skeleton class="h-4! w-20!" />
				<Skeleton class="h-3! w-10!" />
			</div>
		</div>
	);
}

function CommentBox(props: Record<"user", User | null>) {
	const params = Route.useParams();
	const [value, setValue] = createSignal("");
	const createComment = useMutation(() => CreateCommentOptions);

	return (
		<form
			class="w-full rounded-xs border border-border"
			onSubmit={(e) => {
				e.preventDefault();
				const body = value().trim();
				if (body) {
					createComment.mutate(
						{ slug: params().slug, body },
						{
							onSuccess() {
								setValue("");
							},
						},
					);
				}
			}}
		>
			<textarea
				class="w-full p-4 resize-none bg-transparent outline-none"
				placeholder="Write a comment..."
				value={value()}
				onInput={(e) => setValue(e.currentTarget.value)}
			/>
			<Show when={createComment.isError}>
				<p class="w-fit mx-auto text-sm text-destructive my-2">
					{createComment.error?.message ||
						"An error occurred while creating the comment."}
				</p>
			</Show>
			<footer class="flex items-center justify-between border-t border-border px-4 py-2 bg-[#f5f5f5] dark:bg-[#1a1f2e]">
				<Avatar class="size-8">
					<AvatarImage src={props.user?.image ?? undefined} />
					<AvatarFallback class="capitalize bg-background">
						{props.user?.username.charAt(0)}
					</AvatarFallback>
				</Avatar>
				<Button
					type="submit"
					class="rounded-xs"
					disabled={!value().trim().length}
				>
					<Show
						when={createComment.isPending}
						fallback={<i class="ri-send-plane-line text-base mr-1" />}
					>
						<Swirling class="size-4 mr-1" />
					</Show>
					Post Comment
				</Button>
			</footer>
		</form>
	);
}
