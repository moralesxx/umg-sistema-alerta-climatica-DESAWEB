using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertaClimatica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarComunidadYCamposSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComunidadId",
                table: "Sensores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Sensores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInstalacion",
                table: "Sensores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Comunidades",
                columns: table => new
                {
                    ComunidadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Departamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunidades", x => x.ComunidadId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_ComunidadId",
                table: "Sensores",
                column: "ComunidadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sensores_Comunidades_ComunidadId",
                table: "Sensores",
                column: "ComunidadId",
                principalTable: "Comunidades",
                principalColumn: "ComunidadId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sensores_Comunidades_ComunidadId",
                table: "Sensores");

            migrationBuilder.DropTable(
                name: "Comunidades");

            migrationBuilder.DropIndex(
                name: "IX_Sensores_ComunidadId",
                table: "Sensores");

            migrationBuilder.DropColumn(
                name: "ComunidadId",
                table: "Sensores");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Sensores");

            migrationBuilder.DropColumn(
                name: "FechaInstalacion",
                table: "Sensores");
        }
    }
}
