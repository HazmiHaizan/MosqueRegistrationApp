using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MosqueRegistrationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToIcNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IcNumber",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IcNumber",
                table: "AspNetUsers",
                column: "IcNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IcNumber",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "IcNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
