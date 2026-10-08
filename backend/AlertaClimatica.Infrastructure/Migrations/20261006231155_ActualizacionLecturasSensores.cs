using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertaClimatica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionLecturasSensores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LecturasClimaticas_SensorId",
                table: "LecturasClimaticas",
                column: "SensorId");

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
                name: "FK_LecturasClimaticas_Sensores_SensorId",
                table: "LecturasClimaticas");

            migrationBuilder.DropIndex(
                name: "IX_LecturasClimaticas_SensorId",
                table: "LecturasClimaticas");
        }
    }
}
