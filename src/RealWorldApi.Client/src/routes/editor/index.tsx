import { createForm } from "@tanstack/solid-form";
import { type QueryClient, useMutation } from "@tanstack/solid-query";
import { createFileRoute, redirect } from "@tanstack/solid-router";
import { type } from "arktype";
import { createSignal, Show } from "solid-js";
import { Button } from "~/components/ui/button.tsx";
import {
	Field,
	FieldError,
	FieldGroup,
	FieldLabel,
} from "~/components/ui/field.tsx";
import { Input } from "~/components/ui/input.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { CreateArticleSchema } from "~/lib/validation.ts";
import {
	CreateArticleOptions,
	EditArticleOptions,
	GetArticleOptionsFn,
} from "~/queries/article.ts";
import { CurrentUserOptions } from "~/queries/user.ts";

export const Route = createFileRoute("/editor/")({
	component: EditorPage,
	beforeLoad: async ({ context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			const user = await queryClient.ensureQueryData(CurrentUserOptions);
			return { user };
		} catch (e) {
			void e;
			throw redirect({
				to: "/login",
			});
		}
	},
	loaderDeps: ({ search }) => ({ slug: search.slug }),
	loader: async ({ deps: { slug }, context }) => {
		try {
			const { queryClient } = context as typeof context & {
				queryClient: QueryClient;
			};
			if (!slug) return { article: null };
			const article = await queryClient.fetchQuery(GetArticleOptionsFn(slug));
			return { article };
		} catch (e) {
			void e;
			return { article: null };
		}
	},
	validateSearch: type({
		"slug?": "string",
	}),
});

function EditorPage() {
	const loaderData = Route.useLoaderData();
	const navigate = Route.useNavigate();
	const [error, setError] = createSignal<string | null>(null);
	const createArticle = useMutation(() => CreateArticleOptions);
	const editArticle = useMutation(() => EditArticleOptions);

	const form = createForm(() => ({
		defaultValues: {
			title: loaderData().article?.article.title || "",
			description: loaderData().article?.article.description || "",
			body: loaderData().article?.article.body || "",
			tagList: loaderData().article?.article.tagList || [],
		},
		validators: {
			onSubmit: CreateArticleSchema,
		},
		onSubmit: async ({ value }) => {
			try {
				setError(null);
				let slug: string;
				if (loaderData().article) {
					const { article } = await editArticle.mutateAsync([
						loaderData().article?.article.slug as string,
						CreateArticleSchema.assert(value),
					]);
					slug = article.slug;
				} else {
					const { article } = await createArticle.mutateAsync(
						CreateArticleSchema.assert(value),
					);
					slug = article.slug;
				}
				await navigate({
					to: "/article/$slug",
					params: { slug },
				});
			} catch (e) {
				if (e instanceof Error) {
					setError(e.message);
				}
			}
		},
	}));

	return (
		<main class="min-h-dvh w-full text-foreground">
			<Show when={error()} keyed>
				<p class="text-destructive text-center mx-auto capitalize">{error()}</p>
			</Show>
			<form
				onSubmit={async (e) => {
					e.preventDefault();
					await form.handleSubmit();
				}}
				class="mx-auto max-w-3xl space-y-3"
			>
				<FieldGroup class="gap-2">
					<form.Field
						name="title"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Title
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										placeholder="Article title"
										type="text"
										class="rounded-none h-max md:text-lg"
									/>
									<FieldError
										class="text-base"
										errors={field().state.meta.errors}
									/>
								</Field>
							);
						}}
					/>
					<form.Field
						name="description"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Description
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										type="text"
										placeholder="What is this article about?"
										class="rounded-none h-max md:text-lg"
									/>
									<FieldError
										class="text-base"
										errors={field().state.meta.errors}
									/>
								</Field>
							);
						}}
					/>
					<form.Field
						name="body"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Body
									</FieldLabel>
									<textarea
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										placeholder="Write your article (in markdown)"
										class="rounded-none border border-input h-40 md:text-lg px-2.5 py-1"
									/>
									<FieldError
										class="text-base"
										errors={field().state.meta.errors}
									/>
								</Field>
							);
						}}
					/>
					<form.Field
						name="tagList"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Tags
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value.join(", ")}
										onBlur={field().handleBlur}
										onChange={(e) =>
											field().handleChange((prev) => [
												...prev,
												...e.target.value
													.split(",")
													.map((v) => v.trim())
													.filter(Boolean),
											])
										}
										aria-invalid={isInvalid}
										placeholder="comma, separated, tags"
										type="text"
										class="rounded-none h-max md:text-lg"
									/>
									<FieldError
										class="text-base"
										errors={field().state.meta.errors}
									/>
								</Field>
							);
						}}
					/>
				</FieldGroup>
				<Button
					type="submit"
					class="float-right text-lg rounded-xs"
					disabled={form.state.isSubmitting}
				>
					<Show when={createArticle.isPending}>
						<Swirling class="size-4 mr-1" />
					</Show>
					<Show when={loaderData().article} fallback={"Publish Article"}>
						Update Article
					</Show>
				</Button>
			</form>
		</main>
	);
}
