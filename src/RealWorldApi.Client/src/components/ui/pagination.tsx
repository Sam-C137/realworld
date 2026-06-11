import { Link, type LinkProps, useSearch } from "@tanstack/solid-router";
import { For, type JSX, splitProps } from "solid-js";
import { type ButtonProps, buttonVariants } from "~/components/ui/button";
import { usePaginationDisplay } from "~/hooks/use-pagination-display.ts";
import { cn } from "~/lib/utils";

type PaginationProps = JSX.HTMLAttributes<HTMLElement> & {
	class?: string | undefined;
};

const Pagination = (props: PaginationProps) => {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<nav
			aria-label="pagination"
			data-slot="pagination"
			data-testid="pagination"
			class={cn("mx-auto flex w-full justify-center", local.class)}
			{...others}
		/>
	);
};

type PaginationContentProps = JSX.HTMLAttributes<HTMLUListElement> & {
	class?: string | undefined;
};

function PaginationContent(props: PaginationContentProps) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<ul
			data-slot="pagination-content"
			data-testid="pagination-content"
			class={cn("flex items-center gap-0.5", local.class)}
			{...others}
		/>
	);
}

const PaginationItems = PaginationContent;

type PaginationItemProps = JSX.LiHTMLAttributes<HTMLLIElement> & {
	class?: string | undefined;
};

function PaginationItem(props: PaginationItemProps) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<li
			data-slot="pagination-item"
			data-testid="pagination-item"
			class={local.class}
			{...others}
		/>
	);
}

type PaginationLinkSize = NonNullable<ButtonProps["size"]>;

type PaginationLinkProps = LinkProps & {
	class?: string | undefined;
	isActive?: boolean;
	size?: PaginationLinkSize;
	children?: JSX.Element;
};

function PaginationLink(props: PaginationLinkProps) {
	const [local, others] = splitProps(props, [
		"class",
		"isActive",
		"size",
		"children",
	]);

	return (
		<Link
			aria-current={local.isActive ? "page" : undefined}
			data-slot="pagination-link"
			data-testid="pagination-link"
			data-active={local.isActive ? "" : undefined}
			class={cn(
				"text-primary",
				buttonVariants({
					variant: "outline",
					size: local.size ?? "icon",
				}),
				local.isActive && "bg-primary text-primary-foreground",
				local.class,
			)}
			{...others}
		>
			{local.children}
		</Link>
	);
}

type PaginationPreviousProps = PaginationLinkProps & {
	text?: string;
};

const PaginationPrevious = (props: PaginationPreviousProps) => {
	const [local, others] = splitProps(props, ["class", "children", "text"]);

	return (
		<PaginationLink
			aria-label="Go to previous page"
			size="default"
			class={cn("gap-0 pl-1.5", local.class)}
			{...others}
		>
			{local.children ?? (
				<>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="size-4"
						aria-hidden="true"
					>
						<path d="M15 6l-6 6l6 6" />
					</svg>
					<span class="hidden sm:block">{local.text ?? "Previous"}</span>
				</>
			)}
		</PaginationLink>
	);
};

type PaginationNextProps = PaginationLinkProps & {
	text?: string;
};

const PaginationNext = (props: PaginationNextProps) => {
	const [local, others] = splitProps(props, ["class", "children", "text"]);

	return (
		<PaginationLink
			aria-label="Go to next page"
			size="default"
			class={cn("gap-0 pr-1.5", local.class)}
			{...others}
		>
			{local.children ?? (
				<>
					<span class="hidden sm:block">{local.text ?? "Next"}</span>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="size-4"
						aria-hidden="true"
					>
						<path d="M9 6l6 6l-6 6" />
					</svg>
				</>
			)}
		</PaginationLink>
	);
};

type PaginationEllipsisProps = JSX.HTMLAttributes<HTMLSpanElement> & {
	class?: string | undefined;
};

const PaginationEllipsis = (props: PaginationEllipsisProps) => {
	const [local, others] = splitProps(props, ["class"]);

	return (
		<span
			aria-hidden
			data-slot="pagination-ellipsis"
			data-testid="pagination-ellipsis"
			class={cn(
				"flex size-8 items-center justify-center [&_svg:not([class*='size-'])]:size-4",
				local.class,
			)}
			{...others}
		>
			<svg
				xmlns="http://www.w3.org/2000/svg"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				stroke-linecap="round"
				stroke-linejoin="round"
				class="size-4"
				aria-hidden="true"
			>
				<circle cx="12" cy="12" r="1" />
				<circle cx="19" cy="12" r="1" />
				<circle cx="5" cy="12" r="1" />
			</svg>
			<span class="sr-only">More pages</span>
		</span>
	);
};

interface PaginationBarProps {
	total?: number;
	class?: string;
}

export function PaginationBar(props: PaginationBarProps) {
	const params = useSearch({ strict: false });
	const [{ page: currentPage = 1, limit = 10 }, search] = splitProps(params(), [
		"page",
		"limit",
	]);
	const totalPages = props.total ? Math.ceil(props.total / limit) : 0;
	const { pages, showLeftEllipsis, showRightEllipsis } = usePaginationDisplay({
		currentPage,
		totalPages,
		paginationItemsToDisplay: 5,
	});

	return (
		<Pagination class={cn("w-fit", props.class)}>
			<PaginationContent class="gap-0">
				<PaginationItem>
					<PaginationPrevious
						class="aria-disabled:pointer-events-none aria-disabled:opacity-50 rounded-r-none"
						to="."
						search={{
							...search,
							page: currentPage - 1,
						}}
						resetScroll={false}
						aria-disabled={currentPage === 1}
						text=""
					/>
				</PaginationItem>
				{showLeftEllipsis && (
					<PaginationItem>
						<PaginationEllipsis class="border-b border-t" />
					</PaginationItem>
				)}
				<For each={pages}>
					{(page) => (
						<PaginationItem class="*:rounded-none *:border-l-0">
							<PaginationLink
								to="."
								search={{
									...search,
									page,
								}}
								isActive={page === currentPage}
								resetScroll={false}
								class="cursor-pointer"
							>
								{page}
							</PaginationLink>
						</PaginationItem>
					)}
				</For>
				{showRightEllipsis && (
					<PaginationItem>
						<PaginationEllipsis class="border-b border-t" />
					</PaginationItem>
				)}
				<PaginationItem>
					<PaginationNext
						class="aria-disabled:pointer-events-none aria-disabled:opacity-50 rounded-l-none"
						to="."
						search={{
							...search,
							page: currentPage + 1,
						}}
						resetScroll={false}
						aria-disabled={currentPage === totalPages}
						text=""
					/>
				</PaginationItem>
			</PaginationContent>
		</Pagination>
	);
}

export {
	Pagination,
	PaginationContent,
	PaginationItems,
	PaginationItem,
	PaginationLink,
	PaginationEllipsis,
	PaginationPrevious,
	PaginationNext,
};
