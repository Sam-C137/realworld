export interface Article {
	article: {
		slug: string;
		title: string;
		description: string;
		body: string;
		bodyJson: string | null;
		tagList: string[];
		createdAt: string;
		updatedAt: string;
		favorited: boolean;
		favoritesCount: number;
		author: {
			username: string;
			bio: string | null;
			image: string | null;
			following: boolean;
		};
	};
}
