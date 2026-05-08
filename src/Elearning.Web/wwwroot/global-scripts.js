(function () {
    function isClientRuntimePage() {
        return document.querySelector('.client-shell, .session-shell, .result-shell') !== null;
    }

    function setupPremiumContactModal() {
        var modal = document.querySelector('[data-premium-contact-modal]');
        if (!modal) {
            return;
        }

        var openTriggers = document.querySelectorAll('[data-premium-contact-open]');
        var closeTriggers = modal.querySelectorAll('[data-premium-contact-close]');
        var avatarImage = modal.querySelector('[data-premium-avatar-image]');
        var avatarFallback = modal.querySelector('[data-premium-avatar-fallback]');
        var avatarShell = avatarFallback ? avatarFallback.parentElement : null;
        var qrImage = modal.querySelector('[data-premium-qr-image]');
        var qrFallback = modal.querySelector('[data-premium-qr-fallback]');

        function openModal() {
            modal.hidden = false;
            modal.setAttribute('aria-hidden', 'false');
            document.body.classList.add('premium-contact-modal-open');
        }

        function closeModal() {
            modal.hidden = true;
            modal.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('premium-contact-modal-open');
        }

        openTriggers.forEach(function (trigger) {
            trigger.addEventListener('click', function (event) {
                event.preventDefault();
                openModal();
            });
        });

        closeTriggers.forEach(function (trigger) {
            trigger.addEventListener('click', function () {
                closeModal();
            });
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !modal.hidden) {
                closeModal();
            }
        });

        if (avatarImage && avatarShell && avatarFallback) {
            avatarImage.addEventListener('error', function () {
                avatarShell.classList.add('avatar-fallback-active');
            });
        }

        if (qrImage && qrFallback) {
            qrImage.addEventListener('error', function () {
                qrImage.hidden = true;
                qrFallback.hidden = false;
            });
        }
    }

    function applyClientMode() {
        if (!isClientRuntimePage()) {
            return;
        }

        document.body.classList.add('elearning-client-mode');

        var sidebarSelectors = [
            'aside',
            '.lpx-sidebar-container',
            '.lpx-menu-container',
            '.lpx-main-menu-container',
            '.lpx-sider',
            '.lpx-layout-sider',
            '.lpx-mobile-navbar',
            '.lpx-nav-menu'
        ];

        document.querySelectorAll(sidebarSelectors.join(',')).forEach(function (element) {
            if (element.closest('.client-shell, .session-shell, .result-shell')) {
                return;
            }

            element.style.display = 'none';
            element.setAttribute('aria-hidden', 'true');
        });

        var contentSelectors = [
            '.lpx-content-container',
            '.lpx-content',
            '.lpx-page-container',
            'main'
        ];

        document.querySelectorAll(contentSelectors.join(',')).forEach(function (element) {
            element.style.marginLeft = '0';
            element.style.paddingLeft = '0';
            element.style.width = '100%';
            element.style.maxWidth = '100%';
        });

        setupPremiumContactModal();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyClientMode);
    } else {
        applyClientMode();
    }
})();
