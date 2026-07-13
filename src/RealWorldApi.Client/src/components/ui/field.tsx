import { cva, type VariantProps } from "class-variance-authority";
import {
	type ComponentProps,
	createMemo,
	For,
	Show,
	splitProps,
} from "solid-js";
import { Label } from "~/components/ui/label";
import { Separator } from "~/components/ui/separator";
import { cn } from "~/lib/utils";

function FieldSet(props: ComponentProps<"fieldset">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<fieldset
			data-slot="field-set"
			class={cn(
				"flex flex-col gap-4 has-[>[data-slot=checkbox-group]]:gap-3 has-[>[data-slot=radio-group]]:gap-3",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldLegend(
	props: ComponentProps<"legend"> & { variant?: "legend" | "label" },
) {
	const [local, others] = splitProps(props, ["class", "variant"]);
	return (
		<legend
			data-slot="field-legend"
			data-variant={local.variant}
			class={cn(
				"mb-1.5 font-medium data-[variant=label]:text-sm data-[variant=legend]:text-base",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldGroup(props: ComponentProps<"div">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<div
			data-slot="field-group"
			class={cn(
				"group/field-group @container/field-group flex w-full flex-col gap-5 data-[slot=checkbox-group]:gap-3 *:data-[slot=field-group]:gap-4",
				local.class,
			)}
			{...others}
		/>
	);
}

const fieldVariants = cva(
	"group/field flex w-full gap-2 data-[invalid=true]:text-destructive",
	{
		variants: {
			orientation: {
				vertical: "flex-col *:w-full [&>.sr-only]:w-auto",
				horizontal:
					"flex-row items-center has-[>[data-slot=field-content]]:items-start *:data-[slot=field-label]:flex-auto has-[>[data-slot=field-content]]:[&>[role=checkbox],[role=radio]]:mt-px",
				responsive:
					"flex-col *:w-full @md/field-group:flex-row @md/field-group:items-center @md/field-group:*:w-auto @md/field-group:has-[>[data-slot=field-content]]:items-start @md/field-group:*:data-[slot=field-label]:flex-auto [&>.sr-only]:w-auto @md/field-group:has-[>[data-slot=field-content]]:[&>[role=checkbox],[role=radio]]:mt-px",
			},
		},
		defaultVariants: {
			orientation: "vertical",
		},
	},
);

function Field(
	props: ComponentProps<"div"> & VariantProps<typeof fieldVariants>,
) {
	const [local, others] = splitProps(props, ["class", "orientation"]);
	return (
		// biome-ignore lint/a11y/useSemanticElements: reusable component
		<div
			role="group"
			data-slot="field"
			data-orientation={local.orientation ?? "vertical"}
			class={cn(fieldVariants({ orientation: local.orientation }), local.class)}
			{...others}
		/>
	);
}

function FieldContent(props: ComponentProps<"div">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<div
			data-slot="field-content"
			class={cn(
				"group/field-content flex flex-1 flex-col gap-0.5 leading-snug",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldLabel(props: ComponentProps<typeof Label>) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<Label
			data-slot="field-label"
			class={cn(
				"group/field-label peer/field-label flex w-fit gap-2 leading-snug group-data-[disabled=true]/field:opacity-50 has-data-checked:border-primary/30 has-data-checked:bg-primary/5 has-[>[data-slot=field]]:rounded-lg has-[>[data-slot=field]]:border *:data-[slot=field]:p-2.5 dark:has-data-checked:border-primary/20 dark:has-data-checked:bg-primary/10",
				"has-[>[data-slot=field]]:w-full has-[>[data-slot=field]]:flex-col",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldTitle(props: ComponentProps<"div">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<div
			data-slot="field-label"
			class={cn(
				"flex w-fit items-center gap-2 text-sm font-medium group-data-[disabled=true]/field:opacity-50",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldDescription(props: ComponentProps<"p">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		<p
			data-slot="field-description"
			class={cn(
				"text-left text-sm leading-normal font-normal text-muted-foreground group-has-data-horizontal/field:text-balance [[data-variant=legend]+&]:-mt-1.5",
				"last:mt-0 nth-last-2:-mt-1",
				"[&>a]:underline [&>a]:underline-offset-4 [&>a:hover]:text-primary",
				local.class,
			)}
			{...others}
		/>
	);
}

function FieldSeparator(props: ComponentProps<"div">) {
	const [local, others] = splitProps(props, ["class", "children"]);

	return (
		<div
			data-slot="field-separator"
			data-content={!!props.children}
			class={cn(
				"relative -my-2 h-5 text-sm group-data-[variant=outline]/field-group:-mb-2",
				local.class,
			)}
			{...others}
		>
			<Separator class="absolute inset-0 top-1/2" />
			<Show when={props.children}>
				<span
					class="relative mx-auto block w-fit bg-background px-2 text-muted-foreground"
					data-slot="field-separator-content"
				>
					{props.children}
				</span>
			</Show>
		</div>
	);
}

function FieldError(
	props: ComponentProps<"div"> & {
		errors?: Array<{ message?: string } | undefined>;
	},
) {
	const [local, others] = splitProps(props, ["class", "children", "errors"]);

	const content = createMemo(() => {
		if (local.children) {
			return local.children;
		}

		if (!local.errors?.length) {
			return null;
		}

		const uniqueErrors = [
			...new Map(local.errors.map((error) => [error?.message, error])).values(),
		];

		if (uniqueErrors.length && uniqueErrors.length === 1) {
			return uniqueErrors[0]?.message;
		}

		return (
			<ul class="ml-4 flex list-disc flex-col gap-1">
				<For each={uniqueErrors}>{(error) => <li>{error?.message}</li>}</For>
			</ul>
		);
	});

	return (
		<Show when={content()}>
			{(value) => (
				<div
					role="alert"
					data-slot="field-error"
					class={cn("text-sm font-normal text-destructive", local.class)}
					{...others}
				>
					{value()}
				</div>
			)}
		</Show>
	);
}

export {
	Field,
	FieldLabel,
	FieldDescription,
	FieldError,
	FieldGroup,
	FieldLegend,
	FieldSeparator,
	FieldSet,
	FieldContent,
	FieldTitle,
};
