using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mmdba.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VelocidadeInstMaquina_Timestamp",
                table: "VelocidadeInstMaquina",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProducaoInstMaquina_Timestamp",
                table: "ProducaoInstMaquina",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMaquina_TipoEvento_Timestamp",
                table: "EventosMaquina",
                columns: new[] { "TipoEvento", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VelocidadeInstMaquina_Timestamp",
                table: "VelocidadeInstMaquina");

            migrationBuilder.DropIndex(
                name: "IX_ProducaoInstMaquina_Timestamp",
                table: "ProducaoInstMaquina");

            migrationBuilder.DropIndex(
                name: "IX_EventosMaquina_TipoEvento_Timestamp",
                table: "EventosMaquina");
        }
    }
}
