document.addEventListener("DOMContentLoaded", function () {

    // =========================================================
    // ELEMENTOS
    // =========================================================

    const formGasto =
        document.getElementById("formGasto");

    const ruc =
        document.getElementById("ruc");

    const btnRuc =
        document.getElementById("btnConsultarRuc");

    const razonSocial =
        document.getElementById("razonSocial");

    const domicilio =
        document.getElementById("domicilioFiscal");

    const mensajeRuc =
        document.getElementById("mensajeRuc");

    const tipoGasto =
        document.getElementById("tipoGasto");

    const mensajeTipoGasto =
        document.getElementById("mensajeTipoGasto");

    const tipoComprobante =
        document.getElementById("tipoComprobante");

    const mensajeComprobante =
        document.getElementById("mensajeComprobante");

    const mensajeMovilidad =
        document.getElementById("mensajeMovilidad");

    const archivo =
        document.getElementById("archivo");

    const mensajeArchivo =
        document.getElementById("mensajeArchivo");

    const serie =
        document.getElementById("serie");

    const numero =
        document.getElementById("numero");

    const fechaGasto =
        document.querySelector('input[name="Fecha"]');

    const montoTotal =
        document.getElementById("montoTotal");

    const exoneracion =
        document.getElementById("exoneracionIGV");

    const valorVenta =
        document.getElementById("valorVenta");

    const igv =
        document.getElementById("igvCalculado");

    const resumenBase =
        document.getElementById("resumenValorVenta");

    const resumenIgv =
        document.getElementById("resumenIgv");

    const resumenTotal =
        document.getElementById("resumenTotal");

    const mensajeLimiteMonto =
        document.getElementById("mensajeLimiteMonto");


    // =========================================================
    // ELEMENTOS HOSPEDAJE
    // =========================================================

    const periodoHospedaje =
        document.getElementById("periodoHospedaje");

    const fechaInicioHospedaje =
        document.getElementById("fechaInicioHospedaje");

    const fechaFinHospedaje =
        document.getElementById("fechaFinHospedaje");

    const diasHospedaje =
        document.getElementById("diasHospedaje");

    const maximoHospedaje =
        document.getElementById("maximoHospedaje");

    const mensajeHospedaje =
        document.getElementById("mensajeHospedaje");


    // =========================================================
    // DATOS DEL FORMULARIO
    // =========================================================

    const urlConsultarRuc =
        formGasto?.dataset.consultarRucUrl || "";

    const fechaInicioRendicion =
        formGasto?.dataset.fechaInicio || "";

    const fechaFinRendicion =
        formGasto?.dataset.fechaFin || "";


    // =========================================================
    // VARIABLES
    // =========================================================

    let ultimaConsulta = "";

    let diasHospedajeEditadosManualmente = false;


    // =========================================================
    // LÍMITES
    // =========================================================

    const LIMITE_ALIMENTACION = 40.00;

    const LIMITE_HOSPEDAJE = 50.00;

    const TAMANIO_MAXIMO_ARCHIVO =
        5 * 1024 * 1024;


    // =========================================================
    // NORMALIZAR TEXTO
    // =========================================================

    function normalizarTexto(texto) {

        return (texto || "")
            .toString()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .trim();

    }


    // =========================================================
    // OBTENER NOMBRE DEL TIPO DE GASTO
    // =========================================================

    function obtenerNombreTipo() {

        if (!tipoGasto) {
            return "";
        }

        const opcion =
            tipoGasto.options[
            tipoGasto.selectedIndex
            ];

        return opcion?.text || "";

    }


    // =========================================================
    // OBTENER NOMBRE DEL COMPROBANTE
    // =========================================================

    function obtenerNombreComprobante() {

        if (!tipoComprobante) {
            return "";
        }

        const opcion =
            tipoComprobante.options[
            tipoComprobante.selectedIndex
            ];

        return opcion?.text || "";

    }


    // =========================================================
    // ES MOVILIDAD
    // =========================================================

    function esMovilidad() {

        const tipo =
            normalizarTexto(
                obtenerNombreTipo()
            );

        return tipo.includes("movilidad");

    }


    // =========================================================
    // ES HOSPEDAJE
    // =========================================================

    function esHospedaje() {

        const tipo =
            normalizarTexto(
                obtenerNombreTipo()
            );

        return tipo.includes("hospedaje");

    }


    // =========================================================
    // ES FACTURA
    // =========================================================

    function esFactura() {

        const comprobante =
            normalizarTexto(
                obtenerNombreComprobante()
            );

        return comprobante.includes("factura");

    }


    // =========================================================
    // RUC OBLIGATORIO
    // =========================================================
    //
    // Solamente FACTURA exige RUC.
    //
    // Boleta                  -> RUC opcional
    // Ticket                  -> RUC opcional
    // Recibo por Honorarios   -> RUC opcional
    // Movilidad               -> RUC opcional
    //
    // IMPORTANTE:
    // Que el RUC sea opcional NO significa que la consulta
    // esté deshabilitada.
    //
    // Si el usuario escribe un RUC de 11 dígitos,
    // puede consultarlo para cualquier tipo de gasto.
    //
    // =========================================================

    function rucEsObligatorio() {

        if (esMovilidad()) {
            return false;
        }

        return esFactura();

    }


    // =========================================================
    // ACTUALIZAR REQUISITOS
    // =========================================================

    function actualizarRequisitos() {

        const movilidad =
            esMovilidad();

        const hospedaje =
            esHospedaje();


        // =====================================================
        // DOMICILIO FISCAL
        // =====================================================
        //
        // SIEMPRE EDITABLE.
        //
        // No importa si viene de SUNAT o si el usuario
        // lo escribe manualmente.
        //
        // =====================================================

        if (domicilio) {

            domicilio.removeAttribute("readonly");

        }


        // =====================================================
        // HOSPEDAJE
        // =====================================================

        if (periodoHospedaje) {

            periodoHospedaje.style.display =
                hospedaje
                    ? "block"
                    : "none";

        }


        // =====================================================
        // NO ES HOSPEDAJE
        // =====================================================

        if (!hospedaje) {

            if (fechaInicioHospedaje) {

                fechaInicioHospedaje.value = "";

                fechaInicioHospedaje
                    .removeAttribute("required");

            }

            if (fechaFinHospedaje) {

                fechaFinHospedaje.value = "";

                fechaFinHospedaje
                    .removeAttribute("required");

            }

            if (diasHospedaje) {

                diasHospedaje.value = "";

                diasHospedaje
                    .removeAttribute("required");

                diasHospedaje
                    .removeAttribute("min");

            }

            if (maximoHospedaje) {

                maximoHospedaje.value = "";

            }

            if (mensajeHospedaje) {

                mensajeHospedaje.textContent = "";

                mensajeHospedaje.className =
                    "small mt-3";

            }

            if (montoTotal) {

                montoTotal.removeAttribute("max");

            }

            diasHospedajeEditadosManualmente =
                false;

        }


        // =====================================================
        // ES HOSPEDAJE
        // =====================================================

        else {

            fechaInicioHospedaje?.setAttribute(
                "required",
                "required"
            );

            fechaFinHospedaje?.setAttribute(
                "required",
                "required"
            );

            diasHospedaje?.setAttribute(
                "required",
                "required"
            );

            diasHospedaje?.setAttribute(
                "min",
                "1"
            );

        }


        // =====================================================
        // MOVILIDAD
        // =====================================================

        if (movilidad) {

            // RUC OPCIONAL
            ruc?.removeAttribute("required");


            // RAZÓN SOCIAL OPCIONAL
            razonSocial?.removeAttribute("required");


            // DOMICILIO FISCAL OPCIONAL
            domicilio?.removeAttribute("required");


            // COMPROBANTE OPCIONAL
            tipoComprobante?.removeAttribute(
                "required"
            );


            // SERIE OPCIONAL
            serie?.removeAttribute("required");


            // NÚMERO OPCIONAL
            numero?.removeAttribute("required");


            // ARCHIVO OPCIONAL
            archivo?.removeAttribute("required");


            if (mensajeMovilidad) {

                mensajeMovilidad.style.display =
                    "block";

            }


            if (mensajeArchivo) {

                mensajeArchivo.innerHTML =
                    "PDF, JPG, JPEG o PNG. " +
                    "<strong>Opcional para Movilidad.</strong>";

            }


            if (mensajeTipoGasto) {

                mensajeTipoGasto.textContent =
                    "Movilidad: RUC, comprobante y voucher son opcionales. Si ingresa un RUC, puede consultarlo.";

                mensajeTipoGasto.className =
                    "form-text text-success";

            }


            ruc?.classList.remove(
                "is-invalid"
            );

            razonSocial?.classList.remove(
                "is-invalid"
            );

            domicilio?.classList.remove(
                "is-invalid"
            );

            tipoComprobante?.classList.remove(
                "is-invalid"
            );

            archivo?.classList.remove(
                "is-invalid"
            );

            return;

        }


        // =====================================================
        // NO ES MOVILIDAD
        // =====================================================

        if (mensajeMovilidad) {

            mensajeMovilidad.style.display =
                "none";

        }


        // =====================================================
        // TIPO DE COMPROBANTE
        // =====================================================

        tipoComprobante?.setAttribute(
            "required",
            "required"
        );


        // =====================================================
        // VOUCHER
        // =====================================================

        archivo?.setAttribute(
            "required",
            "required"
        );


        // =====================================================
        // RUC
        // =====================================================
        //
        // Solamente FACTURA lo exige.
        //
        // =====================================================

        if (rucEsObligatorio()) {

            ruc?.setAttribute(
                "required",
                "required"
            );

        }
        else {

            ruc?.removeAttribute(
                "required"
            );

        }


        // =====================================================
        // PROVEEDOR
        // =====================================================

        if (rucEsObligatorio()) {

            razonSocial?.setAttribute(
                "required",
                "required"
            );

            domicilio?.setAttribute(
                "required",
                "required"
            );

        }
        else {

            razonSocial?.removeAttribute(
                "required"
            );

            domicilio?.removeAttribute(
                "required"
            );

        }


        // =====================================================
        // SERIE Y NÚMERO
        // =====================================================

        serie?.removeAttribute("required");

        numero?.removeAttribute("required");


        // =====================================================
        // MENSAJE ARCHIVO
        // =====================================================

        if (mensajeArchivo) {

            mensajeArchivo.innerHTML =
                "PDF, JPG, JPEG o PNG. " +
                "<strong>Obligatorio excepto para Movilidad.</strong>";

        }


        // =====================================================
        // MENSAJE TIPO GASTO
        // =====================================================

        if (mensajeTipoGasto) {

            if (esFactura()) {

                mensajeTipoGasto.textContent =
                    "Factura: debe ingresar un RUC válido.";

                mensajeTipoGasto.className =
                    "form-text text-primary";

            }
            else if (
                tipoComprobante &&
                tipoComprobante.value
            ) {

                mensajeTipoGasto.textContent =
                    "RUC opcional para este tipo de comprobante. Si lo ingresa, puede consultarlo.";

                mensajeTipoGasto.className =
                    "form-text text-success";

            }
            else {

                mensajeTipoGasto.textContent = "";

                mensajeTipoGasto.className =
                    "form-text";

            }

        }

    }


    // =========================================================
    // CONSULTAR RUC
    // =========================================================
    //
    // IMPORTANTE:
    //
    // YA NO SE BLOQUEA LA CONSULTA PARA MOVILIDAD.
    //
    // La consulta funciona para TODOS los tipos de gasto.
    //
    // El RUC solamente debe tener 11 dígitos.
    //
    // =========================================================

    async function consultarRuc() {

        const numeroRuc =
            ruc?.value.trim() || "";


        // =====================================================
        // VALIDAR RUC
        // =====================================================

        if (!/^\d{11}$/.test(numeroRuc)) {

            if (mensajeRuc) {

                mensajeRuc.textContent =
                    "Ingrese un RUC válido de 11 dígitos.";

                mensajeRuc.className =
                    "form-text text-danger";

            }

            return;

        }


        // =====================================================
        // EVITAR CONSULTA DUPLICADA
        // =====================================================

        if (
            numeroRuc === ultimaConsulta &&
            numeroRuc !== ""
        ) {

            return;

        }


        // =====================================================
        // LIMPIAR DATOS ANTERIORES
        // =====================================================

        if (razonSocial) {

            razonSocial.value = "";

        }


        if (domicilio) {

            domicilio.value = "";

            // Siempre editable
            domicilio.removeAttribute(
                "readonly"
            );

        }


        if (mensajeRuc) {

            mensajeRuc.textContent = "";

        }


        // =====================================================
        // VALIDAR URL
        // =====================================================

        if (!urlConsultarRuc) {

            if (mensajeRuc) {

                mensajeRuc.textContent =
                    "No se pudo determinar la dirección de consulta del RUC.";

                mensajeRuc.className =
                    "form-text text-danger";

            }

            return;

        }


        // =====================================================
        // DESHABILITAR BOTÓN
        // =====================================================

        if (btnRuc) {

            btnRuc.disabled = true;

            btnRuc.innerHTML =
                '<span class="spinner-border spinner-border-sm me-1"></span>Buscando...';

        }


        // =====================================================
        // CONSULTAR
        // =====================================================

        try {

            const separador =
                urlConsultarRuc.includes("?")
                    ? "&"
                    : "?";


            const url =
                urlConsultarRuc +
                separador +
                "ruc=" +
                encodeURIComponent(
                    numeroRuc
                );


            const response =
                await fetch(
                    url,
                    {
                        method: "GET",
                        headers: {
                            "Accept":
                                "application/json"
                        }
                    }
                );


            // =================================================
            // RESPUESTA JSON
            // =================================================

            let data;


            try {

                data =
                    await response.json();

            }
            catch {

                throw new Error(
                    "La respuesta del servidor no es válida."
                );

            }


            // =================================================
            // ERROR
            // =================================================

            if (!response.ok) {

                throw new Error(
                    data?.mensaje ||
                    "No se encontró el RUC."
                );

            }


            // =================================================
            // RAZÓN SOCIAL
            // =================================================

            if (razonSocial) {

                razonSocial.value =
                    data?.razonSocial ?? "";

            }


            // =================================================
            // DOMICILIO FISCAL
            // =================================================
            //
            // IMPORTANTE:
            // Se carga automáticamente, pero queda editable.
            //
            // =================================================

            if (domicilio) {

                domicilio.value =
                    data?.domicilioFiscal ?? "";

                domicilio.removeAttribute(
                    "readonly"
                );

            }


            // =================================================
            // MENSAJE
            // =================================================

            if (mensajeRuc) {

                mensajeRuc.textContent =
                    `Estado: ${data?.estado ?? "-"} | Condición: ${data?.condicion ?? "-"}`;

                mensajeRuc.className =
                    "form-text text-success";

            }


            // =================================================
            // GUARDAR ÚLTIMA CONSULTA
            // =================================================

            ultimaConsulta =
                numeroRuc;


            // =================================================
            // VALIDAR COMPROBANTE
            // =================================================

            validarComprobantePorRuc();

        }
        catch (error) {

            ultimaConsulta = "";

            if (mensajeRuc) {

                mensajeRuc.textContent =
                    error?.message ||
                    "No se pudo consultar el RUC.";

                mensajeRuc.className =
                    "form-text text-danger";

            }

        }
        finally {

            if (btnRuc) {

                btnRuc.disabled = false;

                btnRuc.innerHTML =
                    '<i class="bi bi-search me-1"></i>Buscar';

            }

        }

    }


    // =========================================================
    // VALIDAR COMPROBANTE SEGÚN RUC
    // =========================================================

    function validarComprobantePorRuc() {

        // =====================================================
        // MOVILIDAD
        // =====================================================

        if (esMovilidad()) {

            if (mensajeComprobante) {

                if (
                    ruc &&
                    ruc.value.trim()
                ) {

                    mensajeComprobante.textContent =
                        "Para Movilidad el comprobante es opcional. El RUC ingresado puede utilizarse para consultar los datos del proveedor.";

                }
                else {

                    mensajeComprobante.textContent =
                        "Para Movilidad el comprobante y el RUC son opcionales.";

                }

                mensajeComprobante.className =
                    "form-text text-success";

            }

            tipoComprobante?.classList.remove(
                "is-invalid"
            );

            return true;

        }


        // =====================================================
        // VALIDACIÓN BÁSICA
        // =====================================================

        if (!ruc || !tipoComprobante) {

            return true;

        }


        const numeroRuc =
            ruc.value.trim();


        const opcionSeleccionada =
            tipoComprobante.options[
            tipoComprobante.selectedIndex
            ];


        const nombreComprobante =
            opcionSeleccionada?.text || "";


        const comprobante =
            normalizarTexto(
                nombreComprobante
            );


        // =====================================================
        // NO HAY COMPROBANTE
        // =====================================================

        if (!tipoComprobante.value) {

            if (mensajeComprobante) {

                mensajeComprobante.textContent =
                    "";

            }

            tipoComprobante.classList.remove(
                "is-invalid"
            );

            return true;

        }


        // =====================================================
        // FACTURA
        // =====================================================

        if (
            comprobante.includes(
                "factura"
            )
        ) {

            if (
                !/^\d{11}$/.test(
                    numeroRuc
                )
            ) {

                if (mensajeComprobante) {

                    mensajeComprobante.textContent =
                        "Para una FACTURA debe ingresar un RUC válido de 11 dígitos.";

                    mensajeComprobante.className =
                        "form-text text-danger";

                }

                tipoComprobante.classList.add(
                    "is-invalid"
                );

                return false;

            }


            if (
                numeroRuc.startsWith("20")
            ) {

                if (mensajeComprobante) {

                    mensajeComprobante.textContent =
                        "RUC iniciado en 20: comprobante válido.";

                    mensajeComprobante.className =
                        "form-text text-success";

                }

                tipoComprobante.classList.remove(
                    "is-invalid"
                );

                return true;

            }


            if (mensajeComprobante) {

                mensajeComprobante.textContent =
                    "Factura seleccionada con RUC válido.";

                mensajeComprobante.className =
                    "form-text text-success";

            }

            tipoComprobante.classList.remove(
                "is-invalid"
            );

            return true;

        }


        // =====================================================
        // BOLETA / TICKET / RECIBO POR HONORARIOS
        // =====================================================

        if (mensajeComprobante) {

            if (numeroRuc) {

                if (
                    !/^\d{11}$/.test(
                        numeroRuc
                    )
                ) {

                    mensajeComprobante.textContent =
                        "El RUC es opcional, pero si lo ingresa debe contener 11 dígitos.";

                    mensajeComprobante.className =
                        "form-text text-warning";

                    tipoComprobante.classList.add(
                        "is-invalid"
                    );

                    return false;

                }

                mensajeComprobante.textContent =
                    "RUC válido. El RUC es opcional para este comprobante.";

            }
            else {

                mensajeComprobante.textContent =
                    "El RUC es opcional para este tipo de comprobante.";

            }

            mensajeComprobante.className =
                "form-text text-success";

        }


        tipoComprobante.classList.remove(
            "is-invalid"
        );

        return true;

    }


    // =========================================================
    // OBTENER LÍMITE TIPO GASTO
    // =========================================================

    function obtenerLimiteTipo() {

        if (!tipoGasto) {
            return null;
        }


        const texto =
            tipoGasto.options[
                tipoGasto.selectedIndex
            ]?.text || "";


        const tipo =
            normalizarTexto(texto);


        if (
            tipo.includes(
                "alimentacion"
            )
        ) {

            return LIMITE_ALIMENTACION;

        }


        return null;

    }


    // =========================================================
    // OBTENER TOTAL EXISTENTE POR DÍA
    // =========================================================

    function obtenerTotalExistentePorDia(
        fecha,
        tipo
    ) {

        if (!fecha || !tipo) {
            return 0;
        }


        const tipoBuscado =
            normalizarTexto(tipo);


        let totalExistente = 0;


        const filas =
            document.querySelectorAll(
                ".table-gastos tbody tr"
            );


        filas.forEach(
            function (fila) {

                const celdas =
                    fila.querySelectorAll("td");


                if (celdas.length < 7) {
                    return;
                }


                const fechaTexto =
                    celdas[0]
                        ?.textContent
                        ?.trim()
                        .split("\n")[0]
                        .trim() || "";


                const partes =
                    fechaTexto.split("/");


                if (partes.length !== 3) {
                    return;
                }


                const fechaFila =
                    `${partes[2]}-${partes[1].padStart(2, "0")}-${partes[0].padStart(2, "0")}`;


                if (fechaFila !== fecha) {
                    return;
                }


                const tipoFila =
                    normalizarTexto(
                        celdas[1]?.textContent || ""
                    );


                if (
                    !tipoFila.includes(
                        tipoBuscado
                    )
                ) {

                    return;

                }


                const totalTexto =
                    (
                        celdas[6]
                            ?.textContent || ""
                    )
                        .replace("S/", "")
                        .replace(/,/g, "")
                        .trim();


                const total =
                    parseFloat(
                        totalTexto
                    ) || 0;


                totalExistente += total;

            }
        );


        return totalExistente;

    }


    // =========================================================
    // FORMATEAR FECHA
    // =========================================================

    function formatearFecha(fecha) {

        if (!fecha) {
            return "";
        }


        const partes =
            fecha.split("-");


        if (partes.length !== 3) {
            return fecha;
        }


        return `${partes[2]}/${partes[1]}/${partes[0]}`;

    }


    // =========================================================
    // VALIDAR LÍMITE DIARIO
    // =========================================================

    function validarLimiteDiario(
        mostrarMensaje = true
    ) {

        if (
            !fechaGasto ||
            !tipoGasto ||
            !montoTotal
        ) {

            return true;

        }


        // =====================================================
        // HOSPEDAJE
        // =====================================================

        if (esHospedaje()) {

            if (mensajeLimiteMonto) {

                mensajeLimiteMonto.textContent =
                    "";

                mensajeLimiteMonto.className =
                    "form-text";

            }

            montoTotal.removeAttribute(
                "max"
            );

            return true;

        }


        // =====================================================
        // MOVILIDAD
        // =====================================================

        if (esMovilidad()) {

            if (mensajeLimiteMonto) {

                mensajeLimiteMonto.textContent =
                    "";

                mensajeLimiteMonto.className =
                    "form-text";

            }

            montoTotal.removeAttribute(
                "max"
            );

            return true;

        }


        // =====================================================
        // ALIMENTACIÓN
        // =====================================================

        const fecha =
            fechaGasto.value;


        const tipo =
            obtenerNombreTipo();


        const limite =
            obtenerLimiteTipo();


        const monto =
            parseFloat(
                montoTotal.value
            ) || 0;


        if (limite === null) {

            if (mensajeLimiteMonto) {

                mensajeLimiteMonto.textContent =
                    "";

                mensajeLimiteMonto.className =
                    "form-text";

            }

            montoTotal.removeAttribute(
                "max"
            );

            return true;

        }


        if (!fecha) {

            if (mostrarMensaje) {

                mensajeLimiteMonto.textContent =
                    "Seleccione la fecha del gasto.";

                mensajeLimiteMonto.className =
                    "form-text text-danger";

            }

            return false;

        }


        const totalExistente =
            obtenerTotalExistentePorDia(
                fecha,
                tipo
            );


        const disponible =
            Math.max(
                0,
                limite -
                totalExistente
            );


        montoTotal.max =
            disponible.toFixed(2);


        if (
            totalExistente >=
            limite
        ) {

            if (mostrarMensaje) {

                mensajeLimiteMonto.textContent =
                    `${tipo}: ya alcanzó el límite diario de S/ ${limite.toFixed(2)}.`;

                mensajeLimiteMonto.className =
                    "form-text text-danger";

            }

            return false;

        }


        if (monto > disponible) {

            if (mostrarMensaje) {

                mensajeLimiteMonto.textContent =
                    `El máximo permitido para ${tipo.toLowerCase()} en esta fecha es S/ ${disponible.toFixed(2)}. ` +
                    `El límite diario es S/ ${limite.toFixed(2)}.`;

                mensajeLimiteMonto.className =
                    "form-text text-danger";

            }

            return false;

        }


        if (mostrarMensaje) {

            mensajeLimiteMonto.textContent =
                `Límite diario: S/ ${limite.toFixed(2)} | ` +
                `Registrado: S/ ${totalExistente.toFixed(2)} | ` +
                `Disponible: S/ ${disponible.toFixed(2)}`;

            mensajeLimiteMonto.className =
                "form-text text-muted";

        }


        return true;

    }


    // =========================================================
    // OBTENER DÍAS HOSPEDAJE
    // =========================================================

    function obtenerDiasHospedaje() {

        if (
            !fechaInicioHospedaje?.value ||
            !fechaFinHospedaje?.value
        ) {

            return 0;

        }


        const inicio =
            new Date(
                fechaInicioHospedaje.value +
                "T00:00:00"
            );


        const fin =
            new Date(
                fechaFinHospedaje.value +
                "T00:00:00"
            );


        if (
            Number.isNaN(
                inicio.getTime()
            ) ||
            Number.isNaN(
                fin.getTime()
            )
        ) {

            return 0;

        }


        if (fin < inicio) {
            return 0;
        }


        return Math.floor(
            (
                fin - inicio
            ) /
            (
                1000 *
                60 *
                60 *
                24
            )
        ) + 1;

    }


    // =========================================================
    // ACTUALIZAR DÍAS
    // =========================================================

    function actualizarDiasDesdeFechas() {

        if (!diasHospedaje) {
            return;
        }


        if (
            diasHospedajeEditadosManualmente
        ) {

            return;

        }


        const dias =
            obtenerDiasHospedaje();


        if (dias > 0) {

            diasHospedaje.value =
                dias.toString();

        }

    }


    // =========================================================
    // DETECTAR CRUCE HOSPEDAJE
    // =========================================================

    function existeCruceHospedaje(
        inicioNuevo,
        finNuevo
    ) {

        if (
            !inicioNuevo ||
            !finNuevo
        ) {

            return false;

        }


        const nuevoInicio =
            new Date(
                inicioNuevo +
                "T00:00:00"
            );


        const nuevoFin =
            new Date(
                finNuevo +
                "T00:00:00"
            );


        if (
            Number.isNaN(
                nuevoInicio.getTime()
            ) ||
            Number.isNaN(
                nuevoFin.getTime()
            )
        ) {

            return false;

        }


        const filas =
            document.querySelectorAll(
                ".table-gastos tbody tr"
            );


        for (
            const fila of filas
        ) {

            const inicioExistente =
                fila.dataset
                    .inicioHospedaje || "";


            const finExistente =
                fila.dataset
                    .finHospedaje || "";


            if (
                !inicioExistente ||
                !finExistente
            ) {

                continue;

            }


            const inicio =
                new Date(
                    inicioExistente +
                    "T00:00:00"
                );


            const fin =
                new Date(
                    finExistente +
                    "T00:00:00"
                );


            if (
                nuevoInicio <= fin &&
                nuevoFin >= inicio
            ) {

                return true;

            }

        }


        return false;

    }


    // =========================================================
    // CALCULAR HOSPEDAJE
    // =========================================================

    function calcularHospedaje(
        mostrarMensaje = true
    ) {

        if (!esHospedaje()) {
            return true;
        }


        if (
            !fechaInicioHospedaje ||
            !fechaFinHospedaje ||
            !diasHospedaje ||
            !montoTotal
        ) {

            return false;

        }


        const inicio =
            fechaInicioHospedaje.value;


        const fin =
            fechaFinHospedaje.value;


        // =====================================================
        // FECHAS OBLIGATORIAS
        // =====================================================

        if (!inicio || !fin) {

            if (maximoHospedaje) {

                maximoHospedaje.value =
                    "";

            }

            montoTotal.removeAttribute(
                "max"
            );


            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    "Seleccione la fecha de inicio y la fecha de fin del hospedaje.";

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // FECHAS VÁLIDAS
        // =====================================================

        const fechaInicio =
            new Date(
                inicio +
                "T00:00:00"
            );


        const fechaFin =
            new Date(
                fin +
                "T00:00:00"
            );


        if (
            Number.isNaN(
                fechaInicio.getTime()
            ) ||
            Number.isNaN(
                fechaFin.getTime()
            )
        ) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    "Las fechas de hospedaje no son válidas.";

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // FECHA FIN ANTERIOR
        // =====================================================

        if (fin < inicio) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    "La fecha de fin no puede ser anterior a la fecha de inicio.";

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // PERÍODO DE RENDICIÓN
        // =====================================================

        if (
            fechaInicioRendicion &&
            inicio < fechaInicioRendicion
        ) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    `La fecha de inicio del hospedaje no puede ser anterior al inicio de la rendición (${formatearFecha(fechaInicioRendicion)}).`;

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        if (
            fechaFinRendicion &&
            fin > fechaFinRendicion
        ) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    `La fecha de fin del hospedaje no puede ser posterior al fin de la rendición (${formatearFecha(fechaFinRendicion)}).`;

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // DÍAS
        // =====================================================

        let dias =
            parseInt(
                diasHospedaje.value,
                10
            );


        if (
            !diasHospedajeEditadosManualmente &&
            (
                !Number.isFinite(dias) ||
                dias < 1
            )
        ) {

            dias =
                obtenerDiasHospedaje();


            if (dias > 0) {

                diasHospedaje.value =
                    dias.toString();

            }

        }


        // =====================================================
        // VALIDAR DÍAS
        // =====================================================

        if (
            !Number.isFinite(dias) ||
            dias < 1
        ) {

            if (maximoHospedaje) {

                maximoHospedaje.value =
                    "";

            }

            montoTotal.removeAttribute(
                "max"
            );


            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    "Debe ingresar al menos 1 día de hospedaje.";

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // MÁXIMO
        // =====================================================

        const maximo =
            dias *
            LIMITE_HOSPEDAJE;


        if (maximoHospedaje) {

            maximoHospedaje.value =
                `S/ ${maximo.toFixed(2)}`;

        }


        montoTotal.max =
            maximo.toFixed(2);


        // =====================================================
        // CRUCE
        // =====================================================

        if (
            existeCruceHospedaje(
                inicio,
                fin
            )
        ) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    "El período seleccionado se cruza con otro hospedaje ya registrado en esta rendición.";

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // MONTO
        // =====================================================

        const monto =
            parseFloat(
                montoTotal.value
            ) || 0;


        if (monto > maximo) {

            if (
                mostrarMensaje &&
                mensajeHospedaje
            ) {

                mensajeHospedaje.textContent =
                    `El monto ingresado es S/ ${monto.toFixed(2)}. ` +
                    `Para ${dias} día(s) de hospedaje, ` +
                    `el máximo permitido es S/ ${maximo.toFixed(2)} ` +
                    `(S/ ${LIMITE_HOSPEDAJE.toFixed(2)} × ${dias}).`;

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";

            }

            return false;

        }


        // =====================================================
        // CORRECTO
        // =====================================================

        if (
            mostrarMensaje &&
            mensajeHospedaje
        ) {

            mensajeHospedaje.textContent =
                `${dias} día(s) de hospedaje × S/ ${LIMITE_HOSPEDAJE.toFixed(2)} = máximo S/ ${maximo.toFixed(2)}.`;

            mensajeHospedaje.className =
                "small mt-3 hospedaje-valido";

        }


        return true;

    }


    // =========================================================
    // CALCULAR IGV
    // =========================================================

    function calcularIgv() {

        if (
            !montoTotal ||
            !valorVenta ||
            !igv
        ) {

            return;

        }


        const total =
            parseFloat(
                montoTotal.value
            ) || 0;


        let base = 0;

        let impuesto = 0;


        if (total > 0) {

            if (exoneracion?.checked) {

                base = total;

            }
            else {

                base =
                    Math.round(
                        (
                            total /
                            1.18
                        ) *
                        100
                    ) / 100;


                impuesto =
                    Math.round(
                        (
                            total -
                            base
                        ) *
                        100
                    ) / 100;

            }

        }


        valorVenta.value =
            base.toFixed(2);


        igv.value =
            impuesto.toFixed(2);


        if (resumenBase) {

            resumenBase.textContent =
                `S/ ${base.toFixed(2)}`;

        }


        if (resumenIgv) {

            resumenIgv.textContent =
                `S/ ${impuesto.toFixed(2)}`;

        }


        if (resumenTotal) {

            resumenTotal.textContent =
                `S/ ${total.toFixed(2)}`;

        }

    }


    // =========================================================
    // EVENTOS RUC
    // =========================================================

    if (ruc && btnRuc) {

        // =====================================================
        // BOTÓN BUSCAR
        // =====================================================

        btnRuc.addEventListener(
            "click",
            function (event) {

                event.preventDefault();

                consultarRuc();

            }
        );


        // =====================================================
        // BLUR DEL RUC
        // =====================================================
        //
        // FUNCIONA PARA TODOS LOS TIPOS DE GASTO,
        // INCLUYENDO MOVILIDAD.
        //
        // =====================================================

        ruc.addEventListener(
            "blur",
            function () {

                if (
                    /^\d{11}$/.test(
                        ruc.value.trim()
                    )
                ) {

                    consultarRuc();

                }

            }
        );


        // =====================================================
        // CAMBIO DEL RUC
        // =====================================================

        ruc.addEventListener(
            "input",
            function () {

                ruc.value =
                    ruc.value
                        .replace(/\D/g, "")
                        .slice(0, 11);


                if (
                    ruc.value !==
                    ultimaConsulta
                ) {

                    ultimaConsulta = "";


                    if (razonSocial) {

                        razonSocial.value =
                            "";

                    }


                    if (domicilio) {

                        domicilio.value =
                            "";

                        // Siempre editable
                        domicilio.removeAttribute(
                            "readonly"
                        );

                    }


                    if (mensajeRuc) {

                        mensajeRuc.textContent =
                            "";

                    }


                    if (mensajeComprobante) {

                        mensajeComprobante.textContent =
                            "";

                    }


                    tipoComprobante?.classList.remove(
                        "is-invalid"
                    );

                }

            }
        );

    }


    // =========================================================
    // CAMBIO TIPO COMPROBANTE
    // =========================================================

    tipoComprobante?.addEventListener(
        "change",
        function () {

            actualizarRequisitos();

            validarComprobantePorRuc();

        }
    );


    // =========================================================
    // CAMBIO TIPO GASTO
    // =========================================================

    tipoGasto?.addEventListener(
        "change",
        function () {

            diasHospedajeEditadosManualmente =
                false;


            actualizarRequisitos();

            validarComprobantePorRuc();


            if (esHospedaje()) {

                actualizarDiasDesdeFechas();

                calcularHospedaje(true);

            }
            else {

                validarLimiteDiario(true);

            }


            calcularIgv();

        }
    );


    // =========================================================
    // CAMBIO FECHA GASTO
    // =========================================================

    fechaGasto?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) {

                validarLimiteDiario(true);

            }

            calcularIgv();

        }
    );


    // =========================================================
    // CAMBIO INICIO HOSPEDAJE
    // =========================================================

    fechaInicioHospedaje?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) {
                return;
            }


            actualizarDiasDesdeFechas();

            calcularHospedaje(true);

        }
    );


    // =========================================================
    // CAMBIO FIN HOSPEDAJE
    // =========================================================

    fechaFinHospedaje?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) {
                return;
            }


            actualizarDiasDesdeFechas();

            calcularHospedaje(true);

        }
    );


    // =========================================================
    // CAMBIO MANUAL DÍAS HOSPEDAJE
    // =========================================================

    diasHospedaje?.addEventListener(
        "input",
        function () {

            diasHospedajeEditadosManualmente =
                true;


            this.value =
                this.value
                    .replace(/\D/g, "");


            calcularHospedaje(true);

        }
    );


    diasHospedaje?.addEventListener(
        "blur",
        function () {

            let dias =
                parseInt(
                    this.value || "0",
                    10
                );


            if (
                !Number.isFinite(dias) ||
                dias < 1
            ) {

                this.value = "";

            }
            else {

                this.value =
                    dias.toString();

            }


            calcularHospedaje(true);

        }
    );


    // =========================================================
    // CAMBIO MONTO
    // =========================================================

    montoTotal?.addEventListener(
        "input",
        function () {

            if (esHospedaje()) {

                calcularHospedaje(true);

            }
            else {

                validarLimiteDiario(true);

            }


            calcularIgv();

        }
    );


    montoTotal?.addEventListener(
        "blur",
        function () {

            if (esHospedaje()) {

                calcularHospedaje(true);

            }
            else {

                validarLimiteDiario(true);

            }


            calcularIgv();

        }
    );


    // =========================================================
    // CAMBIO EXONERACIÓN
    // =========================================================

    exoneracion?.addEventListener(
        "change",
        calcularIgv
    );


    // =========================================================
    // VALIDAR ARCHIVO
    // =========================================================

    archivo?.addEventListener(
        "change",
        function () {

            if (
                !this.files ||
                this.files.length === 0
            ) {

                return;

            }


            const file =
                this.files[0];


            const nombre =
                file.name.toLowerCase();


            const extensionesPermitidas = [
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            ];


            const extensionValida =
                extensionesPermitidas.some(
                    function (extension) {

                        return nombre.endsWith(
                            extension
                        );

                    }
                );


            if (!extensionValida) {

                this.value = "";

                if (mensajeArchivo) {

                    mensajeArchivo.textContent =
                        "Solo se permiten archivos PDF, JPG, JPEG o PNG.";

                    mensajeArchivo.className =
                        "form-text text-danger";

                }

                return;

            }


            if (
                file.size >
                TAMANIO_MAXIMO_ARCHIVO
            ) {

                this.value = "";

                if (mensajeArchivo) {

                    mensajeArchivo.textContent =
                        "El archivo no puede superar los 5 MB.";

                    mensajeArchivo.className =
                        "form-text text-danger";

                }

                return;

            }


            if (mensajeArchivo) {

                mensajeArchivo.textContent =
                    "Archivo válido.";

                mensajeArchivo.className =
                    "form-text text-success";

            }

        }
    );


    // =========================================================
    // VALIDACIÓN FINAL DEL FORMULARIO
    // =========================================================

    formGasto?.addEventListener(
        "submit",
        function (event) {

            const movilidad =
                esMovilidad();

            const hospedaje =
                esHospedaje();


            // =====================================================
            // MOVILIDAD
            // =====================================================
            //
            // NO se exige:
            // - RUC
            // - Razón social
            // - Domicilio
            // - Comprobante
            // - Serie
            // - Número
            // - Voucher
            //
            // PERO:
            // Si el usuario ingresó RUC, debe ser válido.
            // =====================================================

            if (movilidad) {

                const numeroRuc =
                    ruc?.value.trim() || "";


                if (
                    numeroRuc &&
                    !/^\d{11}$/.test(
                        numeroRuc
                    )
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "RUC inválido",
                        "El RUC es opcional para Movilidad, pero si lo ingresa debe contener exactamente 11 dígitos.",
                        "warning"
                    );


                    ruc?.focus();

                    return;

                }


                // No se valida comprobante.
                // No se valida voucher.
                // No se valida domicilio.
                // No se valida razón social.


                return;

            }


            // =====================================================
            // HOSPEDAJE
            // =====================================================

            if (hospedaje) {

                // -------------------------------------------------
                // VALIDAR HOSPEDAJE
                // -------------------------------------------------

                if (
                    !calcularHospedaje(true)
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Período de hospedaje inválido",
                        mensajeHospedaje?.textContent ||
                        "Revise las fechas, los días y el monto del hospedaje.",
                        "warning"
                    );


                    if (
                        !fechaInicioHospedaje?.value
                    ) {

                        fechaInicioHospedaje?.focus();

                    }
                    else if (
                        !fechaFinHospedaje?.value
                    ) {

                        fechaFinHospedaje?.focus();

                    }
                    else if (
                        !diasHospedaje?.value ||
                        parseInt(
                            diasHospedaje.value,
                            10
                        ) < 1
                    ) {

                        diasHospedaje?.focus();

                    }
                    else {

                        montoTotal?.focus();

                    }


                    return;

                }


                // -------------------------------------------------
                // COMPROBANTE
                // -------------------------------------------------

                if (
                    !tipoComprobante ||
                    !tipoComprobante.value
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Comprobante obligatorio",
                        "Debe seleccionar el tipo de comprobante para el hospedaje.",
                        "warning"
                    );


                    tipoComprobante?.focus();

                    return;

                }


                // -------------------------------------------------
                // VALIDAR RUC SOLO SI ES FACTURA
                // -------------------------------------------------

                if (esFactura()) {

                    const numeroRuc =
                        ruc?.value.trim() || "";


                    if (
                        !/^\d{11}$/.test(
                            numeroRuc
                        )
                    ) {

                        event.preventDefault();


                        mostrarAlerta(
                            "RUC obligatorio",
                            "Para una FACTURA debe ingresar un RUC válido de 11 dígitos.",
                            "error"
                        );


                        ruc?.focus();

                        return;

                    }

                }


                // -------------------------------------------------
                // SI HAY RUC, DEBE SER VÁLIDO
                // -------------------------------------------------

                const numeroRuc =
                    ruc?.value.trim() || "";


                if (
                    numeroRuc &&
                    !/^\d{11}$/.test(
                        numeroRuc
                    )
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "RUC inválido",
                        "Si ingresa un RUC, debe contener exactamente 11 dígitos.",
                        "warning"
                    );


                    ruc?.focus();

                    return;

                }


                // -------------------------------------------------
                // VALIDAR COMPROBANTE
                // -------------------------------------------------

                if (
                    !validarComprobantePorRuc()
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Comprobante no válido",
                        mensajeComprobante?.textContent ||
                        "Revise el tipo de comprobante y el RUC.",
                        "warning"
                    );


                    tipoComprobante?.focus();

                    return;

                }


                // -------------------------------------------------
                // ARCHIVO
                // -------------------------------------------------

                if (
                    !archivo ||
                    !archivo.files ||
                    archivo.files.length === 0
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Voucher obligatorio",
                        "Debe adjuntar el comprobante o voucher del hospedaje.",
                        "warning"
                    );


                    archivo?.focus();

                    return;

                }

            }


            // =====================================================
            // OTROS TIPOS DE GASTO
            // =====================================================

            else {

                // -------------------------------------------------
                // TIPO COMPROBANTE
                // -------------------------------------------------

                if (
                    !tipoComprobante ||
                    !tipoComprobante.value
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Comprobante obligatorio",
                        "Debe seleccionar el tipo de comprobante para este tipo de gasto.",
                        "warning"
                    );


                    tipoComprobante?.focus();

                    return;

                }


                // -------------------------------------------------
                // RUC
                // -------------------------------------------------

                const numeroRuc =
                    ruc?.value.trim() || "";


                // -------------------------------------------------
                // FACTURA
                // -------------------------------------------------

                if (esFactura()) {

                    if (
                        !/^\d{11}$/.test(
                            numeroRuc
                        )
                    ) {

                        event.preventDefault();


                        mostrarAlerta(
                            "RUC obligatorio",
                            "Para una FACTURA debe ingresar un RUC válido de 11 dígitos.",
                            "error"
                        );


                        ruc?.focus();

                        return;

                    }

                }


                // -------------------------------------------------
                // BOLETA / TICKET / RECIBO
                // -------------------------------------------------

                else {

                    if (
                        numeroRuc &&
                        !/^\d{11}$/.test(
                            numeroRuc
                        )
                    ) {

                        event.preventDefault();


                        mostrarAlerta(
                            "RUC inválido",
                            "El RUC es opcional para este comprobante, pero si lo ingresa debe contener 11 dígitos.",
                            "warning"
                        );


                        ruc?.focus();

                        return;

                    }

                }


                // -------------------------------------------------
                // VALIDAR COMPROBANTE
                // -------------------------------------------------

                if (
                    !validarComprobantePorRuc()
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Comprobante no válido",
                        mensajeComprobante?.textContent ||
                        "Revise el tipo de comprobante y el RUC.",
                        "warning"
                    );


                    tipoComprobante?.focus();

                    return;

                }


                // -------------------------------------------------
                // ARCHIVO
                // -------------------------------------------------

                if (
                    !archivo ||
                    !archivo.files ||
                    archivo.files.length === 0
                ) {

                    event.preventDefault();


                    mostrarAlerta(
                        "Voucher obligatorio",
                        "Debe adjuntar el comprobante o voucher del gasto.",
                        "warning"
                    );


                    archivo?.focus();

                    return;

                }

            }


            // =====================================================
            // LÍMITE ALIMENTACIÓN
            // =====================================================

            if (
                !movilidad &&
                !hospedaje
            ) {

                if (
                    !validarLimiteDiario(true)
                ) {

                    event.preventDefault();


                    const fecha =
                        fechaGasto?.value || "";


                    const tipo =
                        obtenerNombreTipo();


                    const limite =
                        obtenerLimiteTipo();


                    const monto =
                        parseFloat(
                            montoTotal?.value
                        ) || 0;


                    const existente =
                        obtenerTotalExistentePorDia(
                            fecha,
                            tipo
                        );


                    const disponible =
                        limite !== null
                            ? Math.max(
                                0,
                                limite -
                                existente
                            )
                            : 0;


                    if (
                        limite !== null &&
                        existente >= limite
                    ) {

                        mostrarAlerta(
                            "Límite diario alcanzado",
                            `Ya registró S/ ${existente.toFixed(2)} en ${tipo.toLowerCase()} para el día seleccionado.<br><br>` +
                            `<strong>Límite permitido: S/ ${limite.toFixed(2)}</strong><br>` +
                            `No puede registrar otro gasto de este tipo en esta fecha.`,
                            "warning"
                        );

                    }
                    else if (
                        limite !== null &&
                        monto > disponible
                    ) {

                        mostrarAlerta(
                            "Monto excede el límite",
                            `El gasto ingresado es de <strong>S/ ${monto.toFixed(2)}</strong>.<br><br>` +
                            `Ya tiene registrado: <strong>S/ ${existente.toFixed(2)}</strong>.<br>` +
                            `Disponible: <strong>S/ ${disponible.toFixed(2)}</strong>.<br><br>` +
                            `Límite diario de ${tipo.toLowerCase()}: <strong>S/ ${limite.toFixed(2)}</strong>.`,
                            "warning"
                        );

                    }
                    else {

                        mostrarAlerta(
                            "No se puede registrar",
                            "El gasto no cumple con las restricciones establecidas.",
                            "warning"
                        );

                    }


                    montoTotal?.focus();

                    return;

                }

            }

        }
    );


    // =========================================================
    // ALERTAS
    // =========================================================

    function mostrarAlerta(
        titulo,
        mensaje,
        tipo
    ) {

        if (
            typeof Swal !==
            "undefined"
        ) {

            Swal.fire({

                icon: tipo,

                title: titulo,

                html: mensaje,

                confirmButtonText:
                    "Entendido",

                confirmButtonColor:
                    "#0C4A8A"

            });

            return;

        }


        const mensajePlano =
            String(mensaje || "")
                .replace(
                    /<[^>]*>/g,
                    ""
                );


        alert(
            titulo +
            "\n\n" +
            mensajePlano
        );

    }


    // =========================================================
    // INICIALIZACIÓN
    // =========================================================

    // Asegurar que el domicilio fiscal siempre sea editable
    if (domicilio) {

        domicilio.removeAttribute(
            "readonly"
        );

    }


    actualizarRequisitos();

    calcularIgv();


    if (esHospedaje()) {

        if (
            diasHospedaje &&
            diasHospedaje.value &&
            parseInt(
                diasHospedaje.value,
                10
            ) >= 1
        ) {

            diasHospedajeEditadosManualmente =
                false;

        }
        else {

            actualizarDiasDesdeFechas();

        }


        calcularHospedaje(false);

    }
    else {

        validarLimiteDiario(false);

    }


    // =========================================================
    // ACTUALIZAR MENSAJE INICIAL DEL COMPROBANTE
    // =========================================================

    if (tipoComprobante?.value) {

        validarComprobantePorRuc();

    }

});