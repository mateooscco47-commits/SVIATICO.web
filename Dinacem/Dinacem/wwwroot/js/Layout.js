/**
 * DINACEN - Layout Management
 * Control de interactividad para el Sidebar, Overlay móvil y eventos de redimensionamiento.
 */

document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggler = document.getElementById('sidebarToggler');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    /**
     * Alterna la visibilidad del menú lateral y el overlay oscuro en dispositivos móviles.
     */
    function toggleSidebar() {
        if (!sidebar) return;
        
        sidebar.classList.toggle('show');
        if (sidebarOverlay) {
            sidebarOverlay.classList.toggle('show');
        }
        
        // Bloquea o desbloquea el scroll del body cuando el menú está abierto en móviles
        document.body.style.overflow = sidebar.classList.contains('show') ? 'hidden' : '';
    }

    // Event listeners para los botones de apertura y cierre
    if (sidebarToggler) {
        sidebarToggler.addEventListener('click', toggleSidebar);
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', toggleSidebar);
    }

    /**
     * Resetea el estado del menú si la ventana pasa a un tamaño de escritorio (> 991.98px).
     */
    window.addEventListener('resize', function () {
        if (window.innerWidth > 991.98) {
            if (sidebar) sidebar.classList.remove('show');
            if (sidebarOverlay) sidebarOverlay.classList.remove('show');
            document.body.style.overflow = '';
        }
    });

    /**
     * Opcional: Cierra el menú automáticamente al hacer clic en un enlace del menú en pantallas móviles.
     */
    const menuLinks = document.querySelectorAll('.sidebar-menu a');
    menuLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 991.98 && sidebar && sidebar.classList.contains('show')) {
                toggleSidebar();
            }
        });
    });
});