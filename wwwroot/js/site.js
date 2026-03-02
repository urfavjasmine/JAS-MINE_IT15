// ═══════════════════════════════════════════════
//  JAS-MINE UI/UX Enhancements
// ═══════════════════════════════════════════════

(function () {
    'use strict';

    // ── Loading spinner ──
    const overlay = document.getElementById('loadingOverlay');

    // Show spinner on form submits & navigation links with data-loading
    document.addEventListener('submit', function (e) {
        const form = e.target;
        // Don't show for AJAX forms or forms with data-no-loading
        if (form.dataset.noLoading || form.dataset.ajax) return;
        if (overlay) overlay.classList.add('active');
    });

    document.addEventListener('click', function (e) {
        const link = e.target.closest('a[data-loading]');
        if (link && !link.dataset.noLoading) {
            if (overlay) overlay.classList.add('active');
        }
    });

    // Hide spinner when page finishes loading (back/forward cache)
    window.addEventListener('pageshow', function () {
        if (overlay) overlay.classList.remove('active');
    });

    // ── Confirm delete modals ──
    // Usage: <button data-confirm-delete="Are you sure?" data-confirm-form="#deleteForm123">
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-confirm-delete]');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        const message = btn.dataset.confirmDelete || 'Are you sure you want to delete this item?';
        const formSelector = btn.dataset.confirmForm;
        const href = btn.getAttribute('href');

        // Use Bootstrap modal if available
        const modalEl = document.getElementById('confirmDeleteModal');
        if (modalEl && typeof bootstrap !== 'undefined') {
            const modal = new bootstrap.Modal(modalEl);
            document.getElementById('confirmDeleteMessage').textContent = message;

            const confirmBtn = document.getElementById('confirmDeleteBtn');
            // Clone to remove old listeners
            const newBtn = confirmBtn.cloneNode(true);
            confirmBtn.parentNode.replaceChild(newBtn, confirmBtn);

            newBtn.addEventListener('click', function () {
                modal.hide();
                if (formSelector) {
                    const form = document.querySelector(formSelector);
                    if (form) form.submit();
                } else if (href) {
                    window.location.href = href;
                } else if (btn.form) {
                    btn.form.submit();
                }
            });

            modal.show();
        } else {
            // Fallback to native confirm
            if (confirm(message)) {
                if (formSelector) {
                    const form = document.querySelector(formSelector);
                    if (form) form.submit();
                } else if (href) {
                    window.location.href = href;
                } else if (btn.form) {
                    btn.form.submit();
                }
            }
        }
    });

    // ── Dark mode toggle ──
    const THEME_KEY = 'jm_theme';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        // Update toggle button icon
        const toggleIcon = document.getElementById('darkModeIcon');
        if (toggleIcon) {
            toggleIcon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon';
        }
    }

    // Load saved theme
    const saved = localStorage.getItem(THEME_KEY) || 'light';
    applyTheme(saved);

    // Toggle handler
    window.toggleDarkMode = function () {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem(THEME_KEY, next);
        applyTheme(next);
    };

    // ── Auto-dismiss alerts after 5s ──
    document.querySelectorAll('.alert-dismissible[data-auto-dismiss]').forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });

})();
