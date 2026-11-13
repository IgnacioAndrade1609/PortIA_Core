using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortIA.Core.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJefeIdToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JEFE_ID",
                table: "USUARIO",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_JEFE_ID",
                table: "USUARIO",
                column: "JEFE_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_USUARIO_JEFE",
                table: "USUARIO",
                column: "JEFE_ID",
                principalTable: "USUARIO",
                principalColumn: "ID_USUARIO",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USUARIO_JEFE",
                table: "USUARIO");

            migrationBuilder.DropIndex(
                name: "IX_USUARIO_JEFE_ID",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "JEFE_ID",
                table: "USUARIO");
        }
    }
}
