using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateTasksCommentsAndLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id",
                table: "users");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_users_tenant_id_id",
                table: "users",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_projects_tenant_id_id",
                table: "projects",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labels", x => x.id);
                    table.UniqueConstraint("ak_labels_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_labels_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tasks", x => x.id);
                    table.UniqueConstraint("ak_tasks_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_tasks_projects_tenant_id_project_id",
                        columns: x => new { x.tenant_id, x.project_id },
                        principalTable: "projects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tasks_users_tenant_id_assignee_id",
                        columns: x => new { x.tenant_id, x.assignee_id },
                        principalTable: "users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_tasks_tenant_id_task_id",
                        columns: x => new { x.tenant_id, x.task_id },
                        principalTable: "tasks",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_comments_users_tenant_id_author_id",
                        columns: x => new { x.tenant_id, x.author_id },
                        principalTable: "users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_labels",
                columns: table => new
                {
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_labels", x => new { x.task_id, x.label_id });
                    table.ForeignKey(
                        name: "fk_task_labels_labels_tenant_id_label_id",
                        columns: x => new { x.tenant_id, x.label_id },
                        principalTable: "labels",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_task_labels_tasks_tenant_id_task_id",
                        columns: x => new { x.tenant_id, x.task_id },
                        principalTable: "tasks",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comments_task_id_created_at_id",
                table: "comments",
                columns: new[] { "task_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_tenant_id_author_id",
                table: "comments",
                columns: new[] { "tenant_id", "author_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_tenant_id_task_id",
                table: "comments",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_labels_tenant_id_name",
                table: "labels",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_labels_tenant_id_label_id",
                table: "task_labels",
                columns: new[] { "tenant_id", "label_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_labels_tenant_id_task_id",
                table: "task_labels",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_project_id_created_at_id",
                table: "tasks",
                columns: new[] { "project_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_tenant_id_assignee_id",
                table: "tasks",
                columns: new[] { "tenant_id", "assignee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_tenant_id_project_id",
                table: "tasks",
                columns: new[] { "tenant_id", "project_id" });

            foreach (var table in new[] { "tasks", "comments", "labels", "task_labels" })
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "task_labels");

            migrationBuilder.DropTable(
                name: "labels");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_users_tenant_id_id",
                table: "users");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_projects_tenant_id_id",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                table: "users",
                column: "tenant_id");
        }
    }
}
