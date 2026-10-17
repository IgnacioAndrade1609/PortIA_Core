using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortIA.Core.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstadoContainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ALERTA_CONTAINER",
                table: "ALERTA");

            migrationBuilder.RenameColumn(
                name: "ESTADO_PUERTO",
                table: "CONTAINER",
                newName: "EstadoPuerto");

            migrationBuilder.AlterColumn<string>(
                name: "EstadoPuerto",
                table: "CONTAINER",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ID_ESTADO",
                table: "CONTAINER",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ESTADO_CONTAINER",
                columns: table => new
                {
                    ID_ESTADO = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DESCRIPCION = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTADO_CONTAINER", x => x.ID_ESTADO);
                });

            migrationBuilder.InsertData(
                table: "ESTADO_CONTAINER",
                columns: new[] { "ID_ESTADO", "DESCRIPCION", "VIGENTE" },
                values: new object[,]
                {
                    { 1, "PLANIFICADO", true },
                    { 2, "EN_PATIO_ORIGEN", true },
                    { 3, "EMBARCADO", true },
                    { 4, "EN_TRANSITO", true },
                    { 5, "ARRIBADO", true },
                    { 6, "DESCARGADO", true },
                    { 7, "EN_INSPECCION", true },
                    { 8, "LIBERADO", true },
                    { 9, "DISPONIBLE_RETIRO", true },
                    { 10, "RETIRADO", true },
                    { 11, "ENTREGADO_DESTINO", true },
                    { 12, "DEVUELTO_VACIO", true },
                    { 13, "DEMURRAGE", true },
                    { 14, "VENCIDO", true },
                    { 15, "RETENIDO", true },
                    { 16, "DANADO", true }
                });

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ID_ROL",
                keyValue: 1,
                column: "NOMBRE_ROL",
                value: "Analista");

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ID_ROL",
                keyValue: 2,
                column: "NOMBRE_ROL",
                value: "Jefe");

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ID_ROL", "NOMBRE_ROL", "VIGENTE" },
                values: new object[] { 3, "Admin", true });

            migrationBuilder.CreateIndex(
                name: "IX_CONTAINER_ID_ESTADO",
                table: "CONTAINER",
                column: "ID_ESTADO");

            migrationBuilder.CreateIndex(
                name: "IX_ESTADO_CONTAINER_DESCRIPCION",
                table: "ESTADO_CONTAINER",
                column: "DESCRIPCION",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ALERTA",
                table: "ALERTA",
                column: "ID_CONTAINER",
                principalTable: "CONTAINER",
                principalColumn: "ID_CONTAINER",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CONTAINER_ESTADO",
                table: "CONTAINER",
                column: "ID_ESTADO",
                principalTable: "ESTADO_CONTAINER",
                principalColumn: "ID_ESTADO",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ALERTA",
                table: "ALERTA");

            migrationBuilder.DropForeignKey(
                name: "FK_CONTAINER_ESTADO",
                table: "CONTAINER");

            migrationBuilder.DropTable(
                name: "ESTADO_CONTAINER");

            migrationBuilder.DropIndex(
                name: "IX_CONTAINER_ID_ESTADO",
                table: "CONTAINER");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ID_ROL",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "ID_ESTADO",
                table: "CONTAINER");

            migrationBuilder.RenameColumn(
                name: "EstadoPuerto",
                table: "CONTAINER",
                newName: "ESTADO_PUERTO");

            migrationBuilder.AlterColumn<string>(
                name: "ESTADO_PUERTO",
                table: "CONTAINER",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ID_ROL",
                keyValue: 1,
                column: "NOMBRE_ROL",
                value: "ComEx");

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ID_ROL",
                keyValue: 2,
                column: "NOMBRE_ROL",
                value: "Admin");

            migrationBuilder.AddForeignKey(
                name: "FK_ALERTA_CONTAINER",
                table: "ALERTA",
                column: "ID_CONTAINER",
                principalTable: "CONTAINER",
                principalColumn: "ID_CONTAINER",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
