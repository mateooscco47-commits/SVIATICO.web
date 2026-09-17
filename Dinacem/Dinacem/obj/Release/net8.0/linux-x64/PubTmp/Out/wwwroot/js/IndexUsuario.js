document.addEventListener("DOMContentLoaded", function () {

    const passwordRegex =
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/;

    const emailRegex =
        /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    const btnMostrarRegistro =
        document.getElementById("btnMostrarRegistro");

    const btnCancelarRegistro =
        document.getElementById("btnCancelarRegistro");

    const formularioRegistro =
        document.getElementById("formularioRegistro");

    if (btnMostrarRegistro && formularioRegistro) {
        btnMostrarRegistro.addEventListener("click", function () {
            formularioRegistro.style.display = "block";

            formularioRegistro.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });
        });
    }

    if (btnCancelarRegistro && formularioRegistro) {
        btnCancelarRegistro.addEventListener("click", function () {
            formularioRegistro.style.display = "none";
        });
    }

    document
        .querySelectorAll('input[name="Celular"]')
        .forEach(function (input) {
            input.addEventListener("input", function () {
                this.value = this.value
                    .replace(/\D/g, "")
                    .slice(0, 9);

                validarCampo(this);
            });
        });

    document
        .querySelectorAll(".usuario-form")
        .forEach(function (form) {
            form.addEventListener("submit", function (event) {

                let formularioValido = true;

                form
                    .querySelectorAll(".form-control, .form-select")
                    .forEach(function (campo) {
                        campo.classList.remove("is-invalid");
                        campo.classList.remove("is-valid");
                    });

                form
                    .querySelectorAll("[required]")
                    .forEach(function (campo) {
                        if (!campo.value.trim()) {
                            campo.classList.add("is-invalid");
                            formularioValido = false;
                        }
                    });

                const usuario =
                    form.querySelector('input[name="UsuarioAcceso"]');

                if (usuario) {
                    const valor = usuario.value.trim();

                    if (valor.length < 4 || valor.length > 50) {
                        usuario.classList.add("is-invalid");
                        formularioValido = false;
                    }
                }

                const correo =
                    form.querySelector('input[name="Correo"]');

                if (correo) {
                    const valor = correo.value.trim();

                    if (!emailRegex.test(valor)) {
                        correo.classList.add("is-invalid");
                        formularioValido = false;
                    }
                }

                const celular =
                    form.querySelector('input[name="Celular"]');

                if (celular && celular.value.trim() !== "") {
                    if (!/^9\d{8}$/.test(celular.value.trim())) {
                        celular.classList.add("is-invalid");
                        formularioValido = false;
                    }
                }

                const password =
                    form.querySelector('input[name="Contrasenia"]');

                if (password) {
                    if (!passwordRegex.test(password.value)) {
                        password.classList.add("is-invalid");
                        formularioValido = false;
                    }
                }

                const rol =
                    form.querySelector('select[name="IdRol"]');

                if (rol && !rol.value) {
                    rol.classList.add("is-invalid");
                    formularioValido = false;
                }

                const zona =
                    form.querySelector('select[name="IdZona"]');

                if (zona && !zona.value) {
                    zona.classList.add("is-invalid");
                    formularioValido = false;
                }

                if (!formularioValido) {
                    event.preventDefault();
                    event.stopPropagation();

                    const primerError =
                        form.querySelector(".is-invalid");

                    if (primerError) {
                        primerError.focus();
                    }
                }
            });
        });

    document
        .querySelectorAll(".usuario-form input, .usuario-form select")
        .forEach(function (campo) {

            campo.addEventListener("input", function () {
                validarCampo(this);
            });

            campo.addEventListener("change", function () {
                validarCampo(this);
            });
        });

    function validarCampo(campo) {

        const valor = campo.value.trim();

        if (!valor) {
            campo.classList.remove("is-valid");

            if (campo.hasAttribute("required")) {
                campo.classList.add("is-invalid");
            } else {
                campo.classList.remove("is-invalid");
            }

            return;
        }

        if (campo.name === "UsuarioAcceso") {
            actualizarEstado(
                campo,
                valor.length >= 4 &&
                valor.length <= 50
            );

            return;
        }

        if (campo.name === "Correo") {
            actualizarEstado(
                campo,
                emailRegex.test(valor)
            );

            return;
        }

        if (campo.name === "Celular") {
            actualizarEstado(
                campo,
                /^9\d{8}$/.test(valor)
            );

            return;
        }

        if (campo.name === "Contrasenia") {
            actualizarEstado(
                campo,
                passwordRegex.test(campo.value)
            );

            return;
        }

        if (campo.name === "IdRol") {
            actualizarEstado(
                campo,
                valor !== ""
            );

            return;
        }

        if (campo.name === "IdZona") {
            actualizarEstado(
                campo,
                valor !== ""
            );

            return;
        }

        actualizarEstado(campo, true);
    }

    function actualizarEstado(campo, valido) {

        campo.classList.remove("is-invalid");
        campo.classList.remove("is-valid");

        if (valido) {
            campo.classList.add("is-valid");
        } else {
            campo.classList.add("is-invalid");
        }
    }

    const buscador =
        document.getElementById("buscadorUsuarios");

    const tabla =
        document.getElementById("tablaUsuarios");

    const limpiarBuscador =
        document.getElementById("limpiarBuscador");

    if (buscador && tabla) {

        buscador.addEventListener("input", function () {

            const texto =
                this.value.toLowerCase().trim();

            const filas =
                tabla.querySelectorAll(
                    "tbody tr.usuario-row"
                );

            filas.forEach(function (fila) {

                const contenido =
                    fila.textContent.toLowerCase();

                const zona =
                    fila.querySelector(".usuario-zona");

                const codigoZona =
                    zona
                        ? zona.textContent.toLowerCase().trim()
                        : "";

                const coincide =
                    contenido.includes(texto) ||
                    codigoZona.includes(texto);

                fila.style.display =
                    coincide
                        ? ""
                        : "none";
            });

            if (limpiarBuscador) {
                limpiarBuscador.style.display =
                    texto.length > 0
                        ? "flex"
                        : "none";
            }
        });
    }

    if (limpiarBuscador && buscador) {

        limpiarBuscador.addEventListener("click", function () {

            buscador.value = "";

            buscador.dispatchEvent(
                new Event("input")
            );

            buscador.focus();
        });
    }

    document
        .querySelectorAll(".btn-confirmar-estado")
        .forEach(function (boton) {

            boton.addEventListener("click", function () {

                const accion =
                    boton.dataset.accion;

                const idUsuario =
                    boton.dataset.id;

                const formulario =
                    boton.closest(".form-estado-usuario");

                if (!formulario) {
                    return;
                }

                const esActivar =
                    accion === "activar";

                const titulo =
                    esActivar
                        ? "¿Activar usuario?"
                        : "¿Desactivar usuario?";

                const texto =
                    esActivar
                        ? `El usuario #${idUsuario} podrá volver a ingresar al sistema.`
                        : `El usuario #${idUsuario} ya no podrá ingresar al sistema.`;

                const icono =
                    esActivar
                        ? "question"
                        : "warning";

                const textoConfirmar =
                    esActivar
                        ? "Sí, activar"
                        : "Sí, desactivar";

                if (typeof Swal === "undefined") {
                    formulario.submit();
                    return;
                }

                Swal.fire({
                    title: titulo,
                    text: texto,
                    icon: icono,
                    showCancelButton: true,
                    confirmButtonText: textoConfirmar,
                    cancelButtonText: "Cancelar",
                    reverseButtons: true,
                    buttonsStyling: true,
                    focusCancel: true
                }).then(function (resultado) {

                    if (resultado.isConfirmed) {
                        formulario.submit();
                    }
                });
            });
        });

    const notificacionUsuario =
        document.getElementById("notificacionUsuario");

    if (
        notificacionUsuario &&
        typeof Swal !== "undefined"
    ) {

        const tipo =
            notificacionUsuario.dataset.tipo;

        const mensaje =
            notificacionUsuario.dataset.mensaje;

        if (tipo && mensaje) {

            Swal.fire({
                icon: tipo,
                title:
                    tipo === "success"
                        ? "Operación realizada"
                        : "Ocurrió un problema",
                text: mensaje,
                confirmButtonText: "Aceptar",
                timer: 3000,
                timerProgressBar: true
            });
        }
    }
});