using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertaClimatica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMotorSimulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorSimulado",
                table: "Sensores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Alertas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Activa");

            migrationBuilder.AddColumn<int>(
                name: "EventoId",
                table: "Alertas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaGestion",
                table: "Alertas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioResponsableId",
                table: "Alertas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDetectado",
                table: "Alertas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_EventoId",
                table: "Alertas",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_UsuarioResponsableId",
                table: "Alertas",
                column: "UsuarioResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Eventos_EventoId",
                table: "Alertas",
                column: "EventoId",
                principalTable: "Eventos",
                principalColumn: "EventoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Usuarios_UsuarioResponsableId",
                table: "Alertas",
                column: "UsuarioResponsableId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Eventos_EventoId",
                table: "Alertas");

            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Usuarios_UsuarioResponsableId",
                table: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_EventoId",
                table: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_UsuarioResponsableId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "ValorSimulado",
                table: "Sensores");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "EventoId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "FechaGestion",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "UsuarioResponsableId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "ValorDetectado",
                table: "Alertas");
        }
    }
}
