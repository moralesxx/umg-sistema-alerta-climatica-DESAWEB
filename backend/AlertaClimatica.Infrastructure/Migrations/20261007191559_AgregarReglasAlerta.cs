using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertaClimatica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReglasAlerta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReglaAlertaId",
                table: "Alertas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReglasAlerta",
                columns: table => new
                {
                    ReglaAlertaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoSensorId = table.Column<int>(type: "int", nullable: false),
                    ValorMinimo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NivelPeligro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoFenomeno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasAlerta", x => x.ReglaAlertaId);
                    table.ForeignKey(
                        name: "FK_ReglasAlerta_TiposSensor_TipoSensorId",
                        column: x => x.TipoSensorId,
                        principalTable: "TiposSensor",
                        principalColumn: "TipoSensorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_ReglaAlertaId",
                table: "Alertas",
                column: "ReglaAlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_SensorId",
                table: "Alertas",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasAlerta_TipoSensorId",
                table: "ReglasAlerta",
                column: "TipoSensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_ReglasAlerta_ReglaAlertaId",
                table: "Alertas",
                column: "ReglaAlertaId",
                principalTable: "ReglasAlerta",
                principalColumn: "ReglaAlertaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Sensores_SensorId",
                table: "Alertas",
                column: "SensorId",
                principalTable: "Sensores",
                principalColumn: "SensorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_ReglasAlerta_ReglaAlertaId",
                table: "Alertas");

            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Sensores_SensorId",
                table: "Alertas");

            migrationBuilder.DropTable(
                name: "ReglasAlerta");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_ReglaAlertaId",
                table: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_SensorId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "ReglaAlertaId",
                table: "Alertas");
        }
    }
}
