document.addEventListener("DOMContentLoaded", function () {
    const formGasto = document.getElementById("formGasto");
    if (!formGasto) return;

    const ruc = document.getElementById("ruc");
    const btnRuc = document.getElementById("btnConsultarRuc");
    const razonSocial = document.getElementById("razonSocial");
    const domicilio = document.getElementById("domicilioFiscal");
    const mensajeRuc = document.getElementById("mensajeRuc");
    const tipoGasto = document.getElementById("tipoGasto");
    const mensajeTipoGasto = document.getElementById("mensajeTipoGasto");
    const tipoComprobante = document.getElementById("tipoComprobante");
    const seccionDatosComprobante =
        document.getElementById("seccionDatosComprobante");
    const mensajeComprobante = document.getElementById("mensajeComprobante");
    const mensajeMovilidad = document.getElementById("mensajeMovilidad");
    const archivo = document.getElementById("archivo");
    const mensajeArchivo = document.getElementById("mensajeArchivo");
    const serie = document.getElementById("serie");
    const numero = document.getElementById("numero");
    const fechaGasto = document.querySelector('input[name="Fecha"]');
    const montoTotal = document.getElementById("montoTotal");
    const exoneracion = document.getElementById("exoneracionIGV");
    const valorVenta = document.getElementById("valorVenta");
    const igv = document.getElementById("igvCalculado");
    const resumenBase = document.getElementById("resumenValorVenta");
    const resumenIgv = document.getElementById("resumenIgv");
    const resumenTotal = document.getElementById("resumenTotal");
    const mensajeLimiteMonto = document.getElementById("mensajeLimiteMonto");

    const periodoHospedaje = document.getElementById("periodoHospedaje");
    const fechaInicioHospedaje = document.getElementById("fechaInicioHospedaje");
    const fechaFinHospedaje = document.getElementById("fechaFinHospedaje");
    const diasHospedaje = document.getElementById("diasHospedaje");
    const maximoHospedaje = document.getElementById("maximoHospedaje");
    const mensajeHospedaje = document.getElementById("mensajeHospedaje");

    const idGasto = document.getElementById("idGasto");
    const tituloFormulario = document.getElementById("tituloFormularioGasto");
    const btnGuardarGasto = document.getElementById("btnGuardarGasto");
    const textoGuardarGasto = document.getElementById("textoGuardarGasto");
    const iconoGuardarGasto = document.getElementById("iconoGuardarGasto");
    const btnCancelarEdicion = document.getElementById("btnCancelarEdicion");
    const comprobanteActual = document.getElementById("comprobanteActual");
    const linkComprobanteActual = document.getElementById("linkComprobanteActual");

    const urlConsultarRuc = formGasto.dataset.consultarRucUrl || "";
    const createUrl = formGasto.dataset.createUrl || formGasto.action;
    const editUrl = formGasto.dataset.editUrl || "";
    const fechaInicioRendicion = formGasto.dataset.fechaInicio || "";
    const fechaFinRendicion = formGasto.dataset.fechaFin || "";
    const estadoRechazada = formGasto.dataset.estadoRechazada === "true";

    let ultimaConsulta = "";
    let diasHospedajeEditadosManualmente = false;

    const LIMITE_ALIMENTACION = 40;
    const LIMITE_MOVILIDAD_INTERNA = 20;
    const LIMITE_HOSPEDAJE = 50;
    const TAMANIO_MAXIMO_ARCHIVO = 5 * 1024 * 1024;

    function normalizarTexto(texto) {
        return (texto || "").toString().normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
    }

    function obtenerNombreTipo() {
        if (!tipoGasto) return "";
        return tipoGasto.options[tipoGasto.selectedIndex]?.text || "";
    }

    function obtenerNombreComprobante() {
        if (!tipoComprobante) return "";
        return tipoComprobante.options[tipoComprobante.selectedIndex]?.text || "";
    }

    function esMovilidad() {
        const tipo = normalizarTexto(obtenerNombreTipo());
        return tipo.includes("movilidad") && !tipo.includes("movilidad interna");
    }

    function esMovilidadInterna() {
        if (!tipoGasto) {
            return false;
        }

        const texto =
            normalizarTexto(
                tipoGasto.options[
                    tipoGasto.selectedIndex
                ]?.text || ""
            );

        return texto === "movilidad interna";
    }

    function esMovilidadNormal() {
        return esMovilidad() && !esMovilidadInterna();
    }

    function esHospedaje() {
        const tipo = normalizarTexto(obtenerNombreTipo());
        return tipo.includes("hospedaje");
    }

    // =========================================================
    // OTROS
    // =========================================================
    function esOtros() {
        const tipo = normalizarTexto(obtenerNombreTipo());
        return tipo === "otros" || tipo.includes("otros");
    }

    function esFactura() {
        const comprobante = normalizarTexto(obtenerNombreComprobante());
        return comprobante.includes("factura");
    }

    function rucEsObligatorio() {
        if (esMovilidadInterna()) return false;
        if (esMovilidadNormal()) return true;
        return esFactura();
    }

    function estaEditando() {
        return idGasto && parseInt(idGasto.value || "0", 10) > 0;
    }

    function tieneComprobanteActual() {
        return estaEditando() &&
            comprobanteActual &&
            comprobanteActual.style.display !== "none" &&
            linkComprobanteActual &&
            linkComprobanteActual.href &&
            linkComprobanteActual.href !== "#" &&
            linkComprobanteActual.href !== "";
    }

    const movilidad = esMovilidad();
    const movilidadInterna = esMovilidadInterna();
    const movilidadNormal = esMovilidadNormal();
    const hospedaje = esHospedaje();
    const otros = esOtros();

    function actualizarRequisitos() {

        // =====================================================
        // OBTENER SIEMPRE EL TIPO ACTUALMENTE SELECCIONADO
        // =====================================================

        const movilidadActual =
            esMovilidad();

        const movilidadInternaActual =
            esMovilidadInterna();

        const movilidadNormalActual =
            esMovilidadNormal();

        const hospedajeActual =
            esHospedaje();

        const otrosActual =
            esOtros();

        if (domicilio) {
            domicilio.removeAttribute("readonly");
        }

        if (periodoHospedaje) {
            periodoHospedaje.style.display =
                hospedajeActual ? "block" : "none";
        }

        if (!hospedajeActual) {
            if (fechaInicioHospedaje) {
                fechaInicioHospedaje.value = "";
                fechaInicioHospedaje.removeAttribute("required");
            }

            if (fechaFinHospedaje) {
                fechaFinHospedaje.value = "";
                fechaFinHospedaje.removeAttribute("required");
            }

            if (diasHospedaje) {
                diasHospedaje.value = "";
                diasHospedaje.removeAttribute("required");
                diasHospedaje.removeAttribute("min");
            }

            if (maximoHospedaje) maximoHospedaje.value = "";

            if (mensajeHospedaje) {
                mensajeHospedaje.textContent = "";
                mensajeHospedaje.className = "small mt-3";
            }

            if (montoTotal) montoTotal.removeAttribute("max");

            diasHospedajeEditadosManualmente = false;

        } else {
            fechaInicioHospedaje?.setAttribute("required", "required");
            fechaFinHospedaje?.setAttribute("required", "required");
            diasHospedaje?.setAttribute("required", "required");
            diasHospedaje?.setAttribute("min", "1");
        }

        // =====================================================
        // OTROS
        // =====================================================
        if (otrosActual) {
            ruc?.removeAttribute("required");
            razonSocial?.removeAttribute("required");
            domicilio?.removeAttribute("required");

            tipoComprobante?.removeAttribute("required");
            serie?.removeAttribute("required");
            numero?.removeAttribute("required");

            archivo?.removeAttribute("required");

            if (mensajeMovilidad) {
                mensajeMovilidad.style.display = "none";
            }

            if (mensajeArchivo) {
                mensajeArchivo.innerHTML =
                    'PDF, JPG, JPEG o PNG. <strong>Opcional para Otros.</strong>';

                mensajeArchivo.className =
                    "form-text text-success";
            }

            if (mensajeTipoGasto) {
                mensajeTipoGasto.textContent =
                    "Para el tipo de gasto Otros, el RUC, comprobante y voucher son opcionales.";

                mensajeTipoGasto.className =
                    "form-text text-success";
            }

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "Para Otros puede registrar el gasto sin comprobante. Si cuenta con uno, puede seleccionarlo y adjuntarlo.";

                mensajeComprobante.className =
                    "form-text text-success";
            }

            [
                ruc,
                razonSocial,
                domicilio,
                tipoComprobante,
                serie,
                numero,
                archivo
            ].forEach(x => {
                x?.classList.remove("is-invalid");

                if (x) {
                    x.setCustomValidity("");
                }
            });

        } else if (movilidadInternaActual) {

            // =====================================================
            // MOVILIDAD INTERNA
            // SIN COMPROBANTE
            // =====================================================

            ruc?.removeAttribute("required");
            razonSocial?.removeAttribute("required");
            domicilio?.removeAttribute("required");
            tipoComprobante?.removeAttribute("required");
            serie?.removeAttribute("required");
            numero?.removeAttribute("required");
            archivo?.removeAttribute("required");

            // Ocultar completamente los datos del comprobante
            if (seccionDatosComprobante) {
                seccionDatosComprobante.style.display = "none";
            }

            // Limpiar valores
            if (ruc) ruc.value = "";
            if (razonSocial) razonSocial.value = "";
            if (domicilio) domicilio.value = "";
            if (tipoComprobante) tipoComprobante.value = "";
            if (serie) serie.value = "";
            if (numero) numero.value = "";
            if (archivo) archivo.value = "";

            if (mensajeMovilidad) {
                mensajeMovilidad.style.display = "none";
            }

            if (mensajeArchivo) {
                mensajeArchivo.textContent = "";
                mensajeArchivo.className = "form-text";
            }

            if (mensajeTipoGasto) {
                mensajeTipoGasto.textContent = "";
                mensajeTipoGasto.className = "form-text";
            }

            if (mensajeComprobante) {
                mensajeComprobante.textContent = "";
                mensajeComprobante.className = "form-text";
            }

            [
                ruc,
                razonSocial,
                domicilio,
                tipoComprobante,
                serie,
                numero,
                archivo
            ].forEach(x => {

                if (!x) return;

                x.classList.remove("is-invalid");
                x.setCustomValidity("");
            });
            // =====================================================
            // MOVILIDAD NORMAL
            // =====================================================
        } else if (movilidadNormalActual) {

            ruc?.setAttribute("required", "required");
            razonSocial?.setAttribute("required", "required");
            domicilio?.setAttribute("required", "required");
            tipoComprobante?.setAttribute("required", "required");
            serie?.removeAttribute("required");
            numero?.removeAttribute("required");

            if (!tieneComprobanteActual()) {
                archivo?.setAttribute("required", "required");
            } else {
                archivo?.removeAttribute("required");
            }

            if (mensajeMovilidad) {
                mensajeMovilidad.style.display = "block";
            }

            if (mensajeArchivo) {
                mensajeArchivo.innerHTML =
                    "PDF, JPG, JPEG o PNG. <strong>Obligatorio para Movilidad.</strong>";
            }

            if (mensajeTipoGasto) {
                mensajeTipoGasto.textContent =
                    "Movilidad: debe ingresar un RUC válido, consultar sus datos, seleccionar un comprobante y adjuntar el voucher.";

                mensajeTipoGasto.className =
                    "form-text text-primary";
            }

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "Para Movilidad el comprobante es obligatorio.";

                mensajeComprobante.className =
                    "form-text text-primary";
            }

            ruc?.classList.remove("is-invalid");
            razonSocial?.classList.remove("is-invalid");
            domicilio?.classList.remove("is-invalid");

            // =====================================================
            // RESTO DE TIPOS DE GASTO
            // =====================================================
        } else {

            if (mensajeMovilidad) {
                mensajeMovilidad.style.display = "none";
            }

            tipoComprobante?.setAttribute("required", "required");

            if (!tieneComprobanteActual()) {
                archivo?.setAttribute("required", "required");
            } else {
                archivo?.removeAttribute("required");
            }

            if (rucEsObligatorio()) {
                ruc?.setAttribute("required", "required");
            } else {
                ruc?.removeAttribute("required");
            }

            if (rucEsObligatorio()) {
                razonSocial?.setAttribute("required", "required");
                domicilio?.setAttribute("required", "required");
            } else {
                razonSocial?.removeAttribute("required");
                domicilio?.removeAttribute("required");
            }

            serie?.removeAttribute("required");
            numero?.removeAttribute("required");

            if (mensajeArchivo) {
                mensajeArchivo.innerHTML =
                    "PDF, JPG, JPEG o PNG. <strong>Obligatorio excepto para Movilidad interna.</strong>";
            }

            if (mensajeTipoGasto) {
                if (esFactura()) {
                    mensajeTipoGasto.textContent =
                        "Factura: debe ingresar un RUC válido.";

                    mensajeTipoGasto.className =
                        "form-text text-primary";

                } else if (tipoComprobante?.value) {

                    mensajeTipoGasto.textContent =
                        "RUC opcional para este tipo de comprobante. Si lo ingresa, puede consultarlo.";

                    mensajeTipoGasto.className =
                        "form-text text-success";

                } else {
                    mensajeTipoGasto.textContent = "";
                    mensajeTipoGasto.className = "form-text";
                }
            }
        }
    }

    async function consultarRuc() {
        const numeroRuc = ruc?.value.trim() || "";

        if (!/^\d{11}$/.test(numeroRuc)) {
            if (mensajeRuc) {
                mensajeRuc.textContent =
                    "Ingrese un RUC válido de 11 dígitos.";

                mensajeRuc.className =
                    "form-text text-danger";
            }

            return;
        }

        if (numeroRuc === ultimaConsulta && numeroRuc !== "") return;

        if (razonSocial) razonSocial.value = "";

        if (domicilio) {
            domicilio.value = "";
            domicilio.removeAttribute("readonly");
        }

        if (mensajeRuc) mensajeRuc.textContent = "";

        if (!urlConsultarRuc) {
            if (mensajeRuc) {
                mensajeRuc.textContent =
                    "No se pudo determinar la dirección de consulta del RUC.";

                mensajeRuc.className =
                    "form-text text-danger";
            }

            return;
        }

        if (btnRuc) {
            btnRuc.disabled = true;
            btnRuc.innerHTML =
                '<span class="spinner-border spinner-border-sm me-1"></span>Buscando...';
        }

        try {
            const separador =
                urlConsultarRuc.includes("?") ? "&" : "?";

            const url =
                urlConsultarRuc +
                separador +
                "ruc=" +
                encodeURIComponent(numeroRuc);

            const response = await fetch(url, {
                method: "GET",
                headers: {
                    Accept: "application/json"
                }
            });

            let data;

            try {
                data = await response.json();
            } catch {
                throw new Error(
                    "La respuesta del servidor no es válida."
                );
            }

            if (!response.ok) {
                throw new Error(
                    data?.mensaje || "No se encontró el RUC."
                );
            }

            if (razonSocial) {
                razonSocial.value =
                    data?.razonSocial ?? "";
            }

            if (domicilio) {
                domicilio.value =
                    data?.domicilioFiscal ?? "";

                domicilio.removeAttribute("readonly");
            }

            if (mensajeRuc) {
                mensajeRuc.textContent =
                    `Estado: ${data?.estado ?? "-"} | Condición: ${data?.condicion ?? "-"}`;

                mensajeRuc.className =
                    "form-text text-success";
            }

            ultimaConsulta = numeroRuc;

            validarComprobantePorRuc();

        } catch (error) {

            ultimaConsulta = "";

            if (mensajeRuc) {
                mensajeRuc.textContent =
                    error?.message ||
                    "No se pudo consultar el RUC.";

                mensajeRuc.className =
                    "form-text text-danger";
            }

        } finally {

            if (btnRuc) {
                btnRuc.disabled = false;

                btnRuc.innerHTML =
                    '<i class="bi bi-search me-1"></i>Buscar';
            }
        }
    }

    function validarComprobantePorRuc() {

        // =====================================================
        // OTROS: COMPROBANTE TOTALMENTE OPCIONAL
        // =====================================================
        if (esOtros()) {

            tipoComprobante?.classList.remove("is-invalid");

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "Para Otros el comprobante es opcional.";

                mensajeComprobante.className =
                    "form-text text-success";
            }

            return true;
        }

        if (esMovilidadInterna()) {
            tipoComprobante?.classList.remove("is-invalid");
            return true;
        }

        if (!ruc || !tipoComprobante) return true;

        const numeroRuc = ruc.value.trim();

        const opcionSeleccionada =
            tipoComprobante.options[
            tipoComprobante.selectedIndex
            ];

        const comprobante =
            normalizarTexto(
                opcionSeleccionada?.text || ""
            );

        if (!tipoComprobante.value) {

            if (mensajeComprobante) {
                mensajeComprobante.textContent = "";
            }

            tipoComprobante.classList.remove("is-invalid");

            return true;
        }

        if (numeroRuc && !/^\d{11}$/.test(numeroRuc)) {

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "El RUC debe contener exactamente 11 dígitos.";

                mensajeComprobante.className =
                    "form-text text-danger";
            }

            tipoComprobante.classList.add("is-invalid");

            return false;
        }

        const rucEmpiezaEn20 =
            numeroRuc.startsWith("20");

        if (rucEmpiezaEn20) {

            if (comprobante.includes("factura")) {

                if (mensajeComprobante) {
                    mensajeComprobante.textContent =
                        "RUC iniciado en 20: solo se permite FACTURA.";

                    mensajeComprobante.className =
                        "form-text text-success";
                }

                tipoComprobante.classList.remove("is-invalid");

                return true;
            }

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "El RUC ingresado empieza con 20. Para este RUC únicamente se permite FACTURA.";

                mensajeComprobante.className =
                    "form-text text-danger";
            }

            tipoComprobante.classList.add("is-invalid");

            return false;
        }

        if (comprobante.includes("factura")) {

            if (!/^\d{11}$/.test(numeroRuc)) {

                if (mensajeComprobante) {
                    mensajeComprobante.textContent =
                        "Para una FACTURA debe ingresar un RUC válido de 11 dígitos.";

                    mensajeComprobante.className =
                        "form-text text-danger";
                }

                tipoComprobante.classList.add("is-invalid");

                return false;
            }

            if (mensajeComprobante) {
                mensajeComprobante.textContent =
                    "Factura seleccionada con RUC válido.";

                mensajeComprobante.className =
                    "form-text text-success";
            }

            tipoComprobante.classList.remove("is-invalid");

            return true;
        }

        if (mensajeComprobante) {
            mensajeComprobante.textContent =
                numeroRuc
                    ? "RUC válido. Puede utilizar este comprobante."
                    : "Debe ingresar un RUC válido.";

            mensajeComprobante.className =
                "form-text text-success";
        }

        tipoComprobante.classList.remove("is-invalid");

        return true;
    }

    function obtenerLimiteTipo() {
        if (!tipoGasto) return null;

        const texto =
            tipoGasto.options[
                tipoGasto.selectedIndex
            ]?.text || "";

        const tipo = normalizarTexto(texto);

        if (tipo.includes("alimentacion")) {
            return LIMITE_ALIMENTACION;
        }

        if (tipo.includes("movilidad interna")) {
            return LIMITE_MOVILIDAD_INTERNA;
        }

        return null;
    }

    function obtenerTotalExistentePorDia(fecha, tipo) {
        if (!fecha || !tipo) return 0;

        const tipoBuscado =
            normalizarTexto(tipo);

        const idActual =
            estaEditando()
                ? parseInt(idGasto.value || "0", 10)
                : 0;

        let totalExistente = 0;

        document
            .querySelectorAll(
                ".table-gastos tbody tr.fila-gasto"
            )
            .forEach(function (fila) {

                const idFila =
                    parseInt(
                        fila.dataset.idGasto || "0",
                        10
                    );

                if (
                    idActual > 0 &&
                    idFila === idActual
                ) {
                    return;
                }

                const celdas =
                    fila.querySelectorAll("td");

                if (celdas.length < 7) return;

                const fechaTexto =
                    celdas[0]
                        ?.textContent
                        ?.trim()
                        .split("\n")[0]
                        .trim() || "";

                const partes =
                    fechaTexto.split("/");

                if (partes.length !== 3) return;

                const fechaFila =
                    `${partes[2]}-${partes[1].padStart(2, "0")}-${partes[0].padStart(2, "0")}`;

                if (fechaFila !== fecha) return;

                const tipoFila =
                    normalizarTexto(
                        celdas[1]?.textContent || ""
                    );

                if (!tipoFila.includes(tipoBuscado)) {
                    return;
                }

                const totalTexto =
                    (
                        celdas[6]?.textContent || ""
                    )
                        .replace("S/", "")
                        .replace(/,/g, "")
                        .trim();

                const total =
                    parseFloat(totalTexto) || 0;

                totalExistente += total;
            });

        return totalExistente;
    }

    function formatearFecha(fecha) {
        if (!fecha) return "";

        const partes = fecha.split("-");

        if (partes.length !== 3) {
            return fecha;
        }

        return `${partes[2]}/${partes[1]}/${partes[0]}`;
    }

    function validarLimiteDiario(mostrarMensaje = true) {

        if (!fechaGasto || !tipoGasto || !montoTotal) {
            return true;
        }

        // =========================================================
        // HOSPEDAJE
        // =========================================================
        if (esHospedaje()) {

            if (mensajeLimiteMonto) {
                mensajeLimiteMonto.textContent = "";
                mensajeLimiteMonto.className = "form-text";
            }

            montoTotal.removeAttribute("max");

            return true;
        }

        // =========================================================
        // MOVILIDAD NORMAL Y OTROS
        // SIN LÍMITE DIARIO
        // =========================================================
        if (esMovilidad() || esOtros()) {

            if (mensajeLimiteMonto) {
                mensajeLimiteMonto.textContent = "";
                mensajeLimiteMonto.className = "form-text";
            }

            montoTotal.removeAttribute("max");

            return true;
        }

        // =========================================================
        // MOVILIDAD INTERNA
        // LÍMITE INTERNO DE S/ 20
        // NO SE MUESTRA AL USUARIO
        // =========================================================
        if (esMovilidadInterna()) {

            const fecha = fechaGasto.value;

            if (!fecha) {
                if (mensajeLimiteMonto) {
                    mensajeLimiteMonto.textContent = "";
                    mensajeLimiteMonto.className = "form-text";
                }

                montoTotal.removeAttribute("max");

                return true;
            }

            const tipo = obtenerNombreTipo();

            const limite = LIMITE_MOVILIDAD_INTERNA;

            const monto =
                parseFloat(montoTotal.value) || 0;

            const totalExistente =
                obtenerTotalExistentePorDia(
                    fecha,
                    tipo
                );

            const disponible =
                Math.max(
                    0,
                    limite - totalExistente
                );

            // No mostramos el límite en el input
            montoTotal.removeAttribute("max");

            // No mostramos información del límite mientras escribe
            if (mensajeLimiteMonto) {
                mensajeLimiteMonto.textContent = "";
                mensajeLimiteMonto.className = "form-text";
            }

            // Si ya alcanzó el límite
            if (totalExistente >= limite) {

                if (mostrarMensaje) {
                    mensajeLimiteMonto.textContent =
                        "No puede registrar más gastos de Movilidad interna para esta fecha.";

                    mensajeLimiteMonto.className =
                        "form-text text-danger";
                }

                return false;
            }

            // Si el nuevo gasto supera lo disponible
            if (monto > disponible) {

                if (mostrarMensaje) {
                    mensajeLimiteMonto.textContent =
                        "El monto ingresado supera el monto disponible para Movilidad interna en esta fecha.";

                    mensajeLimiteMonto.className =
                        "form-text text-danger";
                }

                return false;
            }

            return true;
        }

        // =========================================================
        // RESTO DE TIPOS CON LÍMITE DIARIO
        // =========================================================

        const fecha = fechaGasto.value;
        const tipo = obtenerNombreTipo();
        const limite = obtenerLimiteTipo();

        const monto =
            parseFloat(montoTotal.value) || 0;

        if (limite === null) {

            if (mensajeLimiteMonto) {
                mensajeLimiteMonto.textContent = "";
                mensajeLimiteMonto.className = "form-text";
            }

            montoTotal.removeAttribute("max");

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
                limite - totalExistente
            );

        montoTotal.max =
            disponible.toFixed(2);

        if (totalExistente >= limite) {

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
                    `El máximo permitido para ${tipo.toLowerCase()} en esta fecha es S/ ${disponible.toFixed(2)}. El límite diario es S/ ${limite.toFixed(2)}.`;

                mensajeLimiteMonto.className =
                    "form-text text-danger";
            }

            return false;
        }

        if (mostrarMensaje) {
            mensajeLimiteMonto.textContent =
                `Límite diario: S/ ${limite.toFixed(2)} | Registrado: S/ ${totalExistente.toFixed(2)} | Disponible: S/ ${disponible.toFixed(2)}`;

            mensajeLimiteMonto.className =
                "form-text text-muted";
        }

        return true;
    }

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
            Number.isNaN(inicio.getTime()) ||
            Number.isNaN(fin.getTime())
        ) {
            return 0;
        }

        if (fin < inicio) return 0;

        return Math.floor(
            (fin - inicio) /
            (1000 * 60 * 60 * 24)
        ) + 1;
    }

    function actualizarDiasDesdeFechas() {
        if (
            !diasHospedaje ||
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

    function existeCruceHospedaje(
        inicioNuevo,
        finNuevo
    ) {
        if (!inicioNuevo || !finNuevo) {
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

        const idActual =
            estaEditando()
                ? parseInt(
                    idGasto.value || "0",
                    10
                )
                : 0;

        if (
            Number.isNaN(nuevoInicio.getTime()) ||
            Number.isNaN(nuevoFin.getTime())
        ) {
            return false;
        }

        const filas =
            document.querySelectorAll(
                ".table-gastos tbody tr.fila-gasto"
            );

        for (const fila of filas) {

            const idFila =
                parseInt(
                    fila.dataset.idGasto || "0",
                    10
                );

            if (
                idActual > 0 &&
                idFila === idActual
            ) {
                continue;
            }

            const inicioExistente =
                fila.dataset.inicioHospedaje || "";

            const finExistente =
                fila.dataset.finHospedaje || "";

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

    function calcularHospedaje(
        mostrarMensaje = true
    ) {
        if (!esHospedaje()) return true;

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

        if (!inicio || !fin) {

            if (maximoHospedaje) {
                maximoHospedaje.value = "";
            }

            montoTotal.removeAttribute("max");

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
            Number.isNaN(fechaInicio.getTime()) ||
            Number.isNaN(fechaFin.getTime())
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
            dias = obtenerDiasHospedaje();

            if (dias > 0) {
                diasHospedaje.value =
                    dias.toString();
            }
        }

        if (
            !Number.isFinite(dias) ||
            dias < 1
        ) {
            if (maximoHospedaje) {
                maximoHospedaje.value = "";
            }

            montoTotal.removeAttribute("max");

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

        const maximo =
            dias *
            LIMITE_HOSPEDAJE;

        if (maximoHospedaje) {
            maximoHospedaje.value =
                `S/ ${maximo.toFixed(2)}`;
        }

        montoTotal.max =
            maximo.toFixed(2);

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
                    `El monto ingresado es S/ ${monto.toFixed(2)}. Para ${dias} día(s) de hospedaje, el máximo permitido es S/ ${maximo.toFixed(2)} (S/ ${LIMITE_HOSPEDAJE.toFixed(2)} × ${dias}).`;

                mensajeHospedaje.className =
                    "small mt-3 hospedaje-invalido";
            }

            return false;
        }

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
            } else {
                base =
                    Math.round(
                        (total / 1.18) * 100
                    ) / 100;

                impuesto =
                    Math.round(
                        (total - base) * 100
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

    function limpiarFormulario() {

        idGasto.value = "0";
        formGasto.action = createUrl;

        fechaGasto.value = "";
        tipoGasto.value = "";

        formGasto.querySelector(
            '[name="Detalle"]'
        ).value = "";

        ruc.value = "";
        razonSocial.value = "";
        domicilio.value = "";
        tipoComprobante.value = "";
        serie.value = "";
        numero.value = "";
        archivo.value = "";
        montoTotal.value = "";

        valorVenta.value = "0.00";
        igv.value = "0.00";

        exoneracion.checked = false;

        fechaInicioHospedaje.value = "";
        fechaFinHospedaje.value = "";
        diasHospedaje.value = "";

        diasHospedajeEditadosManualmente = false;
        ultimaConsulta = "";

        if (comprobanteActual) {
            comprobanteActual.style.display = "none";
        }

        if (linkComprobanteActual) {
            linkComprobanteActual.href = "#";
        }

        btnCancelarEdicion.style.display = "none";

        textoGuardarGasto.textContent =
            estadoRechazada
                ? "Agregar gasto"
                : "Registrar gasto";

        iconoGuardarGasto.className =
            "bi bi-plus-circle me-1";

        tituloFormulario.innerHTML = `
<i class="bi bi-plus-circle me-2 text-primary"></i>
${estadoRechazada
                ? "Agregar gasto para corregir la rendición"
                : "Registrar gasto"}
`;

        actualizarRequisitos();
        calcularIgv();

        if (esHospedaje()) {
            calcularHospedaje(false);
        } else {
            validarLimiteDiario(false);
        }
    }

    function cargarGasto(fila) {

        const data = fila.dataset;

        idGasto.value =
            data.idGasto || "0";

        formGasto.action =
            editUrl;

        fechaGasto.value =
            data.fecha || "";

        tipoGasto.value =
            data.idTipoGasto || "";

        formGasto.querySelector(
            '[name="Detalle"]'
        ).value =
            data.detalle || "";

        ruc.value =
            data.ruc || "";

        razonSocial.value =
            data.razonSocial || "";

        domicilio.value =
            data.domicilioFiscal || "";

        tipoComprobante.value =
            data.idTipoComprobante || "";

        serie.value =
            data.serie || "";

        numero.value =
            data.numero || "";

        montoTotal.value =
            data.montoTotal || "";

        valorVenta.value =
            data.valorVenta || "0.00";

        igv.value =
            data.igv || "0.00";

        exoneracion.checked =
            data.exoneracionIgv === "true";

        fechaInicioHospedaje.value =
            data.inicioHospedaje || "";

        fechaFinHospedaje.value =
            data.finHospedaje || "";

        diasHospedaje.value =
            data.diasHospedaje || "";

        diasHospedajeEditadosManualmente =
            false;

        ultimaConsulta = "";

        if (
            data.comprobante &&
            data.comprobante.trim() !== ""
        ) {
            comprobanteActual.style.display =
                "block";

            linkComprobanteActual.href =
                data.comprobante;

        } else {

            comprobanteActual.style.display =
                "none";

            linkComprobanteActual.href =
                "#";
        }

        btnCancelarEdicion.style.display =
            "inline-block";

        textoGuardarGasto.textContent =
            "Guardar cambios";

        iconoGuardarGasto.className =
            "bi bi-check-circle me-1";

        tituloFormulario.innerHTML = `
<i class="bi bi-pencil-square me-2 text-primary"></i>
Editar gasto #${data.idGasto}
`;

        actualizarRequisitos();

        if (
            data.comprobante &&
            data.comprobante.trim() !== ""
        ) {
            archivo.removeAttribute("required");
        }

        calcularIgv();

        if (esHospedaje()) {
            calcularHospedaje(false);
        } else {
            validarLimiteDiario(false);
        }

        formGasto.scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    }

    document
        .querySelectorAll(".btn-editar-gasto")
        .forEach(function (boton) {

            boton.addEventListener(
                "click",
                function () {

                    const id =
                        this.dataset.id;

                    const fila =
                        document.querySelector(
                            `.fila-gasto[data-id-gasto="${id}"]`
                        );

                    if (fila) {
                        cargarGasto(fila);
                    }
                }
            );
        });

    btnCancelarEdicion?.addEventListener(
        "click",
        function () {

            limpiarFormulario();

            formGasto.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });
        }
    );

    if (ruc && btnRuc) {

        btnRuc.addEventListener(
            "click",
            function (event) {

                event.preventDefault();

                consultarRuc();
            }
        );

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
                        razonSocial.value = "";
                    }

                    if (domicilio) {
                        domicilio.value = "";
                        domicilio.removeAttribute("readonly");
                    }

                    if (mensajeRuc) {
                        mensajeRuc.textContent = "";
                    }

                    if (mensajeComprobante) {
                        mensajeComprobante.textContent = "";
                    }

                    tipoComprobante?.classList.remove(
                        "is-invalid"
                    );
                }
            }
        );
    }

    tipoComprobante?.addEventListener(
        "change",
        function () {

            actualizarRequisitos();
            validarComprobantePorRuc();
        }
    );

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

            } else {

                validarLimiteDiario(true);
            }

            calcularIgv();
        }
    );

    fechaGasto?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) {
                validarLimiteDiario(true);
            }

            calcularIgv();
        }
    );

    fechaInicioHospedaje?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) return;

            actualizarDiasDesdeFechas();
            calcularHospedaje(true);
        }
    );

    fechaFinHospedaje?.addEventListener(
        "change",
        function () {

            if (!esHospedaje()) return;

            actualizarDiasDesdeFechas();
            calcularHospedaje(true);
        }
    );

    diasHospedaje?.addEventListener(
        "input",
        function () {

            diasHospedajeEditadosManualmente =
                true;

            this.value =
                this.value.replace(/\D/g, "");

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
            } else {
                this.value =
                    dias.toString();
            }

            calcularHospedaje(true);
        }
    );

    montoTotal?.addEventListener(
        "input",
        function () {

            if (esHospedaje()) {
                calcularHospedaje(true);
            } else {
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
            } else {
                validarLimiteDiario(true);
            }

            calcularIgv();
        }
    );

    exoneracion?.addEventListener(
        "change",
        calcularIgv
    );

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
                    extension =>
                        nombre.endsWith(extension)
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

    formGasto.addEventListener(
        "submit",
        function (event) {

            const movilidad =
                esMovilidad();

            const movilidadInterna =
                esMovilidadInterna();

            const movilidadNormal =
                esMovilidadNormal();

            const hospedaje =
                esHospedaje();

            const otros =
                esOtros();

            const tieneArchivoNuevo =
                archivo?.files?.length > 0;

            const tieneArchivoAnterior =
                tieneComprobanteActual();

            // =================================================
            // OTROS
            // =================================================
            if (otros) {

                const numeroRuc =
                    ruc?.value.trim() || "";

                if (
                    numeroRuc &&
                    !/^\d{11}$/.test(numeroRuc)
                ) {

                    event.preventDefault();

                    mostrarAlerta(
                        "RUC inválido",
                        "El RUC es opcional para Otros, pero si lo ingresa debe contener exactamente 11 dígitos.",
                        "warning"
                    );

                    ruc?.focus();

                    return;
                }

                // No se exige:
                // - RUC
                // - comprobante
                // - voucher
                // - serie
                // - número
                // - razón social
                // - domicilio

            } else if (movilidadInterna) {

                // =================================================
                // MOVILIDAD INTERNA
                // NO REQUIERE COMPROBANTE NI RUC
                // =================================================

                if (ruc) {
                    ruc.value = "";
                    ruc.removeAttribute("required");
                    ruc.setCustomValidity("");
                    ruc.classList.remove("is-invalid");
                }

                if (razonSocial) {
                    razonSocial.value = "";
                    razonSocial.removeAttribute("required");
                    razonSocial.setCustomValidity("");
                    razonSocial.classList.remove("is-invalid");
                }

                if (domicilio) {
                    domicilio.value = "";
                    domicilio.removeAttribute("required");
                    domicilio.setCustomValidity("");
                    domicilio.classList.remove("is-invalid");
                }

                if (tipoComprobante) {
                    tipoComprobante.value = "";
                    tipoComprobante.removeAttribute("required");
                    tipoComprobante.setCustomValidity("");
                    tipoComprobante.classList.remove("is-invalid");
                }

                if (serie) {
                    serie.value = "";
                    serie.removeAttribute("required");
                    serie.setCustomValidity("");
                    serie.classList.remove("is-invalid");
                }

                if (numero) {
                    numero.value = "";
                    numero.removeAttribute("required");
                    numero.setCustomValidity("");
                    numero.classList.remove("is-invalid");
                }

                if (archivo) {
                    archivo.value = "";
                    archivo.removeAttribute("required");
                    archivo.setCustomValidity("");
                    archivo.classList.remove("is-invalid");
                }

                if (seccionDatosComprobante) {
                    seccionDatosComprobante.style.display = "none";
                }

                // =================================================
                // MOVILIDAD NORMAL
                // =================================================

                // =================================================
                // MOVILIDAD NORMAL
                // =================================================
            } else if (movilidadNormal) {

                const numeroRuc =
                    ruc?.value.trim() || "";

                if (
                    !/^\d{11}$/.test(numeroRuc)
                ) {

                    event.preventDefault();

                    mostrarAlerta(
                        "RUC obligatorio",
                        "Para Movilidad debe ingresar un RUC válido de 11 dígitos y consultar sus datos.",
                        "error"
                    );

                    ruc?.focus();

                    return;
                }

                if (
                    !tipoComprobante ||
                    !tipoComprobante.value
                ) {

                    event.preventDefault();

                    mostrarAlerta(
                        "Comprobante obligatorio",
                        "Debe seleccionar el tipo de comprobante para Movilidad.",
                        "warning"
                    );

                    tipoComprobante?.focus();

                    return;
                }

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

                if (
                    !tieneArchivoNuevo &&
                    !tieneArchivoAnterior
                ) {

                    event.preventDefault();

                    mostrarAlerta(
                        "Voucher obligatorio",
                        "Debe adjuntar el comprobante o voucher de Movilidad.",
                        "warning"
                    );

                    archivo?.focus();

                    return;
                }
            }

            // =================================================
            // HOSPEDAJE
            // =================================================
            if (hospedaje) {

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

                    } else if (
                        !fechaFinHospedaje?.value
                    ) {
                        fechaFinHospedaje?.focus();

                    } else if (
                        !diasHospedaje?.value ||
                        parseInt(
                            diasHospedaje.value,
                            10
                        ) < 1
                    ) {
                        diasHospedaje?.focus();

                    } else {
                        montoTotal?.focus();
                    }

                    return;
                }

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

                const numeroRuc =
                    ruc?.value.trim() || "";

                if (
                    numeroRuc &&
                    !/^\d{11}$/.test(numeroRuc)
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

                if (
                    !tieneArchivoNuevo &&
                    !tieneArchivoAnterior
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

                // =================================================
                // OTROS TIPOS DE GASTO
                // =================================================
            } else if (
                !otros &&
                !movilidadInterna &&
                !movilidadNormal &&
                !hospedaje
            ) {

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

                const numeroRuc =
                    ruc?.value.trim() || "";

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

                } else if (
                    numeroRuc &&
                    !/^\d{11}$/.test(numeroRuc)
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

                if (
                    !tieneArchivoNuevo &&
                    !tieneArchivoAnterior
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

            // =================================================
            // LÍMITE DIARIO
            // =================================================
            if (
                !movilidad &&
                !hospedaje &&
                !otros
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
                                limite - existente
                            )
                            : 0;

                    if (
                        limite !== null &&
                        existente >= limite
                    ) {

                        mostrarAlerta(
                            "Límite diario alcanzado",
                            `Ya registró S/ ${existente.toFixed(2)} en ${tipo.toLowerCase()} para el día seleccionado.<br><br><strong>Límite permitido: S/ ${limite.toFixed(2)}</strong><br>No puede registrar otro gasto de este tipo en esta fecha.`,
                            "warning"
                        );

                    } else if (
                        limite !== null &&
                        monto > disponible
                    ) {

                        mostrarAlerta(
                            "Monto excede el límite",
                            `El gasto ingresado es de <strong>S/ ${monto.toFixed(2)}</strong>.<br><br>Ya tiene registrado: <strong>S/ ${existente.toFixed(2)}</strong>.<br>Disponible: <strong>S/ ${disponible.toFixed(2)}</strong>.<br><br>Límite diario de ${tipo.toLowerCase()}: <strong>S/ ${limite.toFixed(2)}</strong>.`,
                            "warning"
                        );

                    } else {

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

    function mostrarAlerta(
        titulo,
        mensaje,
        tipo
    ) {

        if (typeof Swal !== "undefined") {

            Swal.fire({
                icon: tipo,
                title: titulo,
                html: mensaje,
                confirmButtonText: "Entendido",
                confirmButtonColor: "#0C4A8A"
            });

            return;
        }

        const mensajePlano =
            String(mensaje || "")
                .replace(/<[^>]*>/g, "");

        alert(
            titulo +
            "\n\n" +
            mensajePlano
        );
    }

    if (domicilio) {
        domicilio.removeAttribute("readonly");
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

        } else {

            actualizarDiasDesdeFechas();
        }

        calcularHospedaje(false);

    } else {

        validarLimiteDiario(false);
    }

    if (tipoComprobante?.value) {
        validarComprobantePorRuc();
    }
});