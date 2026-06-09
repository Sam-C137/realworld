import { RouterProvider } from "@tanstack/solid-router";
import { render } from "solid-js/web";

import { getRouter } from "./router";

const root = document.getElementById("root");

if (!root) {
	throw new Error("Root element #root was not found.");
}

render(() => <RouterProvider router={getRouter()} />, root);
