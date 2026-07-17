import { createForm } from "@tanstack/solid-form";
import { type QueryClient, useMutation } from "@tanstack/solid-query";
import { createFileRoute, redirect } from "@tanstack/solid-router";
import { createSignal, onCleanup, Show } from "solid-js";
import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "~/components/ui/avatar.tsx";
import { Button } from "~/components/ui/button.tsx";
import {
	Field,
	FieldError,
	FieldGroup,
	FieldLabel,
} from "~/components/ui/field.tsx";
import { Input } from "~/components/ui/input.tsx";
import { Swirling } from "~/components/ui/swirling.tsx";
import { UpdateUserSchema } from "~/lib/validation.ts";
import { CurrentUserOptions, UpdateUserOptions } from "~/queries/user.ts";

export const Route = createFileRoute("/settings/")({
	beforeLoad: async ({ context }) => {
		try {
			const { queryClient } = context as { queryClient: QueryClient };
			const user = await queryClient.fetchQuery(CurrentUserOptions);
			return { user };
		} catch (e) {
			void e;
			throw redirect({
				to: "/login",
			});
		}
	},
	component: SettingsPage,
});

function SettingsPage() {
	const routeContext = Route.useRouteContext();
	const updateUser = useMutation(() => UpdateUserOptions);
	const [error, setError] = createSignal<string | null>(null);
	const [saved, setSaved] = createSignal(false);
	const [imagePreview, setImagePreview] = createSignal(
		routeContext().user.user.image ?? "",
	);
	let objectUrl: string | undefined;

	const setPreviewFile = (file: File) => {
		if (objectUrl) URL.revokeObjectURL(objectUrl);
		objectUrl = URL.createObjectURL(file);
		setImagePreview(objectUrl);
	};

	onCleanup(() => {
		if (objectUrl) URL.revokeObjectURL(objectUrl);
	});

	const form = createForm(() => ({
		defaultValues: {
			bio: routeContext().user.user.bio ?? "",
			email: routeContext().user.user.email,
			username: routeContext().user.user.username,
			image: (routeContext().user.user.image ?? "") as string | File,
		},
		validators: {
			onSubmit: UpdateUserSchema,
		},
		onSubmit: async ({ value }) => {
			try {
				setError(null);
				setSaved(false);
				const { user } = await updateUser.mutateAsync(
					UpdateUserSchema.assert(value),
				);
				if (user.image) setImagePreview(user.image);
				setSaved(true);
			} catch (e) {
				if (e instanceof Error) setError(e.message);
			}
		},
	}));

	return (
		<main class="min-h-dvh w-full text-foreground">
			<h1 class="text-4xl text-center mx-auto mb-4">Your Settings</h1>
			<Show when={error()} keyed>
				<p class="text-destructive text-center mx-auto mb-3 capitalize">
					{error()}
				</p>
			</Show>
			<Show when={saved()}>
				<output class="block text-primary text-center mx-auto mb-3">
					Your settings have been updated.
				</output>
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
						name="image"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid} class="items-center">
									<FieldLabel
										for={field().name}
										class="group relative cursor-pointer rounded-full w-fit!"
									>
										<Avatar class="size-28 border-2 border-input transition group-hover:border-primary group-focus-within:ring-3 group-focus-within:ring-ring/50">
											<AvatarImage
												src={imagePreview() || undefined}
												alt={`${form.state.values.username}'s profile image`}
												class="object-cover"
											/>
											<AvatarFallback class="text-3xl capitalize">
												{form.state.values.username.charAt(0)}
											</AvatarFallback>
										</Avatar>
										<span class="size-28 absolute inset-0 flex items-center justify-center rounded-full bg-black/55 text-sm font-medium text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
											<i class="ri-camera-line mr-1 text-base" /> Change
										</span>
									</FieldLabel>
									<input
										id={field().name}
										name={field().name}
										type="file"
										accept="image/jpeg,image/png,image/webp"
										class="sr-only"
										onBlur={field().handleBlur}
										onChange={(e) => {
											const file = e.currentTarget.files?.[0];
											if (!file) return;
											field().handleChange(file);
											setPreviewFile(file);
											setSaved(false);
										}}
										aria-invalid={isInvalid}
										aria-describedby={`${field().name}-help`}
									/>
									<p
										id={`${field().name}-help`}
										class="text-sm text-muted-foreground text-center"
									>
										JPEG, PNG, or WebP. Maximum 5MB.
									</p>
									<FieldError
										class="text-base text-center"
										errors={field().state.meta.errors}
									/>
								</Field>
							);
						}}
					/>
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
										onChange={(e) => {
											field().handleChange(e.target.value);
											setSaved(false);
										}}
										aria-invalid={isInvalid}
										autocomplete="username"
										placeholder="Your username"
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
										onChange={(e) => {
											field().handleChange(e.target.value);
											setSaved(false);
										}}
										aria-invalid={isInvalid}
										type="email"
										autocomplete="email"
										placeholder="you@example.com"
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
						name="bio"
						children={(field) => {
							const isInvalid =
								field().state.meta.isTouched && !field().state.meta.isValid;
							return (
								<Field data-invalid={isInvalid}>
									<FieldLabel for={field().name} class="text-lg">
										Bio
									</FieldLabel>
									<textarea
										id={field().name}
										name={field().name}
										value={field().state.value}
										onBlur={field().handleBlur}
										onChange={(e) => {
											field().handleChange(e.target.value);
											setSaved(false);
										}}
										aria-invalid={isInvalid}
										placeholder="A short bio about you"
										class="min-h-28 rounded-none border border-input bg-transparent px-2.5 py-1 text-base outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 md:text-xl"
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
					<Show when={updateUser.isPending}>
						<Swirling class="size-4 mr-1" />
					</Show>
					Update Settings
				</Button>
			</form>
		</main>
	);
}
