// ============================================================
// INDEX PRINCIPAL - DASHBOARD ADMINISTRADOR DINACEN
// ============================================================

document.addEventListener("DOMContentLoaded", function () {

    // ============================================================
    // INICIALIZACIÓN
    // ============================================================

    inicializarDashboard();

});


// ============================================================
// FUNCIÓN PRINCIPAL
// ============================================================

function inicializarDashboard() {

    inicializarGraficoEstados();
    inicializarGraficoMensual();
    inicializarAnimaciones();
    inicializarAccesosRapidos();

}


// ============================================================
// GRÁFICO - ESTADO DE SOLICITUDES
// ============================================================

function inicializarGraficoEstados() {

    const canvas = document.getElementById("estadoSolicitudesChart");

    if (!canvas || typeof Chart === "undefined") {
        return;
    }

    const pendientes =
        parseInt(canvas.dataset.pendientes || 0);

    const aprobadas =
        parseInt(canvas.dataset.aprobadas || 0);

    const rechazadas =
        parseInt(canvas.dataset.rechazadas || 0);

    const finalizadas =
        parseInt(canvas.dataset.finalizadas || 0);


    new Chart(canvas, {

        type: "doughnut",

        data: {

            labels: [
                "Pendientes",
                "Aprobadas",
                "Rechazadas",
                "Finalizadas"
            ],

            datasets: [

                {
                    data: [
                        pendientes,
                        aprobadas,
                        rechazadas,
                        finalizadas
                    ],

                    backgroundColor: [
                        "#f59e0b",
                        "#22c55e",
                        "#ef4444",
                        "#2563eb"
                    ],

                    borderColor: "#ffffff",

                    borderWidth: 3,

                    hoverOffset: 8
                }

            ]

        },

        options: {

            responsive: true,

            maintainAspectRatio: false,

            cutout: "68%",

            animation: {

                animateRotate: true,

                animateScale: true,

                duration: 1100,

                easing: "easeOutQuart"
            },

            plugins: {

                legend: {

                    position: "bottom",

                    labels: {

                        padding: 18,

                        usePointStyle: true,

                        pointStyle: "circle",

                        font: {
                            family: "Segoe UI",
                            size: 11,
                            weight: "600"
                        },

                        color: "#475569"
                    }

                },

                tooltip: {

                    backgroundColor: "#0f172a",

                    titleColor: "#ffffff",

                    bodyColor: "#e2e8f0",

                    padding: 12,

                    cornerRadius: 8,

                    displayColors: true,

                    callbacks: {

                        label: function (context) {

                            const valor =
                                context.raw ?? 0;

                            return " " +
                                context.label +
                                ": " +
                                valor;

                        }

                    }

                }

            }

        }

    });

}


// ============================================================
// GRÁFICO - COMPORTAMIENTO MENSUAL
// ============================================================

function inicializarGraficoMensual() {

    const canvas =
        document.getElementById("montosMensualesChart");

    if (!canvas || typeof Chart === "undefined") {
        return;
    }


    let labels = [];

    let valores = [];


    try {

        labels =
            JSON.parse(
                canvas.dataset.labels || "[]"
            );

        valores =
            JSON.parse(
                canvas.dataset.valores || "[]"
            );

    }
    catch (error) {

        console.error(
            "Error al leer los datos del gráfico mensual:",
            error
        );

        return;
    }


    new Chart(canvas, {

        type: "bar",

        data: {

            labels: labels,

            datasets: [

                {

                    label: "Monto solicitado",

                    data: valores,

                    backgroundColor: "rgba(12, 74, 138, 0.82)",

                    hoverBackgroundColor: "#0C4A8A",

                    borderColor: "#0C4A8A",

                    borderWidth: 1,

                    borderRadius: 8,

                    borderSkipped: false,

                    maxBarThickness: 55
                }

            ]

        },


        options: {

            responsive: true,

            maintainAspectRatio: false,

            animation: {

                duration: 1000,

                easing: "easeOutQuart"
            },


            interaction: {

                intersect: false,

                mode: "index"
            },


            plugins: {

                legend: {

                    display: false
                },


                tooltip: {

                    backgroundColor: "#0f172a",

                    titleColor: "#ffffff",

                    bodyColor: "#e2e8f0",

                    padding: 12,

                    cornerRadius: 8,

                    callbacks: {

                        label: function (context) {

                            const valor =
                                Number(context.raw || 0);

                            return " Monto: S/ " +
                                valor.toLocaleString(
                                    "es-PE",
                                    {
                                        minimumFractionDigits: 2,
                                        maximumFractionDigits: 2
                                    }
                                );

                        }

                    }

                }

            },


            scales: {

                y: {

                    beginAtZero: true,

                    border: {
                        display: false
                    },

                    grid: {

                        color:
                            "rgba(148, 163, 184, 0.14)",

                        drawTicks: false
                    },

                    ticks: {

                        padding: 8,

                        color: "#64748b",

                        font: {

                            family: "Segoe UI",

                            size: 11
                        },

                        callback: function (value) {

                            return "S/ " +
                                Number(value).toLocaleString(
                                    "es-PE",
                                    {
                                        maximumFractionDigits: 0
                                    }
                                );

                        }

                    }

                },


                x: {

                    border: {
                        display: false
                    },

                    grid: {

                        display: false
                    },

                    ticks: {

                        color: "#64748b",

                        font: {

                            family: "Segoe UI",

                            size: 11,

                            weight: "600"
                        }

                    }

                }

            }

        }

    });

}


