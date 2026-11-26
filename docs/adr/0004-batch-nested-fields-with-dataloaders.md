# 0004. Batch nested fields with DataLoaders

## Status

Accepted

## Context

GraphQL resolves each field on its own. A resolver for `Task.assignee` does not know it runs inside a list of 700 tasks,
so a straightforward implementation sends one SQL query per task. With seed data, the query
`{ projects { name tasks { title assignee { displayName } } } }` ran 632 SQL commands for one tenant (the N+1 problem).

## Decision

Every field that loads related rows goes through a DataLoader (Hot Chocolate's source-generated `[DataLoader]` methods):

| DataLoader | Key | Loads |
| --- | --- | --- |
| `UserById` | user id | one user |
| `ProjectById` | project id | one project |
| `TasksByProjectId` | project id | tasks of each project |
| `CommentsByTaskId` | task id | comments of each task |
| `LabelsByTaskId` | task id | labels of each task |

A DataLoader collects the keys requested during one execution step and loads them in one `WHERE key = ANY(...)` query.
Results are cached for the rest of the request.

DataLoaders create their own `PlanboardDbContext` from the tenant-aware factory, so the EF Core filter and row-level
security apply to them like to any other query.

## Consequences

- The query above now runs 3 SQL commands; a deeper query with labels, comments and authors runs 6.
  `NPlusOneTests` fail if a resolver bypasses a DataLoader.
- Lists are loaded completely per parent for now. Pagination (ADR in PB-08) limits how many rows a list returns.
- The same key loaded at two depths (assignees, then comment authors) is two batches, not one.
