using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mmdba.Migrations
{
    /// <inheritdoc />
    public partial class PadronizaTabelaVelocidadeInstMaquina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Velocidade",
                table: "VelocidadeInstMaquina");

            migrationBuilder.RenameColumn(
                name: "IdMaquina",
                table: "VelocidadeInstMaquina",
                newName: "Valor");

            migrationBuilder.AddColumn<string>(
                name: "CodigoEvento",
                table: "VelocidadeInstMaquina",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Informacao",
                table: "VelocidadeInstMaquina",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "VelocidadeInstMaquina",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoEvento",
                table: "VelocidadeInstMaquina",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Valor",
                table: "EventosMaquina",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Informacao",
                table: "EventosMaquina",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEvento",
                table: "VelocidadeInstMaquina");

            migrationBuilder.DropColumn(
                name: "Informacao",
                table: "VelocidadeInstMaquina");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "VelocidadeInstMaquina");

            migrationBuilder.DropColumn(
                name: "TipoEvento",
                table: "VelocidadeInstMaquina");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "VelocidadeInstMaquina",
                newName: "IdMaquina");

            migrationBuilder.AddColumn<double>(
                name: "Velocidade",
                table: "VelocidadeInstMaquina",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "Valor",
                table: "EventosMaquina",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Informacao",
                table: "EventosMaquina",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
