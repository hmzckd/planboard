# 0006. In-memory subscriptions with the token in `connection_init`

## Status

Accepted

## Context

A board screen should show changes from other people without polling. GraphQL subscriptions keep a WebSocket open and
push an event for each change. Two questions come with that:

- **Who is on the socket?** Browsers cannot set an `Authorization` header on a WebSocket. The `graphql-transport-ws`
  protocol has a first message, `connection_init`, whose `payload` the client fills freely.
- **Who may listen to what?** An event about an Acme task must never reach a Globex user.

## Decision

- One subscription, `taskChanged(projectId: ID!)`, over `graphql-transport-ws` on `/graphql`.
- The client sends `{"type": "connection_init", "payload": {"authorization": "Bearer <token>"}}`.
  `ApiTokenSocketSessionInterceptor` checks the token with the same database function as HTTP requests and sets the
  connection's user. A missing or wrong token closes the socket with `4403 Forbidden`.
- When a client subscribes, the project is looked up through the tenant-scoped `DbContext`. A project of another
  tenant is not found, so the subscription ends with a `NOT_FOUND` error and no topic is joined.
- Each project has its own topic, `taskChanged:<projectId>`. Task mutations publish
  `TaskChangedEvent(taskId, projectId, kind, changedById)` after the database commit.
- The event carries ids only. `task` and `changedBy` are loaded per subscriber through DataLoaders, under the
  subscriber's tenant and row-level security, so a subscriber never sees data its own query could not read.
- Events go through Hot Chocolate's in-memory topic (`AddInMemorySubscriptions`).

## Consequences

- Works only with one app instance. With two instances, a change on instance A is not seen by subscribers on
  instance B. Moving to Redis or PostgreSQL `LISTEN/NOTIFY` means adding a Hot Chocolate package and replacing one
  line (`AddRedisSubscriptions` or `AddPostgresSubscriptions`); it is out of scope for now.
- Delivery is at most once. A client that reconnects should refetch the board instead of expecting missed events.
- The token and role are checked when the connection opens. A role change or a revoked token takes effect on the
  next connection.
- Viewers can subscribe: watching is reading.
