(function () {
    function syncAnswerSections(root) {
        const container = root || document;
        const selects = container.querySelectorAll('[data-question-type-select]');

        selects.forEach(select => {
            const scope = select.closest('form') || select.closest('[data-admin-modal-body]') || document;
            const sections = scope.querySelectorAll('[data-answer-section]');
            const inputKind = select.selectedOptions[0]?.dataset.inputKind;

            sections.forEach(section => {
                const allowed = (section.dataset.answerSection || '').split(' ');
                section.classList.toggle('d-none', !allowed.includes(inputKind));
            });
        });
    }

    document.addEventListener('change', event => {
        if (event.target.matches('[data-question-type-select]')) {
            syncAnswerSections(event.target.closest('form') || document);
        }
    });

    document.addEventListener('DOMContentLoaded', () => syncAnswerSections(document));
    document.addEventListener('admin:contentLoaded', event => syncAnswerSections(event.detail?.container || document));
})();
