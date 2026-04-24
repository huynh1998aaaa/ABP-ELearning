(function () {
    function isClientRuntimePage() {
        return document.querySelector('.client-shell, .session-shell, .result-shell') !== null;
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
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyClientMode);
    } else {
        applyClientMode();
    }
})();
