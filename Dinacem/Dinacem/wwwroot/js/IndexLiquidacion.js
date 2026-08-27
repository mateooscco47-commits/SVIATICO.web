document.addEventListener("DOMContentLoaded", function () {

    const buscador = document.getElementById("buscarLiquidacion");
    const btnLimpiarBusqueda = document.getElementById("limpiarBusquedaLiquidacion");
    const fechaDesde = document.getElementById("fechaDesdeRendicion");
    const fechaHasta = document.getElementById("fechaHastaRendicion");
    const btnLimpiarFiltros = document.getElementById("limpiarFiltrosRendicion");
    const cantidadVisible = document.getElementById("cantidadVisible");
    const filaSinResultados = document.getElementById("filaSinResultados");
    const filaSinRegistros = document.getElementById("filaSinRegistros");
    const botonesEstado = document.querySelectorAll(".filtro-btn");
    const filas = document.querySelectorAll("#tablaLiquidaciones tbody tr[data-estado]");

    let estadoSeleccionado = "todos";

    function normalizarTexto(texto) {
        return texto
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .trim();
    }

    function actualizarVisibilidadBotonLimpiar() {
        if (!buscador || !btnLimpiarBusqueda) {
            return;
        }

        if (buscador.value.trim() !== "") {
            btnLimpiarBusqueda.style.display = "flex";
        } else {
            btnLimpiarBusqueda.style.display = "none";
        }
    }

    function aplicarFiltros() {

        const textoBusqueda = normalizarTexto(
            buscador ? buscador.value : ""
        );

        const desde = fechaDesde ? fechaDesde.value : "";
        const hasta = fechaHasta ? fechaHasta.value : "";

        let cantidad = 0;

        filas.forEach(function (fila) {

            const estado = fila.dataset.estado || "";
            const tieneReembolso = fila.dataset.reembolso === "true";
            const fecha = fila.dataset.fecha || "";
            const texto = normalizarTexto(
                fila.dataset.busqueda || fila.textContent
            );

            let mostrar = true;

            if (textoBusqueda !== "" && !texto.includes(textoBusqueda)) {
                mostrar = false;
            }

            if (estadoSeleccionado !== "todos") {

                if (estadoSeleccionado === "reembolso") {

                    if (!tieneReembolso) {
                        mostrar = false;
                    }

                } else {

                    if (estado !== estadoSeleccionado) {
                        mostrar = false;
                    }

                }
            }

            if (desde !== "" && fecha !== "" && fecha < desde) {
                mostrar = false;
            }

            if (hasta !== "" && fecha !== "" && fecha > hasta) {
                mostrar = false;
            }

            fila.style.display = mostrar ? "" : "table-row";

            if (mostrar) {
                cantidad++;
            }
        });

        if (cantidadVisible) {
            cantidadVisible.textContent = cantidad;
        }

        if (filaSinResultados) {
            if (filas.length > 0 && cantidad === 0) {
                filaSinResultados.style.display = "table-row";
            } else {
                filaSinResultados.style.display = "none";
            }
        }

        if (filaSinRegistros) {
            if (filas.length === 0) {
                filaSinRegistros.style.display = "table-row";
            }
        }

        actualizarVisibilidadBotonLimpiar();
    }

    botonesEstado.forEach(function (boton) {

        boton.addEventListener("click", function () {

            botonesEstado.forEach(function (item) {
                item.classList.remove("active");
            });

            this.classList.add("active");

            estadoSeleccionado = this.dataset.estado || "todos";

            aplicarFiltros();
        });
    });

    if (buscador) {

        buscador.addEventListener("input", function () {
            aplicarFiltros();
        });

        buscador.addEventListener("search", function () {
            aplicarFiltros();
        });
    }

    if (btnLimpiarBusqueda) {

        btnLimpiarBusqueda.addEventListener("click", function () {

            if (buscador) {
                buscador.value = "";
                buscador.focus();
            }

            aplicarFiltros();
        });
    }

    if (fechaDesde) {

        fechaDesde.addEventListener("change", function () {

            if (
                fechaHasta &&
                fechaHasta.value !== "" &&
                fechaDesde.value !== "" &&
                fechaDesde.value > fechaHasta.value
            ) {
                fechaHasta.value = fechaDesde.value;
            }

            aplicarFiltros();
        });
    }

    if (fechaHasta) {

        fechaHasta.addEventListener("change", function () {

            if (
                fechaDesde &&
                fechaDesde.value !== "" &&
                fechaHasta.value !== "" &&
                fechaHasta.value < fechaDesde.value
            ) {
                fechaDesde.value = fechaHasta.value;
            }

            aplicarFiltros();
        });
    }

    if (btnLimpiarFiltros) {

        btnLimpiarFiltros.addEventListener("click", function () {

            if (buscador) {
                buscador.value = "";
            }

            if (fechaDesde) {
                fechaDesde.value = "";
            }

            if (fechaHasta) {
                fechaHasta.value = "";
            }

            estadoSeleccionado = "todos";

            botonesEstado.forEach(function (boton) {
                boton.classList.remove("active");
            });

            const botonTodos = document.querySelector(
                '.filtro-btn[data-estado="todos"]'
            );

            if (botonTodos) {
                botonTodos.classList.add("active");
            }

            aplicarFiltros();

            if (buscador) {
                buscador.focus();
            }
        });
    }

    if (buscador) {
        actualizarVisibilidadBotonLimpiar();
    }

    aplicarFiltros();
});