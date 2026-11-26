# 0005. Limit query depth and cost, disable introspection in production

## Status

Accepted

## Context

A REST endpoint decides how much work one request does. In GraphQL the client decides. Page sizes are capped at 50,
but nesting multiplies them: `projects(first: 50) { tasks(first: 50) { comments(first: 50) { author } } }` asks for
up to 125,000 comments in one request. A client can also nest `task { project { tasks { project ... } } }` as deep as
it likes. Both are cheap to send and expensive to run.

Introspection (`__schema`, `__type`) describes the whole schema. It is what the IDE and code generators use, and it
is also the first thing an attacker asks for.

## Decision

Three checks run during validation, before any resolver or SQL command:

| Check | Limit | Error |
| --- | --- | --- |
| Execution depth | 8 levels (introspection fields not counted) | `detectedExecutionDepth` in the error extensions |
| Field cost (work) | 10,000 | `HC0047`, with `fieldCost` and `maxFieldCost` in the extensions |
| Type cost (objects returned) | 10,000 | a cost error with `typeCost` and `maxTypeCost` |

Hot Chocolate computes the cost from the query alone: each resolver has a weight and list fields multiply by their
`first` argument (or the default page size of 20). Send the header `GraphQL-Cost: report` to see the numbers.

The limits come from the app's own queries, measured with seed data:

| Query | Depth | Field cost | Type cost |
| --- | --- | --- | --- |
| Board: 10 projects × 20 tasks with assignees | 6 | 2,121 | 422 |
| Deepest screen: 10 projects × 10 tasks with assignee, labels and 3 comments with authors | 8 | 6,221 | 1,022 |
| 50 × 50 × 50 comments with authors | 8 | 1,278,061 | 255,102 |

The default field cost limit of 1,000 would reject the board query. 10,000 leaves room above the deepest screen and
rejects the third query by a factor of more than 100.

Outside the `Development` environment introspection is disabled (`HC0046`, only `__typename` stays allowed) and the
Nitro IDE is not served. Development seed data and the `X-Sql-Command-Count` header are also on only in
`Development` (`appsettings.Development.json`).

## Consequences

- An expensive query fails with `400` before it reaches PostgreSQL; `QueryLimitsTests` checks that it runs 0 SQL
  commands.
- A new screen that needs more than depth 8 or cost 10,000 must change these limits on purpose, with a new
  measurement.
- In production, clients need the schema from `schema.graphql` in the repository instead of introspection.
- Cost is an estimate from `first`, not a measurement of real rows. A small tenant pays the same price as a large one.
