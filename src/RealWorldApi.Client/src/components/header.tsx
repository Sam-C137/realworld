import { useColorMode } from "@kobalte/core";
import { useQuery } from "@tanstack/solid-query";
import { Link, useLocation } from "@tanstack/solid-router";
import { Show } from "solid-js";
import { Button } from "~/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { CurrentUserOptionsFn } from "~/queries/user.ts";

export default function Header() {
	const location = useLocation();
	const user = useQuery(() => CurrentUserOptionsFn(location().pathname));
	return (
		<header class="py-4 px-4 sm:px-10">
			<nav class="flex items-center justify-between max-w-7xl mx-auto">
				<h2 class="m-0 shrink-0 text-base font-semibold tracking-tight">
					<Link to="/" class="font-bold text-2xl text-primary">
						conduit
					</Link>
				</h2>
				<div class="flex items-center gap-4">
					<Show
						when={user.isSuccess}
						fallback={
							<>
								<Link
									to="/"
									class="text-muted-foreground"
									activeProps={{ class: "text-foreground!" }}
								>
									Home
								</Link>
								<Link
									to="/login"
									class="text-muted-foreground"
									activeProps={{ class: "text-foreground!" }}
								>
									Sign in
								</Link>
								<Link
									to="/register"
									class="text-muted-foreground"
									activeProps={{ class: "text-foreground!" }}
								>
									Sign up
								</Link>
							</>
						}
					>
						<Link
							to="/logout"
							class="text-muted-foreground"
							activeProps={{ class: "text-foreground!" }}
							preload={false}
						>
							Sign Out
						</Link>
					</Show>
					<ThemeToggle />
				</div>
			</nav>
		</header>
	);
}

export function ThemeToggle() {
	const { setColorMode } = useColorMode();

	function handleModeChange(mode: "light" | "dark" | "system") {
		setColorMode(mode);
		document.documentElement.classList.remove("light", "dark", "system");
		document.documentElement.classList.add(mode);
	}

	return (
		<DropdownMenu>
			<DropdownMenuTrigger
				as={Button<"button">}
				variant="ghost"
				size="sm"
				class="h-8 px-0.5 py-2 grid place-items-center cursor-pointer"
			>
				<i class="ri-sun-line size-6 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
				<i class="ri-moon-line absolute size-6 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
				<span class="sr-only">Toggle theme</span>
			</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuItem onSelect={() => handleModeChange("light")}>
					<i class="ri-sun-line mr-2 size-4" />
					<span>Light</span>
				</DropdownMenuItem>
				<DropdownMenuItem onSelect={() => handleModeChange("dark")}>
					<i class="ri-moon-line mr-2 size-4" />
					<span>Dark</span>
				</DropdownMenuItem>
				<DropdownMenuItem onSelect={() => handleModeChange("system")}>
					<i class="ri-macbook-line mr-2 size-4" />
					<span>System</span>
				</DropdownMenuItem>
			</DropdownMenuContent>
		</DropdownMenu>
	);
}
