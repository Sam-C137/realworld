import { type } from "arktype";

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
	username: "3 <= string <= 20",
});
