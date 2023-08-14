using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viewer.Server.Migrations
{
    /// <inheritdoc />
    public partial class albums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGroupId");

            migrationBuilder.DropColumn(
                name: "Policy",
                table: "UserGroups");

            migrationBuilder.RenameColumn(
                name: "Prefix",
                table: "Uploads",
                newName: "DirectoryPrefix");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Uploads",
                newName: "OriginalFileName");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AlbumId",
                table: "Uploads",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Album",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Album", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Album_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Album_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserId",
                table: "UserGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_AlbumId",
                table: "Uploads",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Album_UserGroupId",
                table: "Album",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Album_UserId",
                table: "Album",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_Users_Id",
                table: "GroupMember",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Uploads_Album_AlbumId",
                table: "Uploads",
                column: "AlbumId",
                principalTable: "Album",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Users_UserId",
                table: "UserGroups",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_Users_Id",
                table: "GroupMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Uploads_Album_AlbumId",
                table: "Uploads");

            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Users_UserId",
                table: "UserGroups");

            migrationBuilder.DropTable(
                name: "Album");

            migrationBuilder.DropIndex(
                name: "IX_UserGroups_UserId",
                table: "UserGroups");

            migrationBuilder.DropIndex(
                name: "IX_Uploads_AlbumId",
                table: "Uploads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "Uploads");

            migrationBuilder.RenameColumn(
                name: "OriginalFileName",
                table: "Uploads",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "DirectoryPrefix",
                table: "Uploads",
                newName: "Prefix");

            migrationBuilder.AddColumn<int>(
                name: "Policy",
                table: "UserGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserGroupId",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupId_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupId_UserId",
                table: "UserGroupId",
                column: "UserId");
        }
    }
}
