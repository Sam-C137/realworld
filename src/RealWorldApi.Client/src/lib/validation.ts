import { type } from "arktype";

export const ProblemDetails = type({
	detail: "string | null",
	instance: "string | null",
	status: "string",
	title: "string | null",
});

export const ValidationProblemDetails = type({
	"...": ProblemDetails,
	errors: "Record<string, string[]>",
});
