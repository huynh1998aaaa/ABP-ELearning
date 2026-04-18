(function ($) {
    const modalSelector = '#adminAjaxModal';

    function getModalElements() {
        const modalElement = document.querySelector(modalSelector);
        if (!modalElement) {
            return null;
        }

        return {
            element: modalElement,
            title: modalElement.querySelector('[data-admin-modal-title]'),
            body: modalElement.querySelector('[data-admin-modal-body]'),
            instance: bootstrap.Modal.getOrCreateInstance(modalElement)
        };
    }

    function parseValidation(container) {
        if ($ && $.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($(container));
        }

        document.dispatchEvent(new CustomEvent('admin:contentLoaded', {
            detail: {
                container: container
            }
        }));
    }

    function notifySuccess(message) {
        if (window.abp && abp.notify) {
            abp.notify.success(message || 'Saved successfully.');
        }
    }

    function notifyError(message) {
        if (window.abp && abp.notify) {
            abp.notify.error(message || 'The operation failed.');
        }
    }

    function refreshTarget(selector, explicitUrl) {
        if (!selector) {
            return Promise.resolve();
        }

        const target = document.querySelector(selector);
        if (!target) {
            return Promise.resolve();
        }

        const refreshUrl = explicitUrl || target.getAttribute('data-admin-refresh-url');
        if (!refreshUrl) {
            return Promise.resolve();
        }

        return fetch(refreshUrl, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => response.text())
            .then(html => {
                target.innerHTML = html;
                parseValidation(target);
            });
    }

    function setBusy(element, isBusy) {
        if (!element) {
            return;
        }

        element.toggleAttribute('disabled', isBusy);
    }

    function populateBulkSelection(form) {
        if (form.getAttribute('data-admin-bulk-form') !== 'true') {
            return true;
        }

        const selector = form.getAttribute('data-admin-selection-selector');
        const fieldName = form.getAttribute('data-admin-selection-name') || 'SelectedIds';
        const container = form.querySelector('[data-admin-selection-container]');
        if (!selector || !container) {
            return true;
        }

        container.innerHTML = '';
        const selectedValues = Array.from(document.querySelectorAll(selector))
            .map(item => item.value)
            .filter(Boolean);

        if (selectedValues.length === 0) {
            notifyError(form.getAttribute('data-admin-no-selection-message'));
            return false;
        }

        selectedValues.forEach(value => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = fieldName;
            input.value = value;
            container.appendChild(input);
        });

        return true;
    }

    document.addEventListener('change', function (event) {
        const selectAll = event.target.closest('[data-admin-select-all]');
        if (!selectAll) {
            return;
        }

        const selector = selectAll.getAttribute('data-admin-select-all');
        if (!selector) {
            return;
        }

        document.querySelectorAll(selector).forEach(item => {
            item.checked = selectAll.checked;
        });
    });

    document.addEventListener('click', function (event) {
        const opener = event.target.closest('[data-admin-modal-url]');
        if (!opener) {
            return;
        }

        event.preventDefault();

        const modal = getModalElements();
        if (!modal) {
            window.location.href = opener.getAttribute('href') || opener.getAttribute('data-admin-modal-url');
            return;
        }

        const url = opener.getAttribute('data-admin-modal-url');
        const title = opener.getAttribute('data-admin-modal-title') || '';
        const size = opener.getAttribute('data-admin-modal-size') || 'lg';
        const dialog = modal.element.querySelector('.modal-dialog');

        dialog.classList.remove('modal-sm', 'modal-lg', 'modal-xl');
        dialog.classList.add('modal-' + size);
        modal.title.textContent = title;
        modal.body.innerHTML = '<div class="text-muted">Loading...</div>';
        modal.instance.show();

        fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => response.text())
            .then(html => {
                modal.body.innerHTML = html;
                parseValidation(modal.body);
            })
            .catch(() => {
                modal.body.innerHTML = '<div class="alert alert-danger mb-0">Unable to load the form.</div>';
            });
    });

    document.addEventListener('submit', function (event) {
        const form = event.target.closest('form[data-admin-ajax-form="true"]');
        if (!form) {
            return;
        }

        if (event.defaultPrevented) {
            return;
        }

        event.preventDefault();

        if (!populateBulkSelection(form)) {
            return;
        }

        const confirmMessage = form.getAttribute('data-admin-confirm-message');
        if (confirmMessage && !window.confirm(confirmMessage)) {
            return;
        }

        if ($ && $(form).valid && !$(form).valid()) {
            return;
        }

        const submitter = event.submitter;
        setBusy(submitter, true);

        fetch(form.action, {
            method: form.method || 'POST',
            body: new FormData(form),
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(async response => {
                const contentType = response.headers.get('content-type') || '';
                if (contentType.includes('application/json')) {
                    return {
                        ok: response.ok,
                        json: await response.json()
                    };
                }

                return {
                    ok: response.ok,
                    html: await response.text()
                };
            })
            .then(result => {
                if (result.json && result.json.success) {
                    const modal = getModalElements();
                    const refreshTargetSelector = form.getAttribute('data-admin-refresh-target');
                    const refreshUrl = form.getAttribute('data-admin-refresh-url');

                    if (modal && modal.element.contains(form)) {
                        modal.instance.hide();
                    }

                    return refreshTarget(refreshTargetSelector, refreshUrl)
                        .then(() => notifySuccess(result.json.message || form.getAttribute('data-admin-success-message')));
                }

                if (result.json && result.json.error) {
                    notifyError(result.json.error.message);
                    return;
                }

                if (result.html) {
                    const modal = getModalElements();
                    if (modal && modal.element.contains(form)) {
                        modal.body.innerHTML = result.html;
                        parseValidation(modal.body);
                        return;
                    }

                    document.open();
                    document.write(result.html);
                    document.close();
                    return;
                }

                notifyError();
            })
            .catch(() => notifyError())
            .finally(() => setBusy(submitter, false));
    });
})(window.jQuery);
