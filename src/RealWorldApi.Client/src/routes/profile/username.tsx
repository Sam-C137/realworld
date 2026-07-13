import { createFileRoute } from '@tanstack/solid-router'

export const Route = createFileRoute('/profile/username')({
  component: RouteComponent,
})

function RouteComponent() {
  return <div>Hello "/profile/username"!</div>
}
