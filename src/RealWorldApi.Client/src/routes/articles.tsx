import { createFileRoute } from "@tanstack/solid-router";
import { createResource } from "solid-js";

export const Route = createFileRoute("/articles")({
	component: RouteComponent,
});

async function fetchArticles() {
	return fetch("/api/v1/articles").then((res) => res.json());
}

function RouteComponent() {
	const [articles] = createResource(fetchArticles);
	return (
		<div class="max-w-full mx-auto">
			Hello "/articles"!
			<pre>{JSON.stringify(articles(), null, 2)}</pre>
		</div>
	);
}
