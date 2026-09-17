document.addEventListener("DOMContentLoaded", function () {

    // =========================================================
    // ELEMENTOS
    // =========================================================

    const buscador =
        document.getElementById("buscadorSolicitudes");

    const limpiarBusqueda =
        document.getElementById("limpiarBusqueda");

    const fechaDesde =
        document.getElementById("fechaDesde");

    const fechaHasta =
        document.getElementById("fechaHasta");

    const limpiarFiltros =
        document.getElementById("limpiarFiltros");

    const tabla =
        document.getElementById("tablaSolicitudes");


    // =========================================================
    // VALIDAR TABLA
    // =========================================================

    if (!tabla) return;


    // =========================================================
    // FILAS DE LA TABLA
    // =========================================================

    const filas =
        tabla.querySelectorAll("tbody tr[data-fecha]");


    // =========================================================
    // FUNCIÓN FILTRAR SOLICITUDES
    // =========================================================

    function filtrarSolicitudes() {

        // -----------------------------------------------------
        // TEXTO DE BÚSQUEDA
        // -----------------------------------------------------

        const texto =
            buscador
                ? buscador.value.toLowerCase().trim()
                : "";


        // -----------------------------------------------------
        // FECHAS
        // -----------------------------------------------------

        const desde =
            fechaDesde
                ? fechaDesde.value
                : "";

        const hasta =
            fechaHasta
                ? fechaHasta.value
                : "";


        // =====================================================
        // RECORRER FILAS
        // =====================================================

        filas.forEach(function (fila) {

            // -------------------------------------------------
            // CONTENIDO COMPLETO
            // -------------------------------------------------

            const contenido =
                fila.textContent.toLowerCase();


            // -------------------------------------------------
            // ZONA
            // -------------------------------------------------

            const zona =
                (fila.dataset.zona || "").toLowerCase();


            // -------------------------------------------------
            // ESTADO
            // -------------------------------------------------

            const estado =
                (fila.dataset.estado || "").toLowerCase();


            // -------------------------------------------------
            // FECHA
            // -------------------------------------------------

            const fecha =
                fila.dataset.fecha || "";


            // =================================================
            // BÚSQUEDA GENERAL
            // =================================================
            //
            // El buscador puede encontrar:
            // - Empleado
            // - Zona
            // - Destino
            // - Motivo
            // - Estado
            // - Cualquier otro texto visible de la fila
            //
            // =================================================

            const coincideTexto =
                texto === "" ||
                contenido.includes(texto) ||
                zona.includes(texto) ||
                estado.includes(texto);


            // =================================================
            // FECHA DESDE
            // =================================================

            const coincideDesde =
                desde === "" ||
                fecha >= desde;


            // =================================================
            // FECHA HASTA
            // =================================================

            const coincideHasta =
                hasta === "" ||
                fecha <= hasta;


            // =================================================
            // MOSTRAR / OCULTAR
            // =================================================

            const mostrar =
                coincideTexto &&
                coincideDesde &&
                coincideHasta;


            fila.style.display =
                mostrar ? "" : "none";

        });

    }


    // =========================================================
    // BUSCADOR
    // =========================================================

    if (buscador) {

        buscador.addEventListener(
            "input",
            filtrarSolicitudes
        );

    }


    // =========================================================
    // FECHA DESDE
    // =========================================================

    if (fechaDesde) {

        fechaDesde.addEventListener(
            "change",
            filtrarSolicitudes
        );

    }


    // =========================================================
    // FECHA HASTA
    // =========================================================

    if (fechaHasta) {

        fechaHasta.addEventListener(
            "change",
            filtrarSolicitudes
        );

    }


    // =========================================================
    // LIMPIAR BÚSQUEDA
    // =========================================================

    if (limpiarBusqueda) {

        limpiarBusqueda.addEventListener(
            "click",
            function () {

                if (buscador) {
                    buscador.value = "";
                }

                filtrarSolicitudes();

                if (buscador) {
                    buscador.focus();
                }

            }
        );

    }


    // =========================================================
    // LIMPIAR TODOS LOS FILTROS
    // =========================================================

    if (limpiarFiltros) {

        limpiarFiltros.addEventListener(
            "click",
            function () {

                // -------------------------------------------------
                // Limpiar buscador
                // -------------------------------------------------

                if (buscador) {
                    buscador.value = "";
                }


                // -------------------------------------------------
                // Limpiar fecha desde
                // -------------------------------------------------

                if (fechaDesde) {
                    fechaDesde.value = "";
                }


                // -------------------------------------------------
                // Limpiar fecha hasta
                // -------------------------------------------------

                if (fechaHasta) {
                    fechaHasta.value = "";
                }


                // -------------------------------------------------
                // Aplicar limpieza
                // -------------------------------------------------

                filtrarSolicitudes();


                // -------------------------------------------------
                // Devolver foco al buscador
                // -------------------------------------------------

                if (buscador) {
                    buscador.focus();
                }

            }
        );

    }


    // =========================================================
    // CERRAR ALERTAS AUTOMÁTICAMENTE
    // =========================================================

    const alertas =
        document.querySelectorAll(".alert");


    alertas.forEach(function (alerta) {

        setTimeout(function () {

            if (typeof bootstrap !== "undefined") {

                const instancia =
                    bootstrap.Alert.getOrCreateInstance(alerta);

                instancia.close();

            }

        }, 5000);

    });

});