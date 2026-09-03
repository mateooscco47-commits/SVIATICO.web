document.addEventListener("DOMContentLoaded", function () {
    const buscador = document.getElementById("buscadorSolicitudes");
    const limpiarBusqueda = document.getElementById("limpiarBusqueda");
    const fechaDesde = document.getElementById("fechaDesde");
    const fechaHasta = document.getElementById("fechaHasta");
    const limpiarFiltros = document.getElementById("limpiarFiltros");
    const tabla = document.getElementById("tablaSolicitudes");

    if (!tabla) return;

    const filas = tabla.querySelectorAll("tbody tr[data-fecha]");

    function filtrarSolicitudes() {
        const texto = buscador
            ? buscador.value.toLowerCase().trim()
            : "";

        const desde = fechaDesde
            ? fechaDesde.value
            : "";

        const hasta = fechaHasta
            ? fechaHasta.value
            : "";

        filas.forEach(function (fila) {
            const contenido = fila.textContent.toLowerCase();
            const zona = (fila.dataset.zona || "").toLowerCase();
            const estado = (fila.dataset.estado || "").toLowerCase();
            const fecha = fila.dataset.fecha || "";

            const coincideTexto =
                texto === "" ||
                contenido.includes(texto) ||
                zona.includes(texto) ||
                estado.includes(texto);

            const coincideDesde =
                desde === "" || fecha >= desde;

            const coincideHasta =
                hasta === "" || fecha <= hasta;

            const mostrar =
                coincideTexto &&
                coincideDesde &&
                coincideHasta;

            fila.style.display = mostrar ? "" : "none";
        });
    }

    if (buscador) {
        buscador.addEventListener("input", filtrarSolicitudes);
    }

    if (fechaDesde) {
        fechaDesde.addEventListener("change", filtrarSolicitudes);
    }

    if (fechaHasta) {
        fechaHasta.addEventListener("change", filtrarSolicitudes);
    }

    if (limpiarBusqueda) {
        limpiarBusqueda.addEventListener("click", function () {
            if (buscador) {
                buscador.value = "";
                buscador.focus();
            }

            filtrarSolicitudes();
        });
    }

    if (limpiarFiltros) {
        limpiarFiltros.addEventListener("click", function () {
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

            if (buscador) {
                buscador.focus();
            }
        });
    }

    const alertas = document.querySelectorAll(".alert");

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