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
    const matchingRenderStates = new WeakMap();

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

    function formatTemplate(template, ...args) {
        if (!template) {
            return "";
        }

        return args.reduce((result, value, index) => {
            const pattern = new RegExp(`\\{${index}\\}`, "g");
            return result.replace(pattern, value ?? "");
        }, template);
    }

    function getMatchingBoard(form) {
        return form.querySelector("[data-matching-board='true']");
    }

    function getMatchingRenderState(board) {
        let state = matchingRenderStates.get(board);
        if (!state) {
            state = {
                frameId: 0
            };
            matchingRenderStates.set(board, state);
        }

        return state;
    }

    function getMatchingPairs(board) {
        return Array.from(board.querySelectorAll("[data-matching-left-item='true']")).map((item) => {
            const hiddenInput = item.querySelector("[data-matching-hidden='true']");
            const leftText = item.querySelector(".matching-runtime-left-text")?.textContent?.trim() || "";

            return {
                item,
                pairId: item.getAttribute("data-pair-id") || "",
                order: item.getAttribute("data-pair-order") || "",
                leftText,
                hiddenInput,
                stateElement: item.querySelector("[data-matching-left-state='true']"),
                selectedElement: item.querySelector("[data-matching-left-selected='true']"),
                selectedValue: hiddenInput?.value || ""
            };
        });
    }

    function getMatchingChoices(board) {
        return Array.from(board.querySelectorAll("[data-matching-choice='true']"));
    }

    function getMatchingLayout(board) {
        return board.querySelector("[data-matching-layout='true']");
    }

    function findMatchingPair(board, pairId) {
        return getMatchingPairs(board).find((pair) => pair.pairId === pairId) || null;
    }

    function findMatchingPairUsingChoice(board, value) {
        return getMatchingPairs(board).find((pair) => pair.selectedValue === value) || null;
    }

    function renderMatchingSummary(board, pairs) {
        const summaryList = board.querySelector("[data-matching-summary-list='true']");
        if (!summaryList) {
            return;
        }

        const connectedPairs = pairs.filter((pair) => Boolean(pair.selectedValue));
        const connectedTemplate = board.getAttribute("data-matching-connected-template") || "{0}";
        summaryList.innerHTML = "";

        if (!connectedPairs.length) {
            const emptyState = document.createElement("div");
            emptyState.className = "matching-runtime-summary-empty";
            emptyState.textContent = board.getAttribute("data-matching-no-connections") || "";
            summaryList.appendChild(emptyState);
            return;
        }

        connectedPairs.forEach((pair) => {
            const item = document.createElement("div");
            item.className = "matching-runtime-summary-item";

            const order = document.createElement("span");
            order.className = "matching-runtime-summary-order";
            order.textContent = pair.order;

            const copy = document.createElement("div");
            copy.className = "matching-runtime-summary-copy";

            const title = document.createElement("strong");
            title.textContent = pair.leftText;

            const value = document.createElement("span");
            value.textContent = formatTemplate(connectedTemplate, pair.selectedValue);

            copy.appendChild(title);
            copy.appendChild(value);
            item.appendChild(order);
            item.appendChild(copy);
            summaryList.appendChild(item);
        });
    }

    function drawMatchingLinks(board, pairs) {
        const layout = getMatchingLayout(board);
        const svg = board.querySelector("[data-matching-svg='true']");
        if (!layout || !svg) {
            return;
        }

        const layoutRect = layout.getBoundingClientRect();
        const width = Math.max(layoutRect.width, 1);
        const height = Math.max(layoutRect.height, 1);

        svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
        svg.setAttribute("width", `${width}`);
        svg.setAttribute("height", `${height}`);
        svg.innerHTML = "";

        const connectedPairs = pairs.filter((pair) => Boolean(pair.selectedValue));
        if (!connectedPairs.length || window.matchMedia("(max-width: 991.98px)").matches) {
            return;
        }

        connectedPairs.forEach((pair) => {
            const choice = getMatchingChoices(board).find((button) => (button.getAttribute("data-value") || "") === pair.selectedValue);
            if (!choice) {
                return;
            }

            const leftRect = pair.item.getBoundingClientRect();
            const rightRect = choice.getBoundingClientRect();

            const startX = leftRect.right - layoutRect.left - 10;
            const startY = leftRect.top - layoutRect.top + (leftRect.height / 2);
            const endX = rightRect.left - layoutRect.left + 10;
            const endY = rightRect.top - layoutRect.top + (rightRect.height / 2);
            const deltaX = Math.max((endX - startX) * 0.42, 56);

            const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", `M ${startX} ${startY} C ${startX + deltaX} ${startY}, ${endX - deltaX} ${endY}, ${endX} ${endY}`);
            path.setAttribute("class", "matching-runtime-link-path");
            svg.appendChild(path);

            const startDot = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            startDot.setAttribute("cx", `${startX}`);
            startDot.setAttribute("cy", `${startY}`);
            startDot.setAttribute("r", "5");
            startDot.setAttribute("class", "matching-runtime-link-dot-start");
            svg.appendChild(startDot);

            const endDot = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            endDot.setAttribute("cx", `${endX}`);
            endDot.setAttribute("cy", `${endY}`);
            endDot.setAttribute("r", "5");
            endDot.setAttribute("class", "matching-runtime-link-dot-end");
            svg.appendChild(endDot);

            const labelX = startX + ((endX - startX) / 2);
            const labelY = startY + ((endY - startY) / 2) - 12;
            const label = document.createElementNS("http://www.w3.org/2000/svg", "g");
            label.setAttribute("class", "matching-runtime-link-label");

            const labelRect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            labelRect.setAttribute("x", `${labelX - 12}`);
            labelRect.setAttribute("y", `${labelY - 11}`);
            labelRect.setAttribute("rx", "10");
            labelRect.setAttribute("width", "24");
            labelRect.setAttribute("height", "22");
            label.appendChild(labelRect);

            const labelText = document.createElementNS("http://www.w3.org/2000/svg", "text");
            labelText.setAttribute("x", `${labelX}`);
            labelText.setAttribute("y", `${labelY + 4}`);
            labelText.setAttribute("text-anchor", "middle");
            labelText.textContent = pair.order;
            label.appendChild(labelText);

            svg.appendChild(label);
        });
    }

    function scheduleMatchingRender(board, pairs = null) {
        const state = getMatchingRenderState(board);
        if (state.frameId) {
            window.cancelAnimationFrame(state.frameId);
        }

        state.frameId = window.requestAnimationFrame(() => {
            state.frameId = 0;
            drawMatchingLinks(board, pairs || getMatchingPairs(board));
        });
    }

    function syncMatchingBoard(board) {
        const pairs = getMatchingPairs(board);
        const choices = getMatchingChoices(board);
        const activePairId = board.getAttribute("data-active-pair-id") || "";
        const activePair = pairs.find((pair) => pair.pairId === activePairId) || null;
        const waitingText = board.getAttribute("data-matching-waiting-text") || "";
        const activePrompt = board.getAttribute("data-matching-active-prompt") || "";
        const connectedTemplate = board.getAttribute("data-matching-connected-template") || "{0}";
        const activeTemplate = board.getAttribute("data-matching-active-template") || "{0}";
        const choiceUsedTemplate = board.getAttribute("data-matching-choice-used-template") || "{0}";

        pairs.forEach((pair) => {
            const isConnected = Boolean(pair.selectedValue);
            const isActive = activePair?.pairId === pair.pairId;

            pair.item.classList.toggle("matching-runtime-left-item-connected", isConnected);
            pair.item.classList.toggle("matching-runtime-left-item-active", isActive);
            pair.item.setAttribute("aria-pressed", isActive ? "true" : "false");

            if (pair.stateElement) {
                pair.stateElement.textContent = isConnected
                    ? formatTemplate(connectedTemplate, pair.selectedValue)
                    : waitingText;
            }

            if (pair.selectedElement) {
                pair.selectedElement.textContent = pair.selectedValue;
                pair.selectedElement.toggleAttribute("hidden", !isConnected);
            }
        });

        const activeLabel = board.querySelector("[data-matching-active-label='true']");
        if (activeLabel) {
            activeLabel.textContent = activePair
                ? formatTemplate(activeTemplate, activePair.order)
                : activePrompt;
        }

        const clearButton = board.querySelector("[data-matching-clear='true']");
        if (clearButton) {
            clearButton.disabled = !(activePair && activePair.selectedValue);
        }

        const connectedCount = pairs.filter((pair) => Boolean(pair.selectedValue)).length;
        const progress = board.querySelector("[data-matching-progress='true']");
        if (progress) {
            progress.textContent = formatTemplate(
                board.getAttribute("data-matching-summary-template"),
                connectedCount,
                pairs.length
            );
        }

        renderMatchingSummary(board, pairs);

        choices.forEach((choice) => {
            const value = choice.getAttribute("data-value") || "";
            const pairUsingChoice = findMatchingPairUsingChoice(board, value);
            const isUsed = Boolean(pairUsingChoice);
            const isCurrentSelection = activePair?.selectedValue === value;

            choice.classList.toggle("matching-bank-choice-used", isUsed);
            choice.classList.toggle("matching-bank-choice-active-target", Boolean(isCurrentSelection));
            choice.setAttribute("aria-pressed", isCurrentSelection ? "true" : "false");

            const meta = choice.querySelector("[data-matching-choice-meta='true']");
            if (meta) {
                meta.textContent = isUsed
                    ? formatTemplate(choiceUsedTemplate, pairUsingChoice.order)
                    : "";
            }
        });

        scheduleMatchingRender(board, pairs);
    }

    function initializeMatchingBoard(form) {
        const board = getMatchingBoard(form);
        if (!board) {
            return;
        }

        syncMatchingBoard(board);
        window.addEventListener("resize", () => {
            scheduleMatchingRender(board);
        });
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
        initializeMatchingBoard(form);

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

        form.addEventListener("click", (event) => {
            const board = event.target.closest("[data-matching-board='true']");
            if (!board) {
                return;
            }

            const leftItem = event.target.closest("[data-matching-left-item='true']");
            if (leftItem) {
                const pairId = leftItem.getAttribute("data-pair-id") || "";
                const currentActivePairId = board.getAttribute("data-active-pair-id") || "";
                board.setAttribute("data-active-pair-id", currentActivePairId === pairId ? "" : pairId);
                syncMatchingBoard(board);
                return;
            }

            const clearButton = event.target.closest("[data-matching-clear='true']");
            if (clearButton) {
                const activePairId = board.getAttribute("data-active-pair-id") || "";
                const activePair = findMatchingPair(board, activePairId);
                if (!activePair?.hiddenInput || !activePair.selectedValue) {
                    return;
                }

                activePair.hiddenInput.value = "";
                syncMatchingBoard(board);
                activePair.hiddenInput.dispatchEvent(new Event("change", { bubbles: true }));
                return;
            }

            const choiceButton = event.target.closest("[data-matching-choice='true']");
            if (!choiceButton) {
                return;
            }

            const activePairId = board.getAttribute("data-active-pair-id") || "";
            const activePair = findMatchingPair(board, activePairId);
            if (!activePair?.hiddenInput) {
                window.alert(board.getAttribute("data-matching-select-question-first") || "");
                return;
            }

            const choiceValue = choiceButton.getAttribute("data-value") || "";
            const existingPair = findMatchingPairUsingChoice(board, choiceValue);
            if (existingPair && existingPair.pairId !== activePair.pairId) {
                window.alert(board.getAttribute("data-matching-choice-used-error") || "");
                return;
            }

            activePair.hiddenInput.value = choiceValue;
            board.setAttribute("data-active-pair-id", "");
            syncMatchingBoard(board);
            activePair.hiddenInput.dispatchEvent(new Event("change", { bubbles: true }));
        });
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
