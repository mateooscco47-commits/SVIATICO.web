using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinacen.Migrations
{
    /// <inheritdoc />
    public partial class AgregarZonaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gastos_TipoComprobante_IdTipoComprobante",
                table: "Gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_Gastos_TipoGasto_IdTipoGasto",
                table: "Gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_Reembolsos_EstadoReembolso_IdEstadoReembolso",
                table: "Reembolsos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rendiciones_DevolucionesSaldo_DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Rendiciones_EstadoRendicion_IdEstadoRendicion",
                table: "Rendiciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_EstadoSolicitud_IdEstadoSolicitud",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Rendiciones_DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones");

            migrationBuilder.DropIndex(
                name: "IX_Reembolsos_IdRendicion",
                table: "Reembolsos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoGasto",
                table: "TipoGasto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoComprobante",
                table: "TipoComprobante");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoSolicitud",
                table: "EstadoSolicitud");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoRendicion",
                table: "EstadoRendicion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoReembolso",
                table: "EstadoReembolso");

            migrationBuilder.DropColumn(
                name: "Zona",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones");

            migrationBuilder.RenameTable(
                name: "TipoGasto",
                newName: "TipoGastos");

            migrationBuilder.RenameTable(
                name: "TipoComprobante",
                newName: "TipoComprobantes");

            migrationBuilder.RenameTable(
                name: "EstadoSolicitud",
                newName: "EstadoSolicitudes");

            migrationBuilder.RenameTable(
                name: "EstadoRendicion",
                newName: "EstadoRendiciones");

            migrationBuilder.RenameTable(
                name: "EstadoReembolso",
                newName: "EstadoReembolsos");

            migrationBuilder.AlterColumn<string>(
                name: "Nombres",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Usuarios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apellidos",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "IdZona",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoGastos",
                table: "TipoGastos",
                column: "IdTipoGasto");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoComprobantes",
                table: "TipoComprobantes",
                column: "IdTipoComprobante");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoSolicitudes",
                table: "EstadoSolicitudes",
                column: "IdEstadoSolicitud");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoRendiciones",
                table: "EstadoRendiciones",
                column: "IdEstadoRendicion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoReembolsos",
                table: "EstadoReembolsos",
                column: "IdEstadoReembolso");

            migrationBuilder.CreateTable(
                name: "Zonas",
                columns: table => new
                {
                    IdZona = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoZona = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zonas", x => x.IdZona);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdZona",
                table: "Usuarios",
                column: "IdZona");

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdRendicion",
                table: "Reembolsos",
                column: "IdRendicion");

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_CodigoZona",
                table: "Zonas",
                column: "CodigoZona",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Gastos_TipoComprobantes_IdTipoComprobante",
                table: "Gastos",
                column: "IdTipoComprobante",
                principalTable: "TipoComprobantes",
                principalColumn: "IdTipoComprobante");

            migrationBuilder.AddForeignKey(
                name: "FK_Gastos_TipoGastos_IdTipoGasto",
                table: "Gastos",
                column: "IdTipoGasto",
                principalTable: "TipoGastos",
                principalColumn: "IdTipoGasto");

            migrationBuilder.AddForeignKey(
                name: "FK_Reembolsos_EstadoReembolsos_IdEstadoReembolso",
                table: "Reembolsos",
                column: "IdEstadoReembolso",
                principalTable: "EstadoReembolsos",
                principalColumn: "IdEstadoReembolso");

            migrationBuilder.AddForeignKey(
                name: "FK_Rendiciones_EstadoRendiciones_IdEstadoRendicion",
                table: "Rendiciones",
                column: "IdEstadoRendicion",
                principalTable: "EstadoRendiciones",
                principalColumn: "IdEstadoRendicion");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_EstadoSolicitudes_IdEstadoSolicitud",
                table: "Solicitudes",
                column: "IdEstadoSolicitud",
                principalTable: "EstadoSolicitudes",
                principalColumn: "IdEstadoSolicitud");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Zonas_IdZona",
                table: "Usuarios",
                column: "IdZona",
                principalTable: "Zonas",
                principalColumn: "IdZona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gastos_TipoComprobantes_IdTipoComprobante",
                table: "Gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_Gastos_TipoGastos_IdTipoGasto",
                table: "Gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_Reembolsos_EstadoReembolsos_IdEstadoReembolso",
                table: "Reembolsos");

            migrationBuilder.DropForeignKey(
                name: "FK_Rendiciones_EstadoRendiciones_IdEstadoRendicion",
                table: "Rendiciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_EstadoSolicitudes_IdEstadoSolicitud",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Zonas_IdZona",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Zonas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_IdZona",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Reembolsos_IdRendicion",
                table: "Reembolsos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoGastos",
                table: "TipoGastos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoComprobantes",
                table: "TipoComprobantes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoSolicitudes",
                table: "EstadoSolicitudes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoRendiciones",
                table: "EstadoRendiciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstadoReembolsos",
                table: "EstadoReembolsos");

            migrationBuilder.DropColumn(
                name: "IdZona",
                table: "Usuarios");

            migrationBuilder.RenameTable(
                name: "TipoGastos",
                newName: "TipoGasto");

            migrationBuilder.RenameTable(
                name: "TipoComprobantes",
                newName: "TipoComprobante");

            migrationBuilder.RenameTable(
                name: "EstadoSolicitudes",
                newName: "EstadoSolicitud");

            migrationBuilder.RenameTable(
                name: "EstadoRendiciones",
                newName: "EstadoRendicion");

            migrationBuilder.RenameTable(
                name: "EstadoReembolsos",
                newName: "EstadoReembolso");

            migrationBuilder.AlterColumn<string>(
                name: "Nombres",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apellidos",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Zona",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoGasto",
                table: "TipoGasto",
                column: "IdTipoGasto");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoComprobante",
                table: "TipoComprobante",
                column: "IdTipoComprobante");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoSolicitud",
                table: "EstadoSolicitud",
                column: "IdEstadoSolicitud");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoRendicion",
                table: "EstadoRendicion",
                column: "IdEstadoRendicion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstadoReembolso",
                table: "EstadoReembolso",
                column: "IdEstadoReembolso");

            migrationBuilder.CreateIndex(
                name: "IX_Rendiciones_DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones",
                column: "DevolucionSaldoIdDevolucionSaldo");

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdRendicion",
                table: "Reembolsos",
                column: "IdRendicion",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Gastos_TipoComprobante_IdTipoComprobante",
                table: "Gastos",
                column: "IdTipoComprobante",
                principalTable: "TipoComprobante",
                principalColumn: "IdTipoComprobante");

            migrationBuilder.AddForeignKey(
                name: "FK_Gastos_TipoGasto_IdTipoGasto",
                table: "Gastos",
                column: "IdTipoGasto",
                principalTable: "TipoGasto",
                principalColumn: "IdTipoGasto");

            migrationBuilder.AddForeignKey(
                name: "FK_Reembolsos_EstadoReembolso_IdEstadoReembolso",
                table: "Reembolsos",
                column: "IdEstadoReembolso",
                principalTable: "EstadoReembolso",
                principalColumn: "IdEstadoReembolso");

            migrationBuilder.AddForeignKey(
                name: "FK_Rendiciones_DevolucionesSaldo_DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones",
                column: "DevolucionSaldoIdDevolucionSaldo",
                principalTable: "DevolucionesSaldo",
                principalColumn: "IdDevolucionSaldo");

            migrationBuilder.AddForeignKey(
                name: "FK_Rendiciones_EstadoRendicion_IdEstadoRendicion",
                table: "Rendiciones",
                column: "IdEstadoRendicion",
                principalTable: "EstadoRendicion",
                principalColumn: "IdEstadoRendicion");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_EstadoSolicitud_IdEstadoSolicitud",
                table: "Solicitudes",
                column: "IdEstadoSolicitud",
                principalTable: "EstadoSolicitud",
                principalColumn: "IdEstadoSolicitud");
        }
    }
}
