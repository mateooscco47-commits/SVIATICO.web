document.addEventListener("DOMContentLoaded", function () {

    /* =========================================================
       REGEX
       ========================================================= */
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;


    /* =========================================================
       OCULTAR NOTIFICACIONES AUTOMÁTICAMENTE (TempData)
       ========================================================= */
    setTimeout(() => {
        const alertas = document.querySelectorAll('.alert');
        alertas.forEach(alerta => {
            if (window.bootstrap && bootstrap.Alert) {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alerta);
                bsAlert.close();
            } else {
                alerta.style.display = 'none';
            }
        });
    }, 4000);


    /* =========================================================
       CELULAR (Solo números, máximo 9 dígitos)
       ========================================================= */
    document.querySelectorAll(".celular-input").forEach(input => {
        input.addEventListener("input", function () {
            this.value = this.value
                .replace(/\D/g, "")
                .slice(0, 9);

            validarCampo(this);
        });
    });


    /* =========================================================
       FORMULARIOS CREAR / EDITAR
       ========================================================= */
    document.querySelectorAll(".usuario-form").forEach(form => {
        form.addEventListener("submit", function (event) {
            let formularioValido = true;

            /* LIMPIAR ESTADOS */
            form.querySelectorAll(".form-control, .form-select").forEach(campo => {
                campo.classList.remove("is-invalid");
                campo.classList.remove("is-valid");
            });

            /* CAMPOS OBLIGATORIOS */
            form.querySelectorAll("[required]").forEach(campo => {
                if (!campo.value.trim()) {
                    campo.classList.add("is-invalid");
                    formularioValido = false;
                }
            });

            /* USUARIO */
            const usuario = form.querySelector('input[name="UsuarioAcceso"]');
            if (usuario) {
                const valor = usuario.value.trim();
                if (valor.length < 4 || valor.length > 50) {
                    usuario.classList.add("is-invalid");
                    formularioValido = false;
                }
            }

            /* CORREO */
            const correo = form.querySelector('input[name="Correo"]');
            if (correo) {
                const valor = correo.value.trim();
                if (!emailRegex.test(valor)) {
                    correo.classList.add("is-invalid");
                    formularioValido = false;
                }
            }

            /* CELULAR */
            const celular = form.querySelector(".celular-input");
            if (celular && celular.value.trim() !== "") {
                if (!/^9\d{8}$/.test(celular.value.trim())) {
                    celular.classList.add("is-invalid");
                    formularioValido = false;
                }
            }

            /* CONTRASEÑA */
            const password = form.querySelector(".password-input");
            if (password) {
                if (!passwordRegex.test(password.value)) {
                    password.classList.add("is-invalid");
                    formularioValido = false;
                }
            }

            /* ROL */
            const rol = form.querySelector('select[name="IdRol"]');
            if (rol && !rol.value) {
                rol.classList.add("is-invalid");
                formularioValido = false;
            }

            /* DETENER ENVÍO SI HAY ERRORES */
            if (!formularioValido) {
                event.preventDefault();
                event.stopPropagation();

                const primerError = form.querySelector(".is-invalid");
                if (primerError) {
                    primerError.focus();
                }
            }
        });
    });


    /* =========================================================
       VALIDACIÓN EN TIEMPO REAL
       ========================================================= */
    document.querySelectorAll(".usuario-form input, .usuario-form select").forEach(campo => {
        campo.addEventListener("input", function () {
            validarCampo(this);
        });

        campo.addEventListener("change", function () {
            validarCampo(this);
        });
    });


    /* =========================================================
       FUNCIÓN VALIDAR CAMPO
       ========================================================= */
    function validarCampo(campo) {
        const valor = campo.value.trim();

        /* CAMPO VACÍO */
        if (!valor) {
            campo.classList.remove("is-valid");

            if (campo.hasAttribute("required")) {
                campo.classList.add("is-invalid");
            } else {
                campo.classList.remove("is-invalid");
            }
            return;
        }

        /* USUARIO */
        if (campo.name === "UsuarioAcceso") {
            const valido = valor.length >= 4 && valor.length <= 50;
            actualizarEstado(campo, valido);
            return;
        }

        /* CORREO */
        if (campo.name === "Correo") {
            actualizarEstado(campo, emailRegex.test(valor));
            return;
        }

        /* CELULAR */
        if (campo.classList.contains("celular-input")) {
            actualizarEstado(campo, /^9\d{8}$/.test(valor));
            return;
        }

        /* CONTRASEÑA */
        if (campo.classList.contains("password-input")) {
            actualizarEstado(campo, passwordRegex.test(campo.value));
            return;
        }

        /* SELECT ROL */
        if (campo.name === "IdRol") {
            actualizarEstado(campo, valor !== "");
            return;
        }

        /* CAMPOS NORMALES */
        actualizarEstado(campo, true);
    }


    /* =========================================================
       ACTUALIZAR ESTADO VISUAL
       ========================================================= */
    function actualizarEstado(campo, valido) {
        campo.classList.remove("is-invalid");
        campo.classList.remove("is-valid");

        if (valido) {
            campo.classList.add("is-valid");
        } else {
            campo.classList.add("is-invalid");
        }
    }


    /* =========================================================
       BUSCADOR EN TIEMPO REAL
       ========================================================= */
    const buscador = document.getElementById("buscadorUsuarios");
    const tabla = document.getElementById("tablaUsuarios");
    const contador = document.getElementById("contadorResultados");
    const mensajeSinResultados = document.getElementById("mensajeSinResultados");
    const btnLimpiar = document.getElementById("btnLimpiarBusqueda");

    if (buscador && tabla) {
        buscador.addEventListener("input", function () {
            filtrarUsuarios(this.value);
        });
    }

    function filtrarUsuarios(texto) {
        const busqueda = texto.toLowerCase().trim();
        const filas = tabla.querySelectorAll("tbody tr.usuario-row");
        let encontrados = 0;

        filas.forEach(fila => {
            const contenido = fila.textContent.toLowerCase().trim();
            const coincide = contenido.includes(busqueda);

            if (coincide) {
                fila.style.display = "";
                encontrados++;
            } else {
                fila.style.display = "none";
            }
        });

        /* CONTADOR */
        if (contador) {
            contador.textContent = `${encontrados} ${encontrados === 1 ? "usuario" : "usuarios"}`;
        }

        /* MENSAJE SIN RESULTADOS */
        if (mensajeSinResultados) {
            if (encontrados === 0 && busqueda !== "") {
                mensajeSinResultados.classList.remove("d-none");
            } else {
                mensajeSinResultados.classList.add("d-none");
            }
        }
    }

    /* BOTÓN LIMPIAR BÚSQUEDA */
    if (btnLimpiar && buscador) {
        btnLimpiar.addEventListener("click", function () {
            buscador.value = "";
            filtrarUsuarios("");
            buscador.focus();
        });
    }


    /* =========================================================
       LIMPIAR VALIDACIONES AL CERRAR MODAL
       ========================================================= */
    document.querySelectorAll(".modal").forEach(modal => {
        modal.addEventListener("hidden.bs.modal", function () {
            const form = modal.querySelector(".usuario-form");
            if (!form) return;

            form.querySelectorAll(".is-invalid, .is-valid").forEach(campo => {
                campo.classList.remove("is-invalid");
                campo.classList.remove("is-valid");
            });
        });
    });

});


/* =========================================================
   FUNCIONES GLOBALES DE CONFIRMACIÓN (ACTIVAR / DESACTIVAR)
   ========================================================= */
window.confirmarDesactivacion = function (e, form) {
    if (!confirm('¿Está seguro de que desea desactivar este usuario?')) {
        e.preventDefault();
        return false;
    }
    return true;
};

window.confirmarActivacion = function (e, form) {
    if (!confirm('¿Está seguro de que desea activar este usuario?')) {
        e.preventDefault();
        return false;
    }
    return true;
};