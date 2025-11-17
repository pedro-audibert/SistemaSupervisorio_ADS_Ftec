using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mmdba.Migrations
{
    /// <inheritdoc />
    public partial class AddHabilitarRefugoManualToParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HabilitarRefugoManual",
                table: "OeeParametrosMaquina",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HabilitarRefugoManual",
                table: "OeeParametrosMaquina");
        }
    }
}
