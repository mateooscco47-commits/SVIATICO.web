using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinacen.Migrations
{
    /// <inheritdoc />
    public partial class BaselineActual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionSistema",
                columns: table => new
                {
                    IdConfiguracion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TarifaKilometro = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSistema", x => x.IdConfiguracion);
                });

            migrationBuilder.CreateTable(
                name: "EstadoReembolsos",
                columns: table => new
                {
                    IdEstadoReembolso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoReembolsos", x => x.IdEstadoReembolso);
                });

            migrationBuilder.CreateTable(
                name: "EstadoRendiciones",
                columns: table => new
                {
                    IdEstadoRendicion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoRendiciones", x => x.IdEstadoRendicion);
                });

            migrationBuilder.CreateTable(
                name: "EstadoSolicitudes",
                columns: table => new
                {
                    IdEstadoSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoSolicitudes", x => x.IdEstadoSolicitud);
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
                name: "TipoComprobantes",
                columns: table => new
                {
                    IdTipoComprobante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoComprobantes", x => x.IdTipoComprobante);
                });

            migrationBuilder.CreateTable(
                name: "TipoGastos",
                columns: table => new
                {
                    IdTipoGasto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoGastos", x => x.IdTipoGasto);
                });

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

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRol = table.Column<int>(type: "int", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdZona = table.Column<int>(type: "int", nullable: true),
                    UsuarioAcceso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contrasenia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Usuarios_Zonas_IdZona",
                        column: x => x.IdZona,
                        principalTable: "Zonas",
                        principalColumn: "IdZona");
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
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RutaComprobante = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.IdSolicitud);
                    table.ForeignKey(
                        name: "FK_Solicitudes_EstadoSolicitudes_IdEstadoSolicitud",
                        column: x => x.IdEstadoSolicitud,
                        principalTable: "EstadoSolicitudes",
                        principalColumn: "IdEstadoSolicitud");
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
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
                    ArchivoPdf = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEnvioRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rendiciones", x => x.IdRendicion);
                    table.ForeignKey(
                        name: "FK_Rendiciones_EstadoRendiciones_IdEstadoRendicion",
                        column: x => x.IdEstadoRendicion,
                        principalTable: "EstadoRendiciones",
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
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TarifaKilometro = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_DevolucionesSaldo_Rendiciones_IdRendicion",
                        column: x => x.IdRendicion,
                        principalTable: "Rendiciones",
                        principalColumn: "IdRendicion");
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
                        name: "FK_Gastos_TipoComprobantes_IdTipoComprobante",
                        column: x => x.IdTipoComprobante,
                        principalTable: "TipoComprobantes",
                        principalColumn: "IdTipoComprobante");
                    table.ForeignKey(
                        name: "FK_Gastos_TipoGastos_IdTipoGasto",
                        column: x => x.IdTipoGasto,
                        principalTable: "TipoGastos",
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
                        name: "FK_Reembolsos_EstadoReembolsos_IdEstadoReembolso",
                        column: x => x.IdEstadoReembolso,
                        principalTable: "EstadoReembolsos",
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
                name: "IX_BitacorasVehiculo_IdRendicion",
                table: "BitacorasVehiculo",
                column: "IdRendicion");

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
                column: "IdRendicion");

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_IdUsuario",
                table: "Reembolsos",
                column: "IdUsuario");

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

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdZona",
                table: "Usuarios",
                column: "IdZona");

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_CodigoZona",
                table: "Zonas",
                column: "CodigoZona",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitacorasVehiculo");

            migrationBuilder.DropTable(
                name: "ConfiguracionSistema");

            migrationBuilder.DropTable(
                name: "DevolucionesSaldo");

            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropTable(
                name: "Reembolsos");

            migrationBuilder.DropTable(
                name: "TipoComprobantes");

            migrationBuilder.DropTable(
                name: "TipoGastos");

            migrationBuilder.DropTable(
                name: "EstadoReembolsos");

            migrationBuilder.DropTable(
                name: "Rendiciones");

            migrationBuilder.DropTable(
                name: "EstadoRendiciones");

            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "EstadoSolicitudes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Zonas");
        }
    }
}
