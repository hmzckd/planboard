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
