using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertaClimatica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarEventosYBitacora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComunidadId",
                table: "Eventos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NivelRiesgo",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SensorId",
                table: "Eventos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioResponsableId",
                table: "Eventos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Valor",
                table: "Eventos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Entidad",
                table: "Bitacora",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntidadId",
                table: "Bitacora",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LecturasClimaticas_SensorId",
                table: "LecturasClimaticas",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_ComunidadId",
                table: "Eventos",
                column: "ComunidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_SensorId",
                table: "Eventos",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_UsuarioResponsableId",
                table: "Eventos",
                column: "UsuarioResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Comunidades_ComunidadId",
                table: "Eventos",
                column: "ComunidadId",
                principalTable: "Comunidades",
                principalColumn: "ComunidadId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Sensores_SensorId",
                table: "Eventos",
                column: "SensorId",
                principalTable: "Sensores",
                principalColumn: "SensorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Usuarios_UsuarioResponsableId",
                table: "Eventos",
                column: "UsuarioResponsableId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturasClimaticas_Sensores_SensorId",
                table: "LecturasClimaticas",
                column: "SensorId",
                principalTable: "Sensores",
                principalColumn: "SensorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Comunidades_ComunidadId",
                table: "Eventos");

            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Sensores_SensorId",
                table: "Eventos");

            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Usuarios_UsuarioResponsableId",
                table: "Eventos");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturasClimaticas_Sensores_SensorId",
                table: "LecturasClimaticas");

            migrationBuilder.DropIndex(
                name: "IX_LecturasClimaticas_SensorId",
                table: "LecturasClimaticas");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_ComunidadId",
                table: "Eventos");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_SensorId",
                table: "Eventos");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_UsuarioResponsableId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "ComunidadId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "NivelRiesgo",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "SensorId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "UsuarioResponsableId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "Valor",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "Entidad",
                table: "Bitacora");

            migrationBuilder.DropColumn(
                name: "EntidadId",
                table: "Bitacora");
        }
    }
}
