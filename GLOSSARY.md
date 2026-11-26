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
