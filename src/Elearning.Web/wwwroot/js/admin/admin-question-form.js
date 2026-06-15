(function () {
    const MIN_OPTION_ROWS = 2;

    function getQuestionInputKind(form) {
        return form.querySelector('[data-question-type-select]')?.selectedOptions[0]?.dataset.inputKind;
    }

    function getOptionRows(form) {
        return Array.from(form.querySelectorAll('[data-question-option-row]'));
    }

    function getOptionTemplate(form) {
        return form.querySelector('[data-question-option-template]');
    }

    function setFieldIdentity(control, index, fieldName) {
        if (!control) {
            return;
        }

        control.name = `Input.Options[${index}].${fieldName}`;
        control.id = `Input_Options_${index}__${fieldName}`;
    }

    function setFieldName(control, index, fieldName) {
        if (!control) {
            return;
        }

        control.name = `Input.Options[${index}].${fieldName}`;
        control.removeAttribute('id');
    }

    function reindexOptionRows(form) {
        getOptionRows(form).forEach((row, index) => {
            const text = row.querySelector('[data-question-option-text]');
            const sort = row.querySelector('[data-question-option-sort]');
            const correct = row.querySelector('[data-question-option-correct]');
            const fallback = row.querySelector('[data-question-option-correct-fallback]');
            const label = correct?.closest('.form-check')?.querySelector('label');

            setFieldIdentity(text, index, 'Text');
            setFieldIdentity(sort, index, 'SortOrder');
            setFieldIdentity(correct, index, 'IsCorrect');
            setFieldName(fallback, index, 'IsCorrect');

            if (sort) {
                sort.value = String(index + 1);
            }

            if (label && correct) {
                label.htmlFor = correct.id;
            }
        });
    }

    function updateRemoveButtons(form) {
        const rows = getOptionRows(form);
        rows.forEach(row => {
            const removeButton = row.querySelector('[data-question-option-remove]');
            if (removeButton) {
                removeButton.disabled = rows.length <= MIN_OPTION_ROWS;
            }
        });
    }

    function syncSingleChoiceCorrectness(form, changedCheckbox) {
        if (getQuestionInputKind(form) !== 'SingleChoice') {
            return;
        }

        const checkedOptions = getOptionRows(form)
            .map(row => row.querySelector('[data-question-option-correct]'))
            .filter(checkbox => checkbox?.checked);

        if (changedCheckbox?.checked) {
            checkedOptions
                .filter(checkbox => checkbox !== changedCheckbox)
                .forEach(checkbox => checkbox.checked = false);
            return;
        }

        checkedOptions
            .slice(1)
            .forEach(checkbox => checkbox.checked = false);
    }

    function syncOptionControls(form, changedCheckbox) {
        reindexOptionRows(form);
        syncSingleChoiceCorrectness(form, changedCheckbox);
        updateRemoveButtons(form);
    }

    function addOptionRow(form) {
        if (!form) {
            return;
        }

        const list = form.querySelector('[data-question-options-list]');
        const template = getOptionTemplate(form);

        if (!list || !template) {
            return;
        }

        const row = template.content.firstElementChild.cloneNode(true);
        list.appendChild(row);
        syncOptionControls(form);
        row.querySelector('[data-question-option-text]')?.focus();
    }

    function clearOptionRow(row) {
        const text = row.querySelector('[data-question-option-text]');
        const correct = row.querySelector('[data-question-option-correct]');

        if (text) {
            text.value = '';
        }

        if (correct) {
            correct.checked = false;
        }
    }

    function removeOptionRow(row) {
        if (!row) {
            return;
        }

        const form = row.closest('form');

        if (!form) {
            return;
        }

        if (getOptionRows(form).length <= MIN_OPTION_ROWS) {
            clearOptionRow(row);
            syncOptionControls(form);
            return;
        }

        row.remove();
        syncOptionControls(form);
    }

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

            syncOptionControls(scope);
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

        if (event.target.matches('[data-question-option-correct]')) {
            const form = event.target.closest('form');
            if (form) {
                syncOptionControls(form, event.target);
            }
        }
    });

    document.addEventListener('click', event => {
        const addButton = event.target.closest('[data-question-option-add]');
        if (addButton) {
            addOptionRow(addButton.closest('form'));
            return;
        }

        const removeButton = event.target.closest('[data-question-option-remove]');
        if (removeButton) {
            removeOptionRow(removeButton.closest('[data-question-option-row]'));
        }
    });

    document.addEventListener('submit', event => {
        const form = event.target.closest('form[data-question-form="true"]');

        if (!form) {
            return;
        }

        syncAnswerSections(form);
        syncOptionControls(form);
        setFullPageSubmitBusy(form, event);
    });

    document.addEventListener('DOMContentLoaded', () => syncAnswerSections(document));
    document.addEventListener('admin:contentLoaded', event => syncAnswerSections(event.detail?.container || document));
})();
