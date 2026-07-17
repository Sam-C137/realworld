import type { QueryOptions } from "@tanstack/solid-query";
import { api } from "~/lib/api.ts";
import { keys } from "~/lib/constants.ts";
import type { Profile } from "~/types/profile.ts";

export function GetProfileOptionsFn(username: string) {
	return {
		queryKey: [keys.Query.Profile, username],
		queryFn: async () => {
			return api
				.get<Record<"profile", Profile>>(`/api/v1/profiles/${username}`, {
					context: { authMode: "optional" },
				})
				.json();
		},
	} satisfies QueryOptions<Record<"profile", Profile>>;
}