// ============================================================
// ANIMACIONES DE ENTRADA
// ============================================================

function inicializarAnimaciones() {

    const elementos = document.querySelectorAll(
        ".executive-card, " +
        ".dashboard-panel, " +
        ".quick-action"
    );


    elementos.forEach(function (elemento, index) {

        elemento.style.opacity = "0";

        elemento.style.transform =
            "translateY(12px)";


        elemento.style.transition =
            "opacity .45s ease, transform .45s ease";


        setTimeout(function () {

            elemento.style.opacity = "1";

            elemento.style.transform =
                "translateY(0)";

        }, 80 + (index * 45));

    });

}


// ============================================================
// ACCESOS RÁPIDOS
// ============================================================

function inicializarAccesosRapidos() {

    const acciones =
        document.querySelectorAll(".quick-action");


    acciones.forEach(function (accion) {

        accion.addEventListener(
            "mouseenter",
            function () {

                const icono =
                    accion.querySelector(
                        ".quick-action-icon"
                    );

                if (icono) {

                    icono.style.transform =
                        "scale(1.08)";

                }

            }
        );


        accion.addEventListener(
            "mouseleave",
            function () {

                const icono =
                    accion.querySelector(
                        ".quick-action-icon"
                    );

                if (icono) {

                    icono.style.transform =
                        "scale(1)";

                }

            }
        );

    });

}


// ============================================================
// ANIMACIÓN DE NÚMEROS
// ============================================================

function animarNumeros() {

    const elementos =
        document.querySelectorAll(
            ".executive-number"
        );


    elementos.forEach(function (elemento) {

        const texto =
            elemento.textContent.trim();

        const numero =
            parseFloat(
                texto.replace(
                    /[^0-9.-]/g,
                    ""
                )
            );


        if (isNaN(numero)) {
            return;
        }


        const esDecimal =
            texto.includes(".");

        const duracion = 900;

        const inicio = performance.now();


        function actualizar(tiempo) {

            const progreso =
                Math.min(
                    (tiempo - inicio) / duracion,
                    1
                );


            const suavizado =
                1 -
                Math.pow(
                    1 - progreso,
                    3
                );


            const valor =
                numero * suavizado;


            elemento.textContent =
                esDecimal
                    ? valor.toLocaleString(
                        "es-PE",
                        {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 2
                        }
                    )
                    : Math.round(valor).toLocaleString(
                        "es-PE"
                    );


            if (progreso < 1) {

                requestAnimationFrame(
                    actualizar
                );

            }

        }


        requestAnimationFrame(
            actualizar
        );

    });

}


// ============================================================
// DETECTAR PREFERENCIA DE MOVIMIENTO
// ============================================================

const reducirMovimiento =
    window.matchMedia(
        "(prefers-reduced-motion: reduce)"
    );


if (!reducirMovimiento.matches) {

    window.addEventListener(
        "load",
        function () {

            setTimeout(
                animarNumeros,
                250
            );

        }
    );

}


// ============================================================
// PROTECCIÓN ANTE CAMBIO DE TAMAÑO
// ============================================================

window.addEventListener(
    "resize",
    function () {

        if (typeof Chart !== "undefined") {

            Chart.instances.forEach(
                function (chart) {

                    chart.resize();

                }
            );

        }

    }
);