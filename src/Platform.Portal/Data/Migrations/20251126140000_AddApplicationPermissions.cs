using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crea tabella ApplicationPermissions
            migrationBuilder.CreateTable(
                name: "ApplicationPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GrantedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationPermissions_AspNetUsers_GrantedBy",
                        column: x => x.GrantedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Crea indici
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPermissions_UserId",
                table: "ApplicationPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPermissions_ApplicationName",
                table: "ApplicationPermissions",
                column: "ApplicationName");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPermissions_UserId_ApplicationName",
                table: "ApplicationPermissions",
                columns: new[] { "UserId", "ApplicationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPermissions_GrantedBy",
                table: "ApplicationPermissions",
                column: "GrantedBy");

            // Seed data: Admin con tutti i permessi
            migrationBuilder.Sql(@"
                DECLARE @AdminUserId NVARCHAR(450)
                SELECT TOP 1 @AdminUserId = u.Id 
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE r.Name = 'Admin'

                IF @AdminUserId IS NOT NULL
                BEGIN
                    INSERT INTO ApplicationPermissions (UserId, ApplicationName, CanView, CanCreate, CanEdit, CanDelete, GrantedBy, GrantedAt, UpdatedAt)
                    VALUES 
                        (@AdminUserId, 'KioskRegistration', 1, 1, 1, 1, @AdminUserId, GETUTCDATE(), GETUTCDATE()),
                        (@AdminUserId, 'BugTracking', 1, 1, 1, 1, @AdminUserId, GETUTCDATE(), GETUTCDATE()),
                        (@AdminUserId, 'FeatureRequest', 1, 1, 1, 1, @AdminUserId, GETUTCDATE(), GETUTCDATE()),
                        (@AdminUserId, 'DeveloperDashboard', 1, 1, 1, 1, @AdminUserId, GETUTCDATE(), GETUTCDATE())
                END
            ");

            // Seed data: User con solo View su KioskRegistration
            migrationBuilder.Sql(@"
                DECLARE @AdminUserId NVARCHAR(450)
                SELECT TOP 1 @AdminUserId = u.Id 
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE r.Name = 'Admin'

                DECLARE @UserIds TABLE (UserId NVARCHAR(450))
                INSERT INTO @UserIds (UserId)
                SELECT u.Id 
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE r.Name = 'User'

                IF @AdminUserId IS NOT NULL
                BEGIN
                    INSERT INTO ApplicationPermissions (UserId, ApplicationName, CanView, CanCreate, CanEdit, CanDelete, GrantedBy, GrantedAt, UpdatedAt)
                    SELECT UserId, 'KioskRegistration', 1, 0, 0, 0, @AdminUserId, GETUTCDATE(), GETUTCDATE()
                    FROM @UserIds
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ApplicationPermissions 
                        WHERE ApplicationPermissions.UserId = [@UserIds].UserId 
                        AND ApplicationPermissions.ApplicationName = 'KioskRegistration'
                    )
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationPermissions");
        }
    }
}