using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Platform.Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddPbacArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsSystemPolicy = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyId = table.Column<int>(type: "integer", nullable: false),
                    PermissionCatalogId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyPermissions_PermissionCatalogs_PermissionCatalogId",
                        column: x => x.PermissionCatalogId,
                        principalTable: "PermissionCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PolicyPermissions_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePolicyAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyId = table.Column<int>(type: "integer", nullable: false),
                    CompanyScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePolicyAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePolicyAssignments_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPolicyAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PolicyId = table.Column<int>(type: "integer", nullable: false),
                    CompanyScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentScope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicyAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPolicyAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPolicyAssignments_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyPermissions_PermissionCatalogId",
                table: "PolicyPermissions",
                column: "PermissionCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyPermissions_PolicyId",
                table: "PolicyPermissions",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePolicyAssignments_PolicyId",
                table: "RolePolicyAssignments",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAssignments_PolicyId",
                table: "UserPolicyAssignments",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAssignments_UserId",
                table: "UserPolicyAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PolicyPermissions");
            migrationBuilder.DropTable(name: "RolePolicyAssignments");
            migrationBuilder.DropTable(name: "UserPolicyAssignments");
            migrationBuilder.DropTable(name: "PermissionCatalogs");
            migrationBuilder.DropTable(name: "Policies");
        }
    }
}
