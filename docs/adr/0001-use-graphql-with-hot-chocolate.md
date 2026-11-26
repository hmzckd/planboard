# 0001. Use GraphQL with Hot Chocolate

## Status

Accepted

## Context

Planboard serves nested project data: projects, their tasks, each task's assignee, comments and labels. Different
screens need different slices of that tree. With REST we would either add many endpoints or return large payloads that
most clients do not need.

## Decision

Expose a GraphQL API at `/graphql`, built with Hot Chocolate on ASP.NET Core. Types are written in C# (code-first with
attributes) and the generated schema is committed as `schema.graphql`.

## Consequences

- Clients ask for exactly the fields they need in one request.
- Hot Chocolate brings DataLoader, cursor pagination, cost analysis and subscriptions, so we do not build them by hand.
- GraphQL moves new problems to the server: N+1 queries, expensive nested queries and field-level authorization.
  Later ADRs address each one.
- HTTP status codes no longer describe business errors; errors become part of the schema.
