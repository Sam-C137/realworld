import { createForm } from "@tanstack/solid-form";
import { useMutation, useQueryClient } from "@tanstack/solid-query";
import { createFileRoute, Link } from "@tanstack/solid-router";
import { createSignal, Show } from "solid-js";
import { Button } from "~/components/ui/button.tsx";
import {
	Field,
	FieldError,
	FieldGroup,
	FieldLabel,
} from "~/components/ui/field.tsx";
import { Input } from "~/components/ui/input.tsx";
import { RegisterSchema } from "~/lib/validation.ts";
import { RegisterOptions } from "~/queries/user.ts";

export const Route = createFileRoute("/register")({
	component: RegisterPage,
});

function RegisterPage() {
	const qc = useQueryClient();
	const navigate = Route.useNavigate();
	const [error, setError] = createSignal<string | null>(null);
	const register = useMutation(() => RegisterOptions);
	const form = createForm(() => ({
		defaultValues: {
			username: "",
			email: "",
			password: "",
		},
		validators: {
			onSubmit: RegisterSchema,
		},
		onSubmit: async ({ value }) => {
			try {
				setError(null);
				await register.mutateAsync({ user: value });
				qc.clear();
				await navigate({
					to: "/",
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
			<h1 class="text-4xl text-center mx-auto">Sign Up</h1>
			<Link
				to="/login"
				class="block text-center mx-auto text-primary hover:underline mb-4 mt-2"
			>
				Have an account?
			</Link>
			<Show when={error()} keyed>
				<p class="text-destructive text-center mx-auto capitalize">{error()}</p>
			</Show>
			<form
				onSubmit={async (e) => {
					e.preventDefault();
					await form.handleSubmit();
				}}
				class="mx-auto max-w-md space-y-3"
			>
				<FieldGroup class="gap-2">
					<form.Field
						name="username"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Username
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										placeholder="fayevalentine"
										class="rounded-none h-max md:text-xl"
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
						name="email"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Email
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										placeholder="faye@mars.bb"
										type="email"
										autocomplete="email"
										class="rounded-none h-max md:text-xl"
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
						name="password"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Password
									</FieldLabel>
									<Input
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => field().handleChange(e.target.value)}
										aria-invalid={isInvalid}
										type="password"
										placeholder="********"
										class="rounded-none h-max md:text-xl"
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
					class="float-right text-lg rounded-sm"
					disabled={form.state.isSubmitting}
				>
					Sign Up
				</Button>
			</form>
		</main>
	);
}
