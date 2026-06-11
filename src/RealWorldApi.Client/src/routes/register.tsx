import { createFileRoute } from '@tanstack/solid-router'

export const Route = createFileRoute('/register')({
  component: RouteComponent,
})

function RouteComponent() {
  return <div>Hello "/register"!</div>
}
