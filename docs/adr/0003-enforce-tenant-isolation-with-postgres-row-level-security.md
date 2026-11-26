# 0003. Enforce tenant isolation with PostgreSQL row-level security

## Status

Accepted

## Context

ADR 0002 scopes queries with EF Core global query filters. That protects every query that goes through EF Core with the
filter on. It does not protect raw SQL, a query that calls `IgnoreQueryFilters()` by mistake, or a bug in the filter
itself. A single forgotten condition would leak another tenant's data.

## Decision

Add a second lock in the database:

- Every tenant table has row-level security enabled and forced, with one policy:
  `tenant_id = current_tenant_id()` for reads (`USING`) and writes (`WITH CHECK`).
- `current_tenant_id()` reads the session setting `app.tenant_id`. An unset or empty setting gives `NULL`, so no row matches.
- `TenantConnectionInterceptor` writes `app.tenant_id` every time EF Core opens a connection, using the tenant of the
  context. A context without a tenant writes an empty value. A pooled connection therefore never keeps the previous
  request's tenant.
- The API connects as `planboard_app` (not the table owner, `NOBYPASSRLS`). Migrations and seeding use `planboard_owner`,
  which has `BYPASSRLS` and is used only at startup.
- Token lookup happens before the tenant is known. It goes through `authenticate_api_token(hash)`, a `SECURITY DEFINER`
  function owned by `planboard_owner`. `planboard_app` cannot read `api_tokens` at all.

## Consequences

- Removing the EF Core filter by mistake no longer leaks data; tests prove this with `IgnoreQueryFilters()`.
- Every connection open costs one extra round trip for `set_config`. Acceptable at this scale; a later optimization
  could set it once per transaction.
- Policies are written in SQL inside migrations, so they are reviewed like code.
- Anything that must cross tenants (reports, support tools) needs an explicit role with `BYPASSRLS`.
