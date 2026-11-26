using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION current_tenant_id() RETURNS uuid
                LANGUAGE sql STABLE
                AS $$ SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid $$;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenants FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON tenants
                    USING (id = current_tenant_id())
                    WITH CHECK (id = current_tenant_id());
                """);

            foreach (var table in new[] { "users", "projects" })
            {
                migrationBuilder.Sql(
                    $"""
                    ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE {table} FORCE ROW LEVEL SECURITY;
                    CREATE POLICY tenant_isolation ON {table}
                        USING (tenant_id = current_tenant_id())
                        WITH CHECK (tenant_id = current_tenant_id());
                    """);
            }

            migrationBuilder.Sql(
                """
                CREATE FUNCTION authenticate_api_token(p_token_hash bytea)
                RETURNS TABLE (user_id uuid, tenant_id uuid, role text)
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = public
                AS $$
                    SELECT u.id, u.tenant_id, u.role
                    FROM api_tokens t
                    JOIN users u ON u.id = t.user_id
                    WHERE t.token_hash = p_token_hash
                $$;

                REVOKE ALL ON FUNCTION authenticate_api_token(bytea) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION authenticate_api_token(bytea) TO planboard_app;
                REVOKE ALL ON api_tokens FROM planboard_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE, DELETE ON api_tokens TO planboard_app;
                DROP FUNCTION authenticate_api_token(bytea);
                """);

            foreach (var table in new[] { "tenants", "users", "projects" })
            {
                migrationBuilder.Sql(
                    $"""
                    DROP POLICY tenant_isolation ON {table};
                    ALTER TABLE {table} NO FORCE ROW LEVEL SECURITY;
                    ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;
                    """);
            }

            migrationBuilder.Sql("DROP FUNCTION current_tenant_id();");
        }
    }
}
