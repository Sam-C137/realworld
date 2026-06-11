import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { time } from "~/lib/constants.ts";

export function cn(...inputs: ClassValue[]) {
	return twMerge(clsx(inputs));
}

export async function sleep(ms = time.Second * 0.5) {
	return new Promise((resolve) => setTimeout(resolve, ms));
}
