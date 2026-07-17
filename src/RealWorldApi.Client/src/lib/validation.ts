import { type } from "arktype";
import { size } from "~/lib/constants.ts";

export const ProblemDetails = type({
	"detail?": "string | null",
	"title?": "string | null",
	status: "string | number",
});

export const ValidationProblemDetails = type({
	"...": ProblemDetails,
	errors: "Record<string, string[]>",
});

export const LoginSchema = type({
	email: "string.email",
	password: "string.alphanumeric>=6",
});

export const RegisterSchema = type({
	"...": LoginSchema,
	username: "string.trim |> (3 <= string <= 20)",
});

export const CreateArticleSchema = type({
	title: "string.trim |> (3 <= string <= 255)",
	body: "string.trim |> (string > 0)",
	description: "string.trim |> (5 <= string <= 510)",
	tagList: "(string.trim |> string > 0)[]",
});

export const UpdateUserSchema = type({
	bio: "string.trim |> (3 <= string <= 255)",
	email: "string.email",
	username: "string.trim |> (3 <= string <= 20)",
	image: type.string
		.or(
			type("File")
				.narrow((f) =>
					["image/jpeg", "image/png", "image/webp"].includes(f.type),
				)
				.describe("a valid image file (jpeg, png, webp)")
				.narrow((f) => f.size > 0 && f.size < size.MB * 5)
				.describe("must not be empty or more than 5MB"),
		)
		.pipe((f) => (f instanceof File ? f : undefined)),
});
