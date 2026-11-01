// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Muestra una alerta con SweetAlert2 (si la librería no cargó, usa alert normal)
function showAlert(icon, title, message) {
    if (typeof Swal === 'undefined') {
        alert(title + '\n' + message);
        return;
    }
    Swal.fire({
        icon: icon,
        title: title,
        text: message,
        confirmButtonText: 'Entendido',
        confirmButtonColor: '#0d6efd'
    });
}

// Alerta enviada por el servidor (errores de la API, país no encontrado, etc.)
(function () {
    const el = document.getElementById('serverAlert');
    if (el) {
        showAlert(el.dataset.icon, el.dataset.title, el.dataset.message);
    }
})();

// Validación del buscador en el navegador
(function () {
    const form = document.getElementById('searchForm');
    const input = document.getElementById('searchInput');
    if (!form || !input) return;

    const MAX_LENGTH = 100;
    const COUNTRY_REGEX = /^[\p{L}\p{M}][\p{L}\p{M}\s.'’-]*$/u;

    function validate(value) {
        if (value === '') return null; // vacío = mostrar todos los sismos
        if (value.length > MAX_LENGTH)
            return `El nombre no puede superar los ${MAX_LENGTH} caracteres.`;
        if (!COUNTRY_REGEX.test(value))
            return 'Solo se permiten letras, espacios, puntos, apóstrofes y guiones. No uses números ni símbolos.';
        return null;
    }

    form.addEventListener('submit', function (e) {
        input.value = input.value.trim().replace(/\s+/g, ' ');
        const error = validate(input.value);
        if (error) {
            e.preventDefault();
            input.classList.add('is-invalid');
            showAlert('warning', 'Búsqueda no válida', error);
        }
    });

    input.addEventListener('input', function () {
        input.classList.remove('is-invalid');
    });
})();

// Write your JavaScript code.
