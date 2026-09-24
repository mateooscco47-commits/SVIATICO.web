document.addEventListener("DOMContentLoaded", function () {

    const buscador = document.getElementById("buscarLiquidacion");
    const limpiarBusqueda = document.getElementById("limpiarBusquedaLiquidacion");
    const fechaDesde = document.getElementById("fechaDesdeRendicion");
    const fechaHasta = document.getElementById("fechaHastaRendicion");
    const limpiarFiltros = document.getElementById("limpiarFiltrosRendicion");
    const tabla = document.getElementById("tablaLiquidaciones");
    const cantidadVisible = document.getElementById("cantidadVisible");
    const filaSinResultados = document.getElementById("filaSinResultados");
    const filaSinRegistros = document.getElementById("filaSinRegistros");
    const botonesEstado = document.querySelectorAll(".filtro-btn");

    if (!tabla) {
        return;
    }

    const filas = tabla.querySelectorAll("tbody tr[data-fecha]");

    let estadoSeleccionado = "todos";

    function normalizarTexto(texto) {
        return String(texto || "")
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/\s+/g, " ")
            .trim();
    }

    function filtrarLiquidaciones() {

        const texto = buscador
            ? normalizarTexto(buscador.value)
            : "";

        const desde = fechaDesde
            ? fechaDesde.value
            : "";

        const hasta = fechaHasta
            ? fechaHasta.value
            : "";

        let cantidad = 0;

        filas.forEach(function (fila) {

            const contenido = normalizarTexto(fila.textContent);

            const busqueda = normalizarTexto(
                fila.getAttribute("data-busqueda") || ""
            );

            const zona = normalizarTexto(
                fila.getAttribute("data-zona") || ""
            );

            const estado = normalizarTexto(
                fila.getAttribute("data-estado") || ""
            );

            const reembolso = normalizarTexto(
                fila.getAttribute("data-reembolso") || ""
            );

            const fecha =
                fila.getAttribute("data-fecha") || "";

            /*
             * BUSQUEDA
             *
             * Se revisa:
             * - contenido visible de la fila
             * - data-busqueda
             * - zona
             * - estado
             */
            const coincideTexto =
                texto === "" ||
                contenido.includes(texto) ||
                busqueda.includes(texto) ||
                zona.includes(texto) ||
                estado.includes(texto);

            /*
             * ESTADO
             */
            let coincideEstado = true;

            if (estadoSeleccionado !== "todos") {

                if (estadoSeleccionado === "reembolso") {

                    coincideEstado =
                        reembolso === "true";

                } else {

                    coincideEstado =
                        estado === estadoSeleccionado;

                }
            }

            /*
             * FECHA DESDE
             */
            const coincideDesde =
                desde === "" ||
                fecha >= desde;

            /*
             * FECHA HASTA
             */
            const coincideHasta =
                hasta === "" ||
                fecha <= hasta;

            /*
             * RESULTADO FINAL
             */
            const mostrar =
                coincideTexto &&
                coincideEstado &&
                coincideDesde &&
                coincideHasta;

            fila.style.display =
                mostrar ? "" : "none";

            if (mostrar) {
                cantidad++;
            }

        });

        /*
         * CONTADOR
         */
        if (cantidadVisible) {
            cantidadVisible.textContent = cantidad;
        }

        /*
         * SIN RESULTADOS
         */
        if (filaSinResultados) {

            filaSinResultados.style.display =
                filas.length > 0 && cantidad === 0
                    ? "table-row"
                    : "none";
        }

        /*
         * SIN REGISTROS
         */
        if (filaSinRegistros) {

            filaSinRegistros.style.display =
                filas.length === 0
                    ? "table-row"
                    : "none";
        }
    }

    /*
     * BOTONES DE ESTADO
     */
    botonesEstado.forEach(function (boton) {

        boton.addEventListener("click", function (event) {

            event.preventDefault();

            botonesEstado.forEach(function (item) {
                item.classList.remove("active");
            });

            this.classList.add("active");

            estadoSeleccionado =
                normalizarTexto(
                    this.getAttribute("data-estado") || "todos"
                );

            filtrarLiquidaciones();
        });
    });

    /*
     * BUSCADOR
     */
    if (buscador) {

        buscador.addEventListener(
            "input",
            filtrarLiquidaciones
        );
    }

    /*
     * FECHA DESDE
     */
    if (fechaDesde) {

        fechaDesde.addEventListener(
            "change",
            function () {

                if (
                    fechaHasta &&
                    fechaDesde.value !== "" &&
                    fechaHasta.value !== "" &&
                    fechaDesde.value > fechaHasta.value
                ) {
                    fechaHasta.value =
                        fechaDesde.value;
                }

                filtrarLiquidaciones();
            }
        );
    }

    /*
     * FECHA HASTA
     */
    if (fechaHasta) {

        fechaHasta.addEventListener(
            "change",
            function () {

                if (
                    fechaDesde &&
                    fechaDesde.value !== "" &&
                    fechaHasta.value !== "" &&
                    fechaHasta.value < fechaDesde.value
                ) {
                    fechaDesde.value =
                        fechaHasta.value;
                }

                filtrarLiquidaciones();
            }
        );
    }

    /*
     * LIMPIAR BUSQUEDA
     */
    if (limpiarBusqueda) {

        limpiarBusqueda.addEventListener(
            "click",
            function () {

                if (buscador) {
                    buscador.value = "";
                }

                filtrarLiquidaciones();

                if (buscador) {
                    buscador.focus();
                }
            }
        );
    }

    /*
     * LIMPIAR TODOS LOS FILTROS
     */
    if (limpiarFiltros) {

        limpiarFiltros.addEventListener(
            "click",
            function () {

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

                const botonTodos =
                    document.querySelector(
                        '.filtro-btn[data-estado="todos"]'
                    );

                if (botonTodos) {
                    botonTodos.classList.add("active");
                }

                filtrarLiquidaciones();

                if (buscador) {
                    buscador.focus();
                }
            }
        );
    }

    /*
     * ESTADO INICIAL
     */
    const botonTodos =
        document.querySelector(
            '.filtro-btn[data-estado="todos"]'
        );

    if (botonTodos) {
        botonTodos.classList.add("active");
    }

    /*
     * FILTRO INICIAL
     */
    filtrarLiquidaciones();

});