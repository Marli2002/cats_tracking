document.addEventListener('DOMContentLoaded', function () {
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarContent = document.querySelector('#navbarContent');

    navbarToggler.addEventListener('click', function () {
        if (navbarContent.classList.contains('show')) {
            navbarContent.classList.remove('show');
        } else {
            navbarContent.classList.add('show');
        }
    });
});

function showToast(headerText, bodyText, autoHide = "true") {


    var toastId = 'toast' + Date.now();


    var toastHtml = `
                                <div class="toast bg-light" role="alert" aria-live="assertive" aria-atomic="true" id="${toastId}" data-bs-animation="true" data-bs-autohide="${autoHide}" data-bs-delay="10000">
                    <div class="toast-header">
                        <strong class="me-auto">${headerText}</strong>
                        <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                    </div>
                    <div class="toast-body">
                        ${bodyText}
                    </div>
                </div>`;


    var toastContainer = document.getElementById('toast-container');
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    var newToast = document.getElementById(toastId);

    var toast = new bootstrap.Toast(newToast);
    toast.show();
}


function goTo(url) {
    document.location.href = url;
}

function addNavLink(href, text) {
    $('#navbarUL').append(`
        <li class="nav-item">
            <a class="nav-link chip-nav" href="${href}">${text}</a>
        </li>
    `);
}

// Register the service worker on page load
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/service-worker.js')
            .then((reg) => console.log('SW registered', reg))
            .catch((err) => console.error('SW registration failed', err));
    });
}
// Optional: show basic online/offline toasts
window.addEventListener('online', () => console.log('Back online'));
window.addEventListener('offline', () => console.log('You are offline'));