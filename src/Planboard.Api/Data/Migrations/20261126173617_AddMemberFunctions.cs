using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION email_exists(p_email text) RETURNS boolean
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = public
                AS $$ SELECT EXISTS (SELECT 1 FROM users WHERE email = p_email) $$;

                REVOKE ALL ON FUNCTION email_exists(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION email_exists(text) TO planboard_app;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION issue_api_token(p_user_id uuid, p_token_hash bytea, p_name text) RETURNS void
                LANGUAGE plpgsql SECURITY DEFINER
                SET search_path = public
                AS $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM users WHERE id = p_user_id AND tenant_id = current_tenant_id()) THEN
                        RAISE EXCEPTION 'user % is not in the current tenant', p_user_id USING ERRCODE = '42501';
                    END IF;

                    INSERT INTO api_tokens (id, user_id, token_hash, name, created_at)
                    VALUES (uuidv7(), p_user_id, p_token_hash, p_name, now());
                END
                $$;

                REVOKE ALL ON FUNCTION issue_api_token(uuid, bytea, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION issue_api_token(uuid, bytea, text) TO planboard_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION issue_api_token(uuid, bytea, text);");
            migrationBuilder.Sql("DROP FUNCTION email_exists(text);");
        }
    }
}
