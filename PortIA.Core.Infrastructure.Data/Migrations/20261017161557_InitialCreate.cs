using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortIA.Core.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCION",
                columns: table => new
                {
                    ID_ACCION = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    DESCRIPCION = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCION", x => x.ID_ACCION);
                });

            migrationBuilder.CreateTable(
                name: "CANAL",
                columns: table => new
                {
                    ID_CANAL = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    TIPO = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CANAL", x => x.ID_CANAL);
                });

            migrationBuilder.CreateTable(
                name: "CLIMA",
                columns: table => new
                {
                    ID_CLIMA = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    DESCRIPCION = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIMA", x => x.ID_CLIMA);
                });

            migrationBuilder.CreateTable(
                name: "NAVIERA",
                columns: table => new
                {
                    ID_NAVIERA = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NOMBRE_NAVIERA = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CODIGO_NAVIERA = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EMAIL = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TELEFONO = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NAVIERA", x => x.ID_NAVIERA);
                });

            migrationBuilder.CreateTable(
                name: "PAIS",
                columns: table => new
                {
                    ID_PAIS = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NOMBRE_PAIS = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAIS", x => x.ID_PAIS);
                });

            migrationBuilder.CreateTable(
                name: "RIESGO",
                columns: table => new
                {
                    ID_RIESGO = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    DESCRIPCION = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RIESGO", x => x.ID_RIESGO);
                });

            migrationBuilder.CreateTable(
                name: "ROLES",
                columns: table => new
                {
                    ID_ROL = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NOMBRE_ROL = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLES", x => x.ID_ROL);
                });

            migrationBuilder.CreateTable(
                name: "TIPO_ALERTA",
                columns: table => new
                {
                    ID_TIPO_ALERTA = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    DESCRIPCION = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_ALERTA", x => x.ID_TIPO_ALERTA);
                });

            migrationBuilder.CreateTable(
                name: "TERMINAL",
                columns: table => new
                {
                    ID_TERMINAL = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CODIGO_TERMINAL = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NOMBRE_TERMINAL = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ID_PAIS = table.Column<int>(type: "integer", nullable: true),
                    CONFIG_SCRAPER = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TERMINAL", x => x.ID_TERMINAL);
                    table.ForeignKey(
                        name: "FK_TERMINAL_PAIS",
                        column: x => x.ID_PAIS,
                        principalTable: "PAIS",
                        principalColumn: "ID_PAIS");
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    ID_USUARIO = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NOMBRE_USUARIO = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    EMAIL = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PASSWORD_HASH = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ROL_ID = table.Column<int>(type: "integer", nullable: false),
                    CREACION = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.ID_USUARIO);
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL",
                        column: x => x.ROL_ID,
                        principalTable: "ROLES",
                        principalColumn: "ID_ROL",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CARGA_DOCUMENTOS",
                columns: table => new
                {
                    ID_CARGA_DOCUMENTOS = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    USER_ID = table.Column<int>(type: "integer", nullable: false),
                    STATUS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE"),
                    CREACION_FECHA = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UPDATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CARGA_DOCUMENTOS", x => x.ID_CARGA_DOCUMENTOS);
                    table.CheckConstraint("CK_CARGA_STATUS", "\"STATUS\" IN ('PENDIENTE','PROCESANDO','COMPLETO','ERROR')");
                    table.ForeignKey(
                        name: "FK_CARGA_USUARIO",
                        column: x => x.USER_ID,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LOG_AUDITORIA",
                columns: table => new
                {
                    ID_LOG_AUDITORIA = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NOMBRE_ENTIDAD = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ID_ENTIDAD = table.Column<int>(type: "integer", nullable: false),
                    ID_ACCION = table.Column<int>(type: "integer", nullable: false),
                    RESPONSABLE = table.Column<int>(type: "integer", nullable: true),
                    VALORES_ANTERIORES = table.Column<string>(type: "jsonb", nullable: true),
                    VALORES_NUEVOS = table.Column<string>(type: "jsonb", nullable: true),
                    CREATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_AUDITORIA", x => x.ID_LOG_AUDITORIA);
                    table.ForeignKey(
                        name: "FK_AUDITORIA_ACCION",
                        column: x => x.ID_ACCION,
                        principalTable: "ACCION",
                        principalColumn: "ID_ACCION",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AUDITORIA_USUARIO",
                        column: x => x.RESPONSABLE,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO");
                });

            migrationBuilder.CreateTable(
                name: "CONTAINER",
                columns: table => new
                {
                    ID_CONTAINER = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ID_CARGA_DOCUMENTOS = table.Column<int>(type: "integer", nullable: false),
                    NUMERO_CONTAINER = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ID_NAVIERA = table.Column<int>(type: "integer", nullable: true),
                    PUERTO_DESTINO = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NUMERO_BL = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NOMBRE_FORWARDER = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ID_TERMINAL_ORIGEN = table.Column<int>(type: "integer", nullable: true),
                    ID_TERMINAL_LLEGADA = table.Column<int>(type: "integer", nullable: true),
                    FECHA_LLEGADA_ESTIMADA = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    DIAS_LIBRES_DEMURRAGE = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DIAS_LIBRES_DETENTION = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FECHA_ARRIBO_REAL = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    FECHA_RETIRO_CONTAINER = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ESTADO_PUERTO = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ID_CLIMA = table.Column<int>(type: "integer", nullable: true),
                    DEADLINE_DEMURRAGE = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    DEADLINE_DETENTION = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ID_RIESGO = table.Column<int>(type: "integer", nullable: true),
                    ASIGNADO_A = table.Column<int>(type: "integer", nullable: true),
                    CREATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UPDATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONTAINER", x => x.ID_CONTAINER);
                    table.ForeignKey(
                        name: "FK_CONTAINER_ASIGNADO",
                        column: x => x.ASIGNADO_A,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO");
                    table.ForeignKey(
                        name: "FK_CONTAINER_CARGA",
                        column: x => x.ID_CARGA_DOCUMENTOS,
                        principalTable: "CARGA_DOCUMENTOS",
                        principalColumn: "ID_CARGA_DOCUMENTOS",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CONTAINER_CLIMA",
                        column: x => x.ID_CLIMA,
                        principalTable: "CLIMA",
                        principalColumn: "ID_CLIMA");
                    table.ForeignKey(
                        name: "FK_CONTAINER_NAVIERA",
                        column: x => x.ID_NAVIERA,
                        principalTable: "NAVIERA",
                        principalColumn: "ID_NAVIERA");
                    table.ForeignKey(
                        name: "FK_CONTAINER_RIESGO",
                        column: x => x.ID_RIESGO,
                        principalTable: "RIESGO",
                        principalColumn: "ID_RIESGO");
                    table.ForeignKey(
                        name: "FK_CONTAINER_TERMINAL_LLEGADA",
                        column: x => x.ID_TERMINAL_LLEGADA,
                        principalTable: "TERMINAL",
                        principalColumn: "ID_TERMINAL",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONTAINER_TERMINAL_ORIGEN",
                        column: x => x.ID_TERMINAL_ORIGEN,
                        principalTable: "TERMINAL",
                        principalColumn: "ID_TERMINAL",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENTO",
                columns: table => new
                {
                    ID_DOCUMENTO = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ID_CARGA_DOCUMENTOS = table.Column<int>(type: "integer", nullable: false),
                    TIPO_DOCUMENTO = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    STORAGE_URL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HUELLA_HASH = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CONFIDENCE = table.Column<float>(type: "real", nullable: true),
                    ESTADO_EXTRACCION = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE"),
                    DATOS_EXTRAIDOS_RAW = table.Column<string>(type: "jsonb", nullable: true),
                    CONTIENE_DATOS_PERSONALES = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RETENCION_HASTA = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    CREATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    VIGENTE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENTO", x => x.ID_DOCUMENTO);
                    table.ForeignKey(
                        name: "FK_DOCUMENTO_CARGA",
                        column: x => x.ID_CARGA_DOCUMENTOS,
                        principalTable: "CARGA_DOCUMENTOS",
                        principalColumn: "ID_CARGA_DOCUMENTOS",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ALERTA",
                columns: table => new
                {
                    ID_ALERTA = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ID_CONTAINER = table.Column<int>(type: "integer", nullable: false),
                    ID_TIPO_ALERTA = table.Column<int>(type: "integer", nullable: false),
                    ID_RIESGO = table.Column<int>(type: "integer", nullable: false),
                    MENSAJE = table.Column<string>(type: "text", nullable: false),
                    LEIDA = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    USUARIO_LECTOR = table.Column<int>(type: "integer", nullable: true),
                    FECHA_LECTURA = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    CREATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALERTA", x => x.ID_ALERTA);
                    table.ForeignKey(
                        name: "FK_ALERTA_CONTAINER",
                        column: x => x.ID_CONTAINER,
                        principalTable: "CONTAINER",
                        principalColumn: "ID_CONTAINER",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ALERTA_RIESGO",
                        column: x => x.ID_RIESGO,
                        principalTable: "RIESGO",
                        principalColumn: "ID_RIESGO",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ALERTA_TIPO",
                        column: x => x.ID_TIPO_ALERTA,
                        principalTable: "TIPO_ALERTA",
                        principalColumn: "ID_TIPO_ALERTA",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ALERTA_USUARIO_LEE",
                        column: x => x.USUARIO_LECTOR,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO");
                });

            migrationBuilder.CreateTable(
                name: "HITO",
                columns: table => new
                {
                    ID_HITO = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ID_CONTAINER = table.Column<int>(type: "integer", nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ORIGEN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CREADO_POR = table.Column<int>(type: "integer", nullable: true),
                    FECHA_OCURRENCIA = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HITO", x => x.ID_HITO);
                    table.CheckConstraint("CK_HITO_ORIGEN", "(\"ORIGEN\" = 'USUARIO' AND \"CREADO_POR\" IS NOT NULL) OR (\"ORIGEN\" IN ('IA','SCRAPER') AND \"CREADO_POR\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_HITO_CONTAINER",
                        column: x => x.ID_CONTAINER,
                        principalTable: "CONTAINER",
                        principalColumn: "ID_CONTAINER",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HITO_USUARIO",
                        column: x => x.CREADO_POR,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO");
                });

            migrationBuilder.CreateTable(
                name: "LOG_NOTIFICACION",
                columns: table => new
                {
                    ID_LOG_NOTIFICACION = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ID_ALERTA = table.Column<int>(type: "integer", nullable: false),
                    ID_CANAL = table.Column<int>(type: "integer", nullable: false),
                    DESTINATARIO = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ESTADO = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE_APROBACION"),
                    APROBADO_POR = table.Column<int>(type: "integer", nullable: true),
                    CREATE_AT = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_NOTIFICACION", x => x.ID_LOG_NOTIFICACION);
                    table.ForeignKey(
                        name: "FK_LOGNOTIF_ALERTA",
                        column: x => x.ID_ALERTA,
                        principalTable: "ALERTA",
                        principalColumn: "ID_ALERTA",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LOGNOTIF_CANAL",
                        column: x => x.ID_CANAL,
                        principalTable: "CANAL",
                        principalColumn: "ID_CANAL",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LOGNOTIF_USUARIO",
                        column: x => x.APROBADO_POR,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO");
                });

            migrationBuilder.InsertData(
                table: "ACCION",
                columns: new[] { "ID_ACCION", "DESCRIPCION" },
                values: new object[,]
                {
                    { 1, "CREAR" },
                    { 2, "ACTUALIZAR" },
                    { 3, "LEER" }
                });

            migrationBuilder.InsertData(
                table: "CANAL",
                columns: new[] { "ID_CANAL", "TIPO", "VIGENTE" },
                values: new object[,]
                {
                    { 1, "EMAIL", true },
                    { 2, "TELEGRAM", true }
                });

            migrationBuilder.InsertData(
                table: "CLIMA",
                columns: new[] { "ID_CLIMA", "DESCRIPCION" },
                values: new object[,]
                {
                    { 1, "Sin alerta" },
                    { 2, "Marejada" },
                    { 3, "Cierre de puerto" }
                });

            migrationBuilder.InsertData(
                table: "PAIS",
                columns: new[] { "ID_PAIS", "NOMBRE_PAIS" },
                values: new object[] { 1, "Chile" });

            migrationBuilder.InsertData(
                table: "RIESGO",
                columns: new[] { "ID_RIESGO", "DESCRIPCION", "VIGENTE" },
                values: new object[,]
                {
                    { 1, "OK", true },
                    { 2, "WARNING", true },
                    { 3, "CRITICAL", true },
                    { 4, "OVERDUE", true }
                });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ID_ROL", "NOMBRE_ROL", "VIGENTE" },
                values: new object[,]
                {
                    { 1, "ComEx", true },
                    { 2, "Admin", true }
                });

            migrationBuilder.InsertData(
                table: "TIPO_ALERTA",
                columns: new[] { "ID_TIPO_ALERTA", "DESCRIPCION", "VIGENTE" },
                values: new object[,]
                {
                    { 1, "DEMURRAGE", true },
                    { 2, "DETENTION", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCION_DESCRIPCION",
                table: "ACCION",
                column: "DESCRIPCION",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_CONTAINER",
                table: "ALERTA",
                column: "ID_CONTAINER");

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_LEIDA",
                table: "ALERTA",
                column: "LEIDA");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_ID_RIESGO",
                table: "ALERTA",
                column: "ID_RIESGO");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_ID_TIPO_ALERTA",
                table: "ALERTA",
                column: "ID_TIPO_ALERTA");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_USUARIO_LECTOR",
                table: "ALERTA",
                column: "USUARIO_LECTOR");

            migrationBuilder.CreateIndex(
                name: "IX_CARGA_DOCUMENTOS_USER_ID",
                table: "CARGA_DOCUMENTOS",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IDX_CONTAINER_ASIGNADO",
                table: "CONTAINER",
                column: "ASIGNADO_A");

            migrationBuilder.CreateIndex(
                name: "IDX_CONTAINER_CARGA",
                table: "CONTAINER",
                column: "ID_CARGA_DOCUMENTOS");

            migrationBuilder.CreateIndex(
                name: "IDX_CONTAINER_RIESGO",
                table: "CONTAINER",
                column: "ID_RIESGO");

            migrationBuilder.CreateIndex(
                name: "IX_CONTAINER_ID_CLIMA",
                table: "CONTAINER",
                column: "ID_CLIMA");

            migrationBuilder.CreateIndex(
                name: "IX_CONTAINER_ID_NAVIERA",
                table: "CONTAINER",
                column: "ID_NAVIERA");

            migrationBuilder.CreateIndex(
                name: "IX_CONTAINER_ID_TERMINAL_LLEGADA",
                table: "CONTAINER",
                column: "ID_TERMINAL_LLEGADA");

            migrationBuilder.CreateIndex(
                name: "IX_CONTAINER_ID_TERMINAL_ORIGEN",
                table: "CONTAINER",
                column: "ID_TERMINAL_ORIGEN");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTO_ID_CARGA_DOCUMENTOS",
                table: "DOCUMENTO",
                column: "ID_CARGA_DOCUMENTOS");

            migrationBuilder.CreateIndex(
                name: "UQ_DOCUMENTO_HASH",
                table: "DOCUMENTO",
                column: "HUELLA_HASH",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_HITO_CONTAINER",
                table: "HITO",
                column: "ID_CONTAINER");

            migrationBuilder.CreateIndex(
                name: "IX_HITO_CREADO_POR",
                table: "HITO",
                column: "CREADO_POR");

            migrationBuilder.CreateIndex(
                name: "IDX_AUDITORIA_ENTIDAD",
                table: "LOG_AUDITORIA",
                columns: new[] { "NOMBRE_ENTIDAD", "ID_ENTIDAD" });

            migrationBuilder.CreateIndex(
                name: "IX_LOG_AUDITORIA_ID_ACCION",
                table: "LOG_AUDITORIA",
                column: "ID_ACCION");

            migrationBuilder.CreateIndex(
                name: "IX_LOG_AUDITORIA_RESPONSABLE",
                table: "LOG_AUDITORIA",
                column: "RESPONSABLE");

            migrationBuilder.CreateIndex(
                name: "IDX_LOGNOTIF_ALERTA",
                table: "LOG_NOTIFICACION",
                column: "ID_ALERTA");

            migrationBuilder.CreateIndex(
                name: "IX_LOG_NOTIFICACION_APROBADO_POR",
                table: "LOG_NOTIFICACION",
                column: "APROBADO_POR");

            migrationBuilder.CreateIndex(
                name: "IX_LOG_NOTIFICACION_ID_CANAL",
                table: "LOG_NOTIFICACION",
                column: "ID_CANAL");

            migrationBuilder.CreateIndex(
                name: "IX_PAIS_NOMBRE_PAIS",
                table: "PAIS",
                column: "NOMBRE_PAIS",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RIESGO_DESCRIPCION",
                table: "RIESGO",
                column: "DESCRIPCION",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLES_NOMBRE_ROL",
                table: "ROLES",
                column: "NOMBRE_ROL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TERMINAL_CODIGO_TERMINAL",
                table: "TERMINAL",
                column: "CODIGO_TERMINAL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TERMINAL_ID_PAIS",
                table: "TERMINAL",
                column: "ID_PAIS");

            migrationBuilder.CreateIndex(
                name: "IX_TIPO_ALERTA_DESCRIPCION",
                table: "TIPO_ALERTA",
                column: "DESCRIPCION",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_EMAIL",
                table: "USUARIO",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_ROL_ID",
                table: "USUARIO",
                column: "ROL_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOCUMENTO");

            migrationBuilder.DropTable(
                name: "HITO");

            migrationBuilder.DropTable(
                name: "LOG_AUDITORIA");

            migrationBuilder.DropTable(
                name: "LOG_NOTIFICACION");

            migrationBuilder.DropTable(
                name: "ACCION");

            migrationBuilder.DropTable(
                name: "ALERTA");

            migrationBuilder.DropTable(
                name: "CANAL");

            migrationBuilder.DropTable(
                name: "CONTAINER");

            migrationBuilder.DropTable(
                name: "TIPO_ALERTA");

            migrationBuilder.DropTable(
                name: "CARGA_DOCUMENTOS");

            migrationBuilder.DropTable(
                name: "CLIMA");

            migrationBuilder.DropTable(
                name: "NAVIERA");

            migrationBuilder.DropTable(
                name: "RIESGO");

            migrationBuilder.DropTable(
                name: "TERMINAL");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "PAIS");

            migrationBuilder.DropTable(
                name: "ROLES");
        }
    }
}
