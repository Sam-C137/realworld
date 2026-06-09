import { createRootRouteWithContext, Outlet } from "@tanstack/solid-router";
import { TanStackRouterDevtools } from "@tanstack/solid-router-devtools";

import "@fontsource/inter/400.css";
import { Suspense } from "solid-js";

import Header from "../components/Header";
import "../styles.css";

export const Route = createRootRouteWithContext()({
	component: RootComponent,
});

function RootComponent() {
	return (
		<Suspense>
			<Header />
			<Outlet />
			<TanStackRouterDevtools />
		</Suspense>
	);
}
