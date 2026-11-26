# Glossary

| Term | Meaning in Planboard |
| --- | --- |
| **GraphQL** | A query language for APIs. The client sends one request to `/graphql` and names exactly the fields it wants back. |
| **Schema** | The contract of a GraphQL API: every type, field and argument the server offers. Ours is committed as `schema.graphql`. |
| **Query** | A read operation. Example: `{ ping }`. |
| **Mutation** | A write operation (create, update, delete). |
| **Subscription** | A long-lived operation; the server pushes new results when something changes. |
| **Resolver** | The code that produces the value of one field. `Query.Ping()` is the resolver of the `ping` field. |
| **SDL** | Schema Definition Language, the text format of a schema (`type Query { ping: String! }`). |
| **Schema snapshot test** | A test that fails when the generated schema differs from `schema.graphql`, so schema changes are always deliberate. |
| **Tenant** | One customer organization (for example Acme). Every row of tenant data carries its `tenant_id`. |
| **Owner role** (`planboard_owner`) | The Postgres role that owns the tables and runs migrations. |
| **App role** (`planboard_app`) | The Postgres role the API connects with. It can read and write rows but cannot change the schema. |
| **Migration** | A versioned schema change (EF Core migrations in `src/Planboard.Api/Data/Migrations`). |
| **API token** | A personal secret like `pb_dev_alice`, sent as `Authorization: Bearer <token>`. Only its SHA-256 hash is stored. |
| **Tenant context** | Who is calling and for which tenant (`CurrentUser`: user id, tenant id, role), built from the token on every request. |
| **Global query filter** | An EF Core rule that is added to every query of an entity. Ours adds `tenant_id = <current tenant>`. |
| **Fail closed** | When the tenant is unknown, return nothing instead of everything. |
| **Row-level security (RLS)** | A PostgreSQL feature that hides or blocks rows per session. Our policy allows only rows where `tenant_id = current_tenant_id()`. |
| **`app.tenant_id`** | The session setting the API writes on every connection open; RLS policies read it. |
| **`SECURITY DEFINER` function** | A function that runs with its owner's rights. `authenticate_api_token` uses it to find a token before the tenant is known. |
| **Defense in depth** | Two independent locks (EF Core filter and RLS); either one alone keeps tenants apart. |
| **N+1 problem** | One query for a list (1) plus one more query for every item in it (N). GraphQL resolvers run per field, so nested fields cause it easily. |
| **`X-Sql-Command-Count`** | A development-only response header with the number of SQL commands EF Core ran for the request. |
| **Composite foreign key** | A foreign key over `(tenant_id, x_id)`. It makes cross-tenant references impossible in the database. |
| **DataLoader** | Collects the keys that many resolvers ask for in one step and loads them with one query, then caches them for the request. |
| **Batch** | One DataLoader call that serves many resolvers at once. |
| **Global ID** | An opaque `ID!` such as `UHJvamVjdDo...`: base64 of `Project:<uuid>`. It names the type, so `node(id)` can fetch any object. |
| **Node** | An object that can be fetched by its global ID through `node(id)` / `nodes(ids)` (Relay global object identification). |
| **Connection** | A paged list: `nodes`, `edges { cursor node }` and `pageInfo { hasNextPage endCursor }`. |
| **Keyset (cursor) pagination** | The next page starts after the last row's sort keys: `WHERE (created_at, id) > (...)`. Inserts before the cursor do not shift pages. Offset pagination would. |
| **Query cost** | A score Hot Chocolate computes before execution from field weights and list sizes. Queries above the limit are rejected. |
| **Mutation conventions** | Each mutation takes one `input` object and returns a payload with the result and an `errors` list of typed errors (`NotFoundError`, `ValidationError`, ...). |
| **Business error** | An expected failure (not found, invalid status change). It is part of the payload, not the top-level `errors`, and the HTTP status stays 200. |
| **Role** | `OWNER` (everything, manages members), `MEMBER` (reads and writes projects, tasks, comments), `VIEWER` (reads only). |
| **Policy** | A named ASP.NET Core authorization rule: `Writer` = owner or member, `Owner` = owner only. Used with `[Authorize(Policy = ...)]`. |
| **Field-level authorization** | A rule on one field instead of a whole endpoint. `User.email` is visible to owners and to the user themselves. |
| **Null propagation** | When a non-null field fails, GraphQL nulls its parent. Fields that may be hidden (like `email`) are nullable so only they become `null`. |
