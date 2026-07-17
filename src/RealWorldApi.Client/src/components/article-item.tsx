import { useMutation } from "@tanstack/solid-query";
import { Link, useNavigate } from "@tanstack/solid-router";
import { format } from "date-fns";
import { For, Show } from "solid-js";
import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "~/components/ui/avatar.tsx";
import { Button } from "~/components/ui/button.tsx";
import { Skeleton } from "~/components/ui/skeleton.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { cn } from "~/lib/utils.ts";
import {
	FavoriteArticleOptions,
	UnfavoriteArticleOptions,
} from "~/queries/article.ts";
import type { Article } from "~/types/article.ts";
import type { User } from "~/types/user.ts";

interface ArticleItemProps {
	article: Article;
	user: User | null;
}

export function ArticleItem(props: ArticleItemProps) {
	const navigate = useNavigate();
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
							to="/profile/$username"
							params={{
								username: props.article.article.author.username,
							}}
							class="text-primary leading-none font-medium hover:underline"
						>
							{props.article.article.author.username}
						</Link>
						<time class="text-muted-foreground text-xs leading-none">
							{format(
								new Date(props.article.article.createdAt),
								"MMM dd, yyyy",
							)}
						</time>
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

export function ArticleItemLoading() {
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
