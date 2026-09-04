using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinacen.Migrations
{
    /// <inheritdoc />
    public partial class DinacenDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasHospedaje",
                table: "Gastos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasHospedaje",
                table: "Gastos");
        }
    }
}
