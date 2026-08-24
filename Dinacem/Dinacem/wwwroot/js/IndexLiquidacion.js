document.addEventListener("DOMContentLoaded", function () {

    const buscador =
        document.getElementById("buscarLiquidacion");

    const fechaDesde =
        document.getElementById("fechaDesdeRendicion");

    const fechaHasta =
        document.getElementById("fechaHastaRendicion");

    const limpiarFiltros =
        document.getElementById("limpiarFiltrosRendicion");

    const botonesFiltro =
        document.querySelectorAll(".filtro-btn");

    const tabla =
        document.getElementById("tablaLiquidaciones");

    const cantidadVisible =
        document.getElementById("cantidadVisible");

    const filaSinResultados =
        document.getElementById("filaSinResultados");


    if (!tabla) {

        console.warn(
            "No se encontró #tablaLiquidaciones"
        );

        return;
    }


    let estadoSeleccionado = "todos";


    function normalizarTexto(texto) {

        return (texto || "")
            .toString()
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .trim();
    }


    function aplicarFiltros() {

        const termino =
            normalizarTexto(
                buscador
                    ? buscador.value
                    : ""
            );


        const desde =
            fechaDesde
                ? fechaDesde.value
                : "";


        const hasta =
            fechaHasta
                ? fechaHasta.value
                : "";


        const filas =
            tabla.querySelectorAll(
                "tr[data-estado]"
            );


        let visibles = 0;


        filas.forEach(function (fila) {

            const estado =
                fila.dataset.estado || "";


            const texto =
                normalizarTexto(
                    fila.dataset.busqueda || ""
                );


            const fecha =
                fila.dataset.fecha || "";


            const coincideEstado =
                estadoSeleccionado === "todos" ||
                estado === estadoSeleccionado;


            const coincideBusqueda =
                termino === "" ||
                texto.includes(termino);


            let coincideDesde = true;

            if (desde !== "") {

                coincideDesde =
                    fecha !== "" &&
                    fecha >= desde;

            }


            let coincideHasta = true;

            if (hasta !== "") {

                coincideHasta =
                    fecha !== "" &&
                    fecha <= hasta;

            }


            const mostrar =
                coincideEstado &&
                coincideBusqueda &&
                coincideDesde &&
                coincideHasta;


            fila.style.display =
                mostrar
                    ? ""
                    : "none";


            if (mostrar) {

                visibles++;

            }

        });


        if (cantidadVisible) {

            cantidadVisible.textContent =
                visibles.toString();

        }


        if (filaSinResultados) {

            filaSinResultados.style.display =
                filas.length > 0 && visibles === 0
                    ? ""
                    : "none";

        }

    }


    botonesFiltro.forEach(function (boton) {

        boton.addEventListener(
            "click",
            function () {

                botonesFiltro.forEach(
                    function (item) {

                        item.classList.remove(
                            "active"
                        );

                    }
                );


                boton.classList.add("active");


                estadoSeleccionado =
                    boton.dataset.estado ||
                    "todos";


                aplicarFiltros();

            }
        );

    });


    if (buscador) {

        buscador.addEventListener(
            "input",
            function () {

                aplicarFiltros();

            }
        );


        buscador.addEventListener(
            "search",
            function () {

                aplicarFiltros();

            }
        );


        buscador.addEventListener(
            "keydown",
            function (evento) {

                if (evento.key === "Escape") {

                    buscador.value = "";

                    aplicarFiltros();

                    buscador.focus();

                }

            }
        );

    }


    if (fechaDesde) {

        fechaDesde.addEventListener(
            "change",
            function () {

                aplicarFiltros();

            }
        );

    }


    if (fechaHasta) {

        fechaHasta.addEventListener(
            "change",
            function () {

                aplicarFiltros();

            }
        );

    }


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


                botonesFiltro.forEach(
                    function (boton) {

                        boton.classList.remove(
                            "active"
                        );

                    }
                );


                const botonTodos =
                    document.querySelector(
                        '.filtro-btn[data-estado="todos"]'
                    );


                if (botonTodos) {

                    botonTodos.classList.add(
                        "active"
                    );

                }


                aplicarFiltros();

            }
        );

    }


    aplicarFiltros();

});