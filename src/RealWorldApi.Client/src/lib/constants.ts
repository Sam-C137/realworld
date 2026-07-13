export const time = {
	Millisecond: 1,
	Second: 1000,
	Minute: 60_000,
	Hour: 3_600_000,
	Day: 86_400_000,
	Week: 604_800_000,
	Month: 2_592_000_000,
	Year: 31_536_000_000,
} as const;

export const size = {
	KB: 1024,
	MB: 1_048_576,
	GB: 1_073_741_824,
	TB: 1_099_511_627_776,
} as const;

export const keys = {
	LocalStorage: {
		Theme: "theme",
		AccessToken: "accessToken",
		CsrfToken: "csrfToken",
		TokenExpiresAt: "tokenExpiresAt",
	},
	Query: {
		CurrentUser: "currentUser",
		Articles: "articles",
		Article: "article",
		Feed: "feed",
		Tags: "tags",
	},
} as const;
