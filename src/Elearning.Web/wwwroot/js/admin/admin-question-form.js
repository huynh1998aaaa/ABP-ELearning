(function () {
    function setSectionEnabled(section, enabled) {
        section.querySelectorAll('input, select, textarea').forEach(control => {
            control.disabled = !enabled;
        });
    }

    function syncAnswerSections(root) {
        const container = root || document;
        const selects = container.querySelectorAll('[data-question-type-select]');

        selects.forEach(select => {
            const scope = select.closest('form') || select.closest('[data-admin-modal-body]') || document;
            const sections = scope.querySelectorAll('[data-answer-section]');
            const inputKind = select.selectedOptions[0]?.dataset.inputKind;

            sections.forEach(section => {
                const allowed = (section.dataset.answerSection || '').split(' ');
                const isActive = allowed.includes(inputKind);
                section.classList.toggle('d-none', !isActive);
                setSectionEnabled(section, isActive);
            });
        });
    }

    function setFullPageSubmitBusy(form, event) {
        if (form.dataset.adminAjaxForm === 'true') {
            return;
        }

        if (form.checkValidity && !form.checkValidity()) {
            return;
        }

        const submitter = event.submitter && event.submitter.matches('[data-question-submit-button]')
            ? event.submitter
            : form.querySelector('[data-question-submit-button]');

        if (!submitter) {
            return;
        }

        submitter.dataset.originalText = submitter.textContent;
        submitter.textContent = form.dataset.questionSavingText || submitter.textContent;
        submitter.disabled = true;
    }

    document.addEventListener('change', event => {
        if (event.target.matches('[data-question-type-select]')) {
            syncAnswerSections(event.target.closest('form') || document);
        }
    });

    document.addEventListener('submit', event => {
        const form = event.target.closest('form[data-question-form="true"]');

        if (!form) {
            return;
        }

        syncAnswerSections(form);
        setFullPageSubmitBusy(form, event);
    });

    document.addEventListener('DOMContentLoaded', () => syncAnswerSections(document));
    document.addEventListener('admin:contentLoaded', event => syncAnswerSections(event.detail?.container || document));
})();
