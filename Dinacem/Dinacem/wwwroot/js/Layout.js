/**
 * DINACEN - Core Layout Management & Interactivity Module
 * Configuración global y soporte UI para la plantilla base de la aplicación.
 */

class LayoutManager {
    constructor() {
        this.init();
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.initActiveNavigation();
            this.initBootstrapComponents();
            this.initSessionKeepAlive();
        });
    }

    /**
     * Asegura el resaltado del ítem de navegación activo 
     * según la URL actual (soporte fallback si Razor no lo resuelve).
     */
    initActiveNavigation() {
        const currentPath = window.location.pathname.toLowerCase();
        const menuItems = document.querySelectorAll('.sidebar-menu a');

        menuItems.forEach(link => {
            const href = link.getAttribute('href');
            if (href && href !== '#' && currentPath.includes(href.toLowerCase())) {
                // Elimina estado previo si existiera
                menuItems.forEach(item => item.classList.remove('active'));
                link.classList.add('active');
            }
        });
    }

    /**
     * Inicializa componentes globales de Bootstrap (Tooltips y Popovers).
     */
    initBootstrapComponents() {
        if (typeof bootstrap !== 'undefined') {
            // Inicializar todos los Tooltips
            const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.forEach(tooltipTriggerEl => {
                new bootstrap.Tooltip(tooltipTriggerEl, {
                    trigger: 'hover'
                });
            });

            // Inicializar Popovers
            const popoverTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="popover"]'));
            popoverTriggerList.forEach(popoverTriggerEl => {
                new bootstrap.Popover(popoverTriggerEl);
            });
        }
    }

    /**
     * Prevención básica de pérdida de sesión activa (ping opcional cada 10 min).
     */
    initSessionKeepAlive() {
        const TEN_MINUTES = 10 * 60 * 1000;
        setInterval(() => {
            // Envío silencioso para mantener vivo el backend sin recargar
            fetch(window.location.origin + '/Home/Index', { method: 'HEAD' })
                .catch(err => console.warn('Keep-alive ping non-critical error:', err));
        }, TEN_MINUTES);
    }
}

// Inicialización de la instancia principal del Layout
const AppLayout = new LayoutManager();

/**
 * Helper UI Utility Object (Disponible para ser invocado desde cualquier vista .cshtml)
 */
window.DinacenUI = {
    /**
     * Muestra una notificación Toast utilizando Bootstrap 5.
     * @param {string} message - Texto del mensaje.
     * @param {string} type - Tipo: 'success' | 'danger' | 'warning' | 'info'
     */
    showToast(message, type = 'info') {
        let toastContainer = document.getElementById('dinacen-toast-container');

        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'dinacen-toast-container';
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '1090';
            document.body.appendChild(toastContainer);
        }

        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : type === 'danger' ? 'bg-danger' : type === 'warning' ? 'bg-warning text-dark' : 'bg-primary';

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0 shadow" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body fw-medium">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        const bsToast = new bootstrap.Toast(toastElement, { delay: 4000 });
        
        bsToast.show();

        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }
};