import {
	type Component,
	createEffect,
	onCleanup,
	type ParentProps,
	children as resolveChildren,
} from "solid-js";

export interface InfiniteScrollProps {
	isLoading: boolean;
	hasMore: boolean;
	next: () => unknown;
	threshold?: number;
	root?: Element | Document | null;
	rootMargin?: `${number}px`;
	reverse?: boolean;
	class?: string;
}

const InfiniteScrollContainer: Component<
	ParentProps<
		InfiniteScrollProps & {
			ref?: (el: HTMLDivElement) => void;
		}
	>
> = (props) => {
	return (
		<div class={props.class} ref={props.ref}>
			{props.children}
			<InfiniteScrollEntry
				isLoading={props.isLoading}
				hasMore={props.hasMore}
				next={props.next}
				threshold={props.threshold}
				root={props.root}
				rootMargin={props.rootMargin}
				reverse={props.reverse}
			>
				<div class="h-2 w-2" />
			</InfiniteScrollEntry>
		</div>
	);
};

function InfiniteScrollEntry(props: ParentProps<InfiniteScrollProps>) {
	// resolveChildren gives us the actual resolved child nodes (already real DOM elements)
	const resolved = resolveChildren(() => props.children);

	createEffect(() => {
		const threshold = props.threshold ?? 1;
		const root = props.root ?? null;
		const rootMargin = props.rootMargin ?? "200px";
		const isLoading = props.isLoading;
		const hasMore = props.hasMore;
		const next = props.next;
		const reverse = props.reverse;

		let safeThreshold = threshold;
		if (threshold < 0 || threshold > 1) {
			console.warn(
				"threshold should be between 0 and 1. You are exceed the range. will use default value: 1",
			);
			safeThreshold = 1;
		}

		/**
		 * While loading or after the final page, next() must not fire.
		 * Safe, since any prior observer gets disconnected via onCleanup below.
		 */
		if (isLoading || !hasMore) return;

		const list = resolved
			.toArray()
			.filter((child): child is Element => child instanceof Element);

		if (list.length === 0) {
			if (import.meta.env?.DEV) {
				console.warn("You should use a valid element with InfiniteScroll");
			}
			return;
		}

		const target = reverse ? list[0] : list[list.length - 1];
		if (!target) return;

		/**
		 * Re-created whenever hasMore, next, threshold, root, or rootMargin change,
		 * since the effect re-runs and the previous observer is disconnected first.
		 */
		const observer = new IntersectionObserver(
			(entries) => {
				if (entries[0].isIntersecting && hasMore) {
					observer.unobserve(target);
					next();
				}
			},
			{ threshold: safeThreshold, root, rootMargin },
		);

		observer.observe(target);

		onCleanup(() => {
			observer.disconnect();
		});
	});

	return <>{resolved()}</>;
}

export { InfiniteScrollContainer, InfiniteScrollEntry };
