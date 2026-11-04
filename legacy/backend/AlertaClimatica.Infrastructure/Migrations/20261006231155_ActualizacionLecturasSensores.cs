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
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LecturasClimaticas_SensorId' AND object_id = OBJECT_ID('LecturasClimaticas'))
    CREATE INDEX [IX_LecturasClimaticas_SensorId] ON [LecturasClimaticas] ([SensorId]);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LecturasClimaticas_Sensores_SensorId')
    ALTER TABLE [LecturasClimaticas] WITH NOCHECK ADD CONSTRAINT [FK_LecturasClimaticas_Sensores_SensorId]
        FOREIGN KEY ([SensorId]) REFERENCES [Sensores] ([SensorId]) ON DELETE CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LecturasClimaticas_Sensores_SensorId')
    ALTER TABLE [LecturasClimaticas] DROP CONSTRAINT [FK_LecturasClimaticas_Sensores_SensorId];");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LecturasClimaticas_SensorId' AND object_id = OBJECT_ID('LecturasClimaticas'))
    DROP INDEX [IX_LecturasClimaticas_SensorId] ON [LecturasClimaticas];");
        }
    }
}
