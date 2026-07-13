import type { ComponentProps } from "solid-js";
import { splitProps } from "solid-js";
import { cn } from "~/lib/utils";

function Label(props: ComponentProps<"label">) {
	const [local, others] = splitProps(props, ["class"]);
	return (
		// biome-ignore lint/a11y/noLabelWithoutControl: reusable component
		<label
			class={cn(
				"text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70",
				local.class,
			)}
			{...others}
		/>
	);
}

export { Label };
