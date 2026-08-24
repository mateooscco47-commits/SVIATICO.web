document.addEventListener("DOMContentLoaded", function () {

    // ============================================================
    // ELEMENTOS
    // ============================================================

    const buscador =
        document.getElementById("buscadorSolicitudes");

    const fechaDesde =
        document.getElementById("fechaDesde");

    const fechaHasta =
        document.getElementById("fechaHasta");

    const limpiarBusqueda =
        document.getElementById("limpiarBusqueda");

    const limpiarFiltros =
        document.getElementById("limpiarFiltros");

    const tabla =
        document.getElementById("tablaSolicitudes");


    // ============================================================
    // VERIFICAR TABLA
    // ============================================================

    if (!tabla) {

        console.warn(
            "No se encontró la tabla #tablaSolicitudes."
        );

        return;
    }


    // ============================================================
    // FILAS DE LA TABLA
    // ============================================================

    const filas =
        tabla.querySelectorAll("tbody tr");


    // ============================================================
    // FUNCIÓN PRINCIPAL DE FILTRADO
    // ============================================================

    function filtrarSolicitudes() {

        // --------------------------------------------------------
        // TEXTO DEL BUSCADOR
        // --------------------------------------------------------

        const texto =
            buscador
                ? buscador.value.trim().toLowerCase()
                : "";


        // --------------------------------------------------------
        // FECHA DESDE
        // --------------------------------------------------------

        const desde =
            fechaDesde
                ? fechaDesde.value
                : "";


        // --------------------------------------------------------
        // FECHA HASTA
        // --------------------------------------------------------

        const hasta =
            fechaHasta
                ? fechaHasta.value
                : "";


        // --------------------------------------------------------
        // RECORRER FILAS
        // --------------------------------------------------------

        filas.forEach(function (fila) {

            // ====================================================
            // TEXTO DE LA FILA
            // ====================================================

            const contenido =
                fila.textContent
                    .trim()
                    .toLowerCase();


            // ====================================================
            // FECHA DE ENVÍO
            //
            // Esta fecha viene de:
            //
            // data-fecha="2026-08-23"
            // ====================================================

            const fechaSolicitud =
                fila.dataset.fecha || "";


            // ====================================================
            // FILTRO DE TEXTO
            // ====================================================

            const coincideTexto =
                texto === "" ||
                contenido.includes(texto);


            // ====================================================
            // FILTRO "DESDE"
            // ====================================================

            let coincideDesde = true;

            if (desde !== "") {

                coincideDesde =
                    fechaSolicitud !== "" &&
                    fechaSolicitud >= desde;

            }


            // ====================================================
            // FILTRO "HASTA"
            // ====================================================

            let coincideHasta = true;

            if (hasta !== "") {

                coincideHasta =
                    fechaSolicitud !== "" &&
                    fechaSolicitud <= hasta;

            }


            // ====================================================
            // RESULTADO FINAL
            // ====================================================

            const mostrar =
                coincideTexto &&
                coincideDesde &&
                coincideHasta;


            // ====================================================
            // MOSTRAR / OCULTAR
            // ====================================================

            fila.style.display =
                mostrar ? "" : "none";

        });

    }


    // ============================================================
    // BUSCADOR DE TEXTO
    // ============================================================

    if (buscador) {

        buscador.addEventListener(
            "input",
            filtrarSolicitudes
        );

    }


    // ============================================================
    // FECHA DESDE
    // ============================================================

    if (fechaDesde) {

        fechaDesde.addEventListener(
            "change",
            filtrarSolicitudes
        );

    }


    // ============================================================
    // FECHA HASTA
    // ============================================================

    if (fechaHasta) {

        fechaHasta.addEventListener(
            "change",
            filtrarSolicitudes
        );

    }


    // ============================================================
    // LIMPIAR SOLO EL BUSCADOR
    // ============================================================

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


    // ============================================================
    // LIMPIAR TODOS LOS FILTROS
    // ============================================================

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

                filtrarSolicitudes();

            }
        );

    }

});