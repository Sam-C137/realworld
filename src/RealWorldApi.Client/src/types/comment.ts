export interface Comment {
	id: string;
	body: string;
	bodyJson: null;
	author: {
		username: string;
		bio: null;
		image: null;
		following: true;
	};
	createdAt: string;
	updatedAt: string;
}
