using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinacen.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBitacoraVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitacorasVehiculo",
                columns: table => new
                {
                    IdBitacoraVehiculo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRendicion = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DistanciaKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoAsignado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacorasVehiculo", x => x.IdBitacoraVehiculo);
                    table.ForeignKey(
                        name: "FK_BitacorasVehiculo_Rendiciones_IdRendicion",
                        column: x => x.IdRendicion,
                        principalTable: "Rendiciones",
                        principalColumn: "IdRendicion");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitacorasVehiculo_IdRendicion",
                table: "BitacorasVehiculo",
                column: "IdRendicion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitacorasVehiculo");
        }
    }
}
