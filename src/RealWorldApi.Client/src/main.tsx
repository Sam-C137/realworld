import { QueryClientProvider } from "@tanstack/solid-query";
import { RouterProvider } from "@tanstack/solid-router";
import { render } from "solid-js/web";
import { queryClient } from "~/integrations/tanstack-query/root-provider.tsx";

import { getRouter } from "./router";

const root = document.getElementById("root");

if (!root) {
	throw new Error("Root element #root was not found.");
}

render(
	() => (
		<QueryClientProvider client={queryClient}>
			<RouterProvider router={getRouter()} />
		</QueryClientProvider>
	),
	root,
);
