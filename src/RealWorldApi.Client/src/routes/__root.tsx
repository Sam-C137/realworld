import {
	ColorModeProvider,
	ColorModeScript,
	createLocalStorageManager,
} from "@kobalte/core";
import { createRootRouteWithContext, Outlet } from "@tanstack/solid-router";
import { TanStackRouterDevtools } from "@tanstack/solid-router-devtools";
import "@fontsource/inter/400.css";
import { Suspense } from "solid-js";

import Header from "../components/header.tsx";
import "../styles.css";
import { keys } from "~/lib/constants.ts";

export const Route = createRootRouteWithContext()({
	component: RootComponent,
});

function RootComponent() {
	const storageManager = createLocalStorageManager(keys.LocalStorage.Theme);
	return (
		<>
			<ColorModeScript storageType={storageManager.type} />
			<ColorModeProvider storageManager={storageManager}>
				<Suspense>
					<Header />
					<Outlet />
					<TanStackRouterDevtools />
				</Suspense>
			</ColorModeProvider>
		</>
	);
}
