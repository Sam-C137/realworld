export interface User {
	email: string;
	username: string;
	token: string;
	csrfToken?: string;
	bio: string | null;
	image: string | null;
}
