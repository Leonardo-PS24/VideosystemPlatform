// ===== VIDEOSYSTEM PLATFORM - SITE.JS =====
// Versione: 2.0.0
// Vanilla JavaScript - No jQuery required

document.addEventListener('DOMContentLoaded', function() {
    console.log('%c🟢 Videosystem Platform', 'color: #00945E; font-size: 20px; font-weight: bold;');
    console.log('%cv2.0.0 - © 2024 Videosystem S.r.l.', 'color: #6c757d; font-size: 12px;');

    // Inizializza tutti i componenti
    initAlerts();
    initTooltips();
    initPopovers();
    initModals();
    initFormValidation();
});

// ===== AUTO-DISMISS ALERTS =====
function initAlerts() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });
}

// ===== BOOTSTRAP TOOLTIPS =====
function initTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// ===== BOOTSTRAP POPOVERS =====
function initPopovers() {
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
}

// ===== MODALS =====
function initModals() {
    // Nessuna inizializzazione necessaria, Bootstrap gestisce automaticamente
}

// ===== FORM VALIDATION =====
function initFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');

    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
}

// ===== UTILITY FUNCTIONS =====

// Conferma azione
function confirmAction(message) {
    return confirm(message || 'Sei sicuro di voler procedere?');
}

// Mostra toast notification
function showToast(message, type = 'success') {
    // Colori: success, danger, warning, info
    let toastContainer = document.querySelector('.toast-container');

    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }

    const toastId = 'toast-' + Date.now();
    const bgClass = {
        'success': 'bg-success',
        'danger': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    }[type] || 'bg-primary';

    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
    toast.show();

    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

// Formatta data
function formatDate(date, format = 'dd/MM/yyyy') {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();

    return format
        .replace('dd', day)
        .replace('MM', month)
        .replace('yyyy', year);
}

// Formatta ora
function formatTime(date) {
    const d = new Date(date);
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
}

// Formatta data e ora
function formatDateTime(date) {
    return `${formatDate(date)} ${formatTime(date)}`;
}

// Debounce function per search
function debounce(func, wait = 300) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ===== ESPORTA PER USO GLOBALE =====
window.VideosystemPlatform = {
    showToast,
    confirmAction,
    formatDate,
    formatTime,
    formatDateTime,
    debounce
};
