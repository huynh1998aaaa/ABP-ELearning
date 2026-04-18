(function () {
    const root = document.querySelector("[data-session-root='true']");
    if (!root) {
        return;
    }

    const submitButton = root.querySelector("[data-session-submit]");
    const timerElement = root.querySelector("[data-session-timer]");
    const navItems = Array.from(root.querySelectorAll("[data-session-nav]"));
    const forms = Array.from(root.querySelectorAll("[data-answer-form='true']"));
    const endTimeValue = root.getAttribute("data-end-time");
    const submitConfirm = root.getAttribute("data-submit-confirm") || "Submit now?";
    const submitUrl = root.getAttribute("data-submit-url");
    const resultUrl = root.getAttribute("data-result-url");
    const essaySavingText = root.getAttribute("data-essay-saving-text") || "Saving...";
    const essaySavedText = root.getAttribute("data-essay-saved-text") || "Saved";
    const essaySaveFailedText = root.getAttribute("data-essay-save-failed-text") || "Save failed";
    const formStates = new WeakMap();

    function getFormState(form) {
        let state = formStates.get(form);
        if (!state) {
            state = {
                timeoutId: 0,
                controller: null
            };
            formStates.set(form, state);
        }

        return state;
    }

    function scrollToQuestion(questionId) {
        const panel = document.getElementById(questionId);
        if (!panel) {
            return;
        }

        panel.scrollIntoView({ behavior: "smooth", block: "start" });
        navItems.forEach((item) => {
            item.classList.toggle("session-nav-current", item.getAttribute("data-question-target") === questionId);
        });
    }

    function setEssayStatus(form, text, mode) {
        const statusElement = form.querySelector("[data-essay-status='true']");
        if (!statusElement) {
            return;
        }

        statusElement.textContent = text;
        statusElement.classList.toggle("essay-status-saving", mode === "saving");
        statusElement.classList.toggle("essay-status-error", mode === "error");
    }

    function countWords(value) {
        if (!value || !value.trim()) {
            return 0;
        }

        return value.trim().split(/\s+/).length;
    }

    function updateEssayCounter(form) {
        const textarea = form.querySelector("[data-essay-input='true']");
        const counter = form.querySelector("[data-essay-counter='true']");
        if (!textarea || !counter) {
            return;
        }

        const maxWords = textarea.getAttribute("data-max-words");
        const currentCount = countWords(textarea.value);
        counter.textContent = maxWords
            ? `${currentCount} / ${maxWords}`
            : String(currentCount);
    }

    async function submitForm(form) {
        const state = getFormState(form);
        if (state.timeoutId) {
            window.clearTimeout(state.timeoutId);
            state.timeoutId = 0;
        }

        if (state.controller) {
            state.controller.abort();
        }

        const controller = new AbortController();
        state.controller = controller;
        const formData = new FormData(form);
        setEssayStatus(form, essaySavingText, "saving");

        try {
            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: formData,
                signal: controller.signal
            });

            const payload = await response.json();
            if (!response.ok) {
                throw new Error(payload?.error?.message || "Unable to save answer.");
            }

            const questionId = form.getAttribute("data-question-id");
            const isAnswered = payload?.data?.isAnswered === true;
            const nav = root.querySelector(`[data-question-id='${questionId}']`);
            if (nav) {
                nav.classList.toggle("session-nav-answered", isAnswered);
                nav.classList.toggle("session-nav-unanswered", !isAnswered);
            }

            setEssayStatus(form, essaySavedText, "saved");
        } catch (error) {
            if (error.name === "AbortError") {
                return;
            }

            setEssayStatus(form, essaySaveFailedText, "error");
            throw error;
        } finally {
            if (state.controller === controller) {
                state.controller = null;
            }
        }
    }

    function scheduleEssaySave(form) {
        const state = getFormState(form);
        if (state.timeoutId) {
            window.clearTimeout(state.timeoutId);
        }

        setEssayStatus(form, essaySavingText, "saving");
        state.timeoutId = window.setTimeout(() => {
            submitForm(form).catch((error) => {
                window.alert(error.message);
            });
        }, 800);
    }

    async function submitSession() {
        const formData = new FormData();
        const tokenInput = root.querySelector("input[name='__RequestVerificationToken']");
        if (tokenInput) {
            formData.append("__RequestVerificationToken", tokenInput.value);
        }

        const response = await fetch(submitUrl, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            body: formData
        });

        const payload = await response.json();
        if (!response.ok) {
            throw new Error(payload?.error?.message || "Unable to submit.");
        }

        window.location.href = payload?.data?.redirectUrl || resultUrl;
    }

    forms.forEach((form) => {
        updateEssayCounter(form);

        form.addEventListener("change", async (event) => {
            if (event.target && event.target.matches("[data-essay-input='true']")) {
                return;
            }

            try {
                await submitForm(form);
            } catch (error) {
                window.alert(error.message);
            }
        });

        const essayInput = form.querySelector("[data-essay-input='true']");
        if (essayInput) {
            essayInput.addEventListener("input", () => {
                updateEssayCounter(form);
                scheduleEssaySave(form);
            });

            essayInput.addEventListener("blur", async () => {
                try {
                    await submitForm(form);
                } catch (error) {
                    window.alert(error.message);
                }
            });
        }
    });

    navItems.forEach((item) => {
        item.addEventListener("click", () => {
            scrollToQuestion(item.getAttribute("data-question-target"));
        });
    });

    if (submitButton) {
        submitButton.addEventListener("click", async () => {
            if (!window.confirm(submitConfirm)) {
                return;
            }

            try {
                await submitSession();
            } catch (error) {
                window.alert(error.message);
            }
        });
    }

    if (timerElement && endTimeValue) {
        const endTime = new Date(endTimeValue);

        const renderTimer = () => {
            const diff = endTime.getTime() - Date.now();
            if (diff <= 0) {
                timerElement.textContent = "00:00";
                submitSession().catch(() => {
                    window.location.href = resultUrl;
                });
                return false;
            }

            const totalSeconds = Math.floor(diff / 1000);
            const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
            const seconds = String(totalSeconds % 60).padStart(2, "0");
            timerElement.textContent = `${minutes}:${seconds}`;
            return true;
        };

        if (renderTimer()) {
            const intervalId = window.setInterval(() => {
                if (!renderTimer()) {
                    window.clearInterval(intervalId);
                }
            }, 1000);
        }
    }
})();
