# 0002. Share tables and scope every row by tenant_id

## Status

Accepted

## Context

Planboard hosts many organizations (tenants) in one deployment. A tenant must never see another tenant's data.
There are three common layouts:

1. **Database per tenant.** Strongest isolation, but every new tenant needs a new database, and every migration runs once per tenant.
2. **Schema per tenant.** One database, one schema per tenant. Cheaper, but the number of schemas grows with tenants and
   connection pooling and migrations get harder.
3. **Shared tables.** Every tenant table has a `tenant_id` column. Cheapest to run and simplest to migrate. The risk is a
   query that forgets the `tenant_id` filter.

## Decision

Use shared tables (option 3). The tenant comes from the caller's API token, never from client input.

The first lock is in the application: EF Core global query filters add `tenant_id = <current tenant>` to every query on
tenant tables. `TenantDbContextFactory` sets the tenant on each context it hands out, and the context clears it when it
goes back to the pool. A context without a tenant matches no rows (fail closed).

## Consequences

- One database and one migration path for all tenants.
- A missing filter would leak data, so the application filter alone is not enough. ADR 0003 adds a second lock in PostgreSQL.
- Code that must see every tenant (migrations, seeding) has to say so explicitly with `IgnoreQueryFilters()`.
- Very large tenants cannot be moved to their own hardware without extra work. That is acceptable for this project.
