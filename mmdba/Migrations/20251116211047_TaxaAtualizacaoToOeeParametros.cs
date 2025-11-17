using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mmdba.Migrations
{
    /// <inheritdoc />
    public partial class TaxaAtualizacaoToOeeParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaxaAtualizacaoMinutos",
                table: "OeeParametrosMaquina",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxaAtualizacaoMinutos",
                table: "OeeParametrosMaquina");
        }
    }
}
