using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinacen.Migrations
{
    /// <inheritdoc />
    public partial class CrearBaseLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadoReembolso",
                columns: table => new
                {
                    IdEstadoReembolso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoReembolso", x => x.IdEstadoReembolso);
                });

            migrationBuilder.CreateTable(
                name: "EstadoRendicion",
                columns: table => new
                {
                    IdEstadoRendicion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoRendicion", x => x.IdEstadoRendicion);
                });

            migrationBuilder.CreateTable(
                name: "EstadoSolicitud",
                columns: table => new
                {
                    IdEstadoSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoSolicitud", x => x.IdEstadoSolicitud);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "TipoComprobante",
                columns: table => new
                {
                    IdTipoComprobante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoComprobante", x => x.IdTipoComprobante);
                });

            migrationBuilder.CreateTable(
                name: "TipoGasto",
                columns: table => new
                {
                    IdTipoGasto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoGasto", x => x.IdTipoGasto);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRol = table.Column<int>(type: "int", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contrasenia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zona = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioAcceso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IdEstadoSolicitud = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.IdSolicitud);
                    table.ForeignKey(
                        name: "FK_Solicitudes_EstadoSolicitud_IdEstadoSolicitud",
                        column: x => x.IdEstadoSolicitud,
                        principalTable: "EstadoSolicitud",
                        principalColumn: "IdEstadoSolicitud");
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "DevolucionesSaldo",
                columns: table => new
                {
                    IdDevolucionSaldo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRendicion = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroOperacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Voucher = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesSaldo", x => x.IdDevolucionSaldo);
                });

            migrationBuilder.CreateTable(
                name: "Rendiciones",
                columns: table => new
                {
                    IdRendicion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSolicitud = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IdEstadoRendicion = table.Column<int>(type: "int", nullable: false),
                    DevolucionSaldoIdDevolucionSaldo = table.Column<int>(type: "int", nullable: true),
                    ArchivoPdf = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEnvioRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rendiciones", x => x.IdRendicion);
                    table.ForeignKey(
                        name: "FK_Rendiciones_DevolucionesSaldo_DevolucionSaldoIdDevolucionSaldo",
                        column: x => x.DevolucionSaldoIdDevolucionSaldo,
                        principalTable: "DevolucionesSaldo",
                        principalColumn: "IdDevolucionSaldo");
                    table.ForeignKey(
                        name: "FK_Rendiciones_EstadoRendicion_IdEstadoRendicion",
                        column: x => x.IdEstadoRendicion,
                        principalTable: "EstadoRendicion",
                        principalColumn: "IdEstadoRendicion");
                    table.ForeignKey(
                        name: "FK_Rendiciones_Solicitudes_IdSolicitud",
                        column: x => x.IdSolicitud,
                        principalTable: "Solicitudes",
                        principalColumn: "IdSolicitud");
                    table.ForeignKey(
                        name: "FK_Rendiciones_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Gastos",
                columns: table => new
                {
                    IdGasto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRendicion = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdTipoGasto = table.Column<int>(type: "int", nullable: false),
                    IdTipoComprobante = table.Column<int>(type: "int", nullable: false),
                    Ruc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    RazonSocial = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DomicilioFiscal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Serie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Comprobante = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorVenta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IGV = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExoneracionIGV = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gastos", x => x.IdGasto);
                    table.ForeignKey(
                        name: "FK_Gastos_Rendiciones_IdRendicion",
                        column: x => x.IdRendicion,
                        principalTable: "Rendiciones",
                        principalColumn: "IdRendicion");
                    table.ForeignKey(
                        name: "FK_Gastos_TipoComprobante_IdTipoComprobante",
                        column: x => x.IdTipoComprobante,
                        principalTable: "TipoComprobante",
                        principalColumn: "IdTipoComprobante");
                    table.ForeignKey(
                        name: "FK_Gastos_TipoGasto_IdTipoGasto",
                        column: x => x.IdTipoGasto,
                        principalTable: "TipoGasto",
                        principalColumn: "IdTipoGasto");
                });

            migrationBuilder.CreateTable(
                name: "Reembolsos",
                columns: table => new
                {
                    IdReembolso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRendicion = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdEstadoReembolso = table.Column<int>(type: "int", nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumeroOperacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ComprobantePago = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reembolsos", x => x.IdReembolso);
                    table.ForeignKey(
                        name: "FK_Reembolsos_EstadoReembolso_IdEstadoReembolso",
                        column: x => x.IdEstadoReembolso,
                        principalTable: "EstadoReembolso",
                        principalColumn: "IdEstadoReembolso");
                    table.ForeignKey(
                        name: "FK_Reembolsos_Rendiciones_IdRendicion",
                        column: x => x.IdRendicion,
                        principalTable: "Rendiciones",
                        principalColumn: "IdRendicion");
                    table.ForeignKey(
                        name: "FK_Reembolsos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesSaldo_IdRendicion",
                table: "DevolucionesSaldo",
                column: "IdRendicion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_IdRendicion",
                table: "Gastos",
                column: "IdRendicion");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_IdTipoComprobante",
                table: "Gastos",
                column: "IdTipoComprobante");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_IdTipoGasto",
                table: "Gastos",
                column: "IdTipoGasto");

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdEstadoReembolso",
                table: "Reembolsos",
                column: "IdEstadoReembolso");

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdRendicion",
                table: "Reembolsos",
                column: "IdRendicion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdUsuario",
                table: "Reembolsos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Rendiciones_DevolucionSaldoIdDevolucionSaldo",
                table: "Rendiciones",
                column: "DevolucionSaldoIdDevolucionSaldo");

            migrationBuilder.CreateIndex(
                name: "IX_Rendiciones_IdEstadoRendicion",
                table: "Rendiciones",
                column: "IdEstadoRendicion");

            migrationBuilder.CreateIndex(
                name: "IX_Rendiciones_IdSolicitud",
                table: "Rendiciones",
                column: "IdSolicitud",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rendiciones_IdUsuario",
                table: "Rendiciones",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_IdEstadoSolicitud",
                table: "Solicitudes",
                column: "IdEstadoSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_IdUsuario",
                table: "Solicitudes",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdRol",
                table: "Usuarios",
                column: "IdRol");

            migrationBuilder.AddForeignKey(
                name: "FK_DevolucionesSaldo_Rendiciones_IdRendicion",
                table: "DevolucionesSaldo",
                column: "IdRendicion",
                principalTable: "Rendiciones",
                principalColumn: "IdRendicion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevolucionesSaldo_Rendiciones_IdRendicion",
                table: "DevolucionesSaldo");

            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropTable(
                name: "Reembolsos");

            migrationBuilder.DropTable(
                name: "TipoComprobante");

            migrationBuilder.DropTable(
                name: "TipoGasto");

            migrationBuilder.DropTable(
                name: "EstadoReembolso");

            migrationBuilder.DropTable(
                name: "Rendiciones");

            migrationBuilder.DropTable(
                name: "DevolucionesSaldo");

            migrationBuilder.DropTable(
                name: "EstadoRendicion");

            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "EstadoSolicitud");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
