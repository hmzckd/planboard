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
