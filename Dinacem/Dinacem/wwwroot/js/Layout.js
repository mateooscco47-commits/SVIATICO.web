/**
 * =========================================================
 * DINACEN - Layout Management
 * =========================================================
 *
 * Control global del:
 * - Sidebar
 * - Menú móvil
 * - Overlay
 * - Scroll del body
 * - Redimensionamiento
 *
 * Compatible con:
 * - Layout Empleado
 * - Layout Administrador
 * =========================================================
 */

document.addEventListener('DOMContentLoaded', function () {

    // =====================================================
    // ELEMENTOS
    // =====================================================

    const sidebar = document.getElementById('sidebar');
    const sidebarToggler = document.getElementById('sidebarToggler');
    const sidebarOverlay = document.getElementById('sidebarOverlay');


    // =====================================================
    // VALIDACIÓN
    // =====================================================

    if (!sidebar) {
        return;
    }


    // =====================================================
    // FUNCIÓN PARA SABER SI ESTAMOS EN MÓVIL/TABLET
    // =====================================================

    function isMobile() {
        return window.innerWidth <= 991.98;
    }


    // =====================================================
    // ABRIR SIDEBAR
    // =====================================================

    function openSidebar() {

        if (!sidebar) {
            return;
        }

        sidebar.classList.add('show');

        if (sidebarOverlay) {
            sidebarOverlay.classList.add('show');
        }

        document.body.style.overflow = 'hidden';

        if (sidebarToggler) {
            sidebarToggler.setAttribute('aria-expanded', 'true');
        }
    }


    // =====================================================
    // CERRAR SIDEBAR
    // =====================================================

    function closeSidebar() {

        if (!sidebar) {
            return;
        }

        sidebar.classList.remove('show');

        if (sidebarOverlay) {
            sidebarOverlay.classList.remove('show');
        }

        document.body.style.overflow = '';

        if (sidebarToggler) {
            sidebarToggler.setAttribute('aria-expanded', 'false');
        }
    }


    // =====================================================
    // ALTERNAR SIDEBAR
    // =====================================================

    function toggleSidebar() {

        if (!isMobile()) {
            return;
        }

        if (sidebar.classList.contains('show')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }


    // =====================================================
    // BOTÓN ☰
    // =====================================================

    if (sidebarToggler) {

        sidebarToggler.addEventListener('click', function (event) {

            event.preventDefault();
            event.stopPropagation();

            toggleSidebar();

        });

    }


    // =====================================================
    // OVERLAY
    // =====================================================

    if (sidebarOverlay) {

        sidebarOverlay.addEventListener('click', function () {

            closeSidebar();

        });

    }


    // =====================================================
    // CERRAR AL HACER CLIC EN UNA OPCIÓN DEL MENÚ
    // =====================================================

    const menuLinks = sidebar.querySelectorAll('.sidebar-menu a');

    menuLinks.forEach(function (link) {

        link.addEventListener('click', function () {

            if (isMobile()) {
                closeSidebar();
            }

        });

    });


    // =====================================================
    // REDIMENSIONAMIENTO
    // =====================================================

    window.addEventListener('resize', function () {

        if (!isMobile()) {

            closeSidebar();

        }

    });


    // =====================================================
    // ESCAPE PARA CERRAR
    // =====================================================

    document.addEventListener('keydown', function (event) {

        if (event.key === 'Escape' && isMobile()) {

            if (sidebar.classList.contains('show')) {
                closeSidebar();
            }

        }

    });


    // =====================================================
    // ESTADO INICIAL
    // =====================================================

    if (isMobile()) {

        closeSidebar();

    } else {

        document.body.style.overflow = '';

        if (sidebarToggler) {
            sidebarToggler.setAttribute('aria-expanded', 'false');
        }

    }

});