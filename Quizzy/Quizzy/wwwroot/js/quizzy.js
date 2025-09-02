(() => {
    "use strict";

    // ---------------- Shared: SignalR connection + helpers ----------------

    /** Create an unstarted SignalR connection to the game hub. */
    function createConnection() {
        return new signalR.HubConnectionBuilder()
            .withUrl("/gamehub")
            .withAutomaticReconnect()
            .build();
    }

    /** Return seconds left of a question given its UTC start and duration. */
    function secondsRemainingFromUtc(startUtc, durationSeconds) {
        if (!startUtc) return 0;
        const startedAt = new Date(startUtc).getTime();
        const elapsedSec = (Date.now() - startedAt) / 1000;
        return Math.max(0, Math.floor(durationSeconds - elapsedSec));
    }

    /** Return whole seconds until a UTC instant. */
    function secondsUntilUtc(targetUtc) {
        if (!targetUtc) return 0;
        const targetMs = new Date(targetUtc).getTime();
        return Math.max(0, Math.floor((targetMs - Date.now()) / 1000));
    }

    // Small DOM helpers
    const $ = (id) => document.getElementById(id);
    const show = (el, visible) => { if (el) el.style.display = visible ? "" : "none"; };
    const setText = (el, text) => { if (el) el.textContent = text ?? ""; };

    /** Clear and append <li> items from a string[] of options with index labels. */
    function fillOptionList(ul, options) {
        if (!ul) return;
        ul.innerHTML = "";
        (options || []).forEach((opt, i) => {
            const li = document.createElement("li");
            li.textContent = `${i}: ${opt}`;
            ul.appendChild(li);
        });
    }

    /** Create a button for a player option. Disabled if already answered. */
    function createAnswerButton(text, disabled, onClick) {
        const btn = document.createElement("button");
        btn.className = "big";
        btn.textContent = text;
        if (disabled) btn.setAttribute("disabled", "true");
        btn.onclick = onClick;
        return btn;
    }

    // A tiny interval manager so we don't accumulate stray timers
    function makeTicker() {
        let id = null;
        return {
            start(fn, ms) {
                this.stop();
                id = setInterval(fn, ms);
            },
            stop() {
                if (id) {
                    clearInterval(id); id = null;
                }
            }
        };
    }

    function clearChildren(element) {
        if (!element) {
            return;
        }

        while (element.firstChild) {
            element.removeChild(element.firstChild);
        }
    }

    function normalizeQuestionDto(rawDto) {
        const normalized = {
            text: "",
            options: [],
            durationSeconds: 20,
            questionStartTimeUtc: null
        };

        if (rawDto == null) {
            return normalized;
        }

        // Text
        if (typeof rawDto.text === "string" && rawDto.text.length > 0) {
            normalized.text = rawDto.text;
        } else if (typeof rawDto.question === "string" && rawDto.question.length > 0) {
            normalized.text = rawDto.question;
        }

        // Answers or Options
        if (Array.isArray(rawDto.answers)) {
            normalized.options = rawDto.answers.map(function mapAnswer(answer) {
                if (answer == null) {
                    return { text: "" };
                }

                if (typeof answer === "string") {
                    return { text: answer };
                }

                return { text: answer.text ?? "" };
            });
        } else if (Array.isArray(rawDto.options)) {
            normalized.options = rawDto.options.map(function mapOption(opt) {
                if (opt == null) {
                    return { text: "" };
                }

                if (typeof opt === "string") {
                    return { text: opt };
                }

                return { text: opt.text ?? "" };
            });
        }

        // Duration
        if (typeof rawDto.durationSeconds === "number" && rawDto.durationSeconds > 0) {
            normalized.durationSeconds = rawDto.durationSeconds;
        }

        // Start time
        const startUtcCandidate = rawDto.questionStartTimeUtc ?? rawDto.startUtc ?? rawDto.startedAtUtc;
        if (startUtcCandidate) {
            normalized.questionStartTimeUtc = startUtcCandidate;
        }

        return normalized;
    }

    (function hostScope() {
        const form = $("hostForm");
        if (!form) return; // not on host legacy page

        // Inputs and labels
        const sessionInput = $("sessionId");
        const sessionLabel = $("sessionLabel");
        const playerCountEl = $("playerCount");
        const statusEl = $("status");
        const timerEl = $("timer");

        // Action buttons / inputs
        const startNowBtn = $("startNowBtn");
        const scheduleBtn = $("scheduleBtn");
        const endBtn = $("endBtn");
        const qText = $("qText");
        const qDur = $("qDur");
        const qIn = $("qIn");
        const qCorrect = $("qCorrect");
        const qOpts = Array.from(document.querySelectorAll(".opt"));

        // Sections
        const questionArea = $("questionArea");
        const liveQ = $("liveQ");
        const liveOptions = $("liveOptions");
        const resultsArea = $("resultsArea");
        const upcomingArea = $("upcomingArea");
        const nextAt = $("nextAt");
        const nextQ = $("nextQ");
        const nextOpts = $("nextOpts");

        // State
        const conn = createConnection();
        let sessionId = null;
        const questionTicker = makeTicker();

        /** Render host view from server state. */
        function render(state) {
            // session + players
            setText(sessionLabel, state.sessionId);
            setText(playerCountEl, state.players?.length ?? 0);

            // live question
            if (state.question) {
                setText(statusEl, "Question Live");
                show(questionArea, true);
                show(resultsArea, false);

                setText(liveQ, state.question.text);
                fillOptionList(liveOptions, state.question.options);

                // timer
                questionTicker.start(() => {
                    setText(
                        timerEl,
                        secondsRemainingFromUtc(state.question.questionStartTimeUtc, state.question.durationSeconds)
                    );
                }, 500);

                if (startNowBtn) startNowBtn.disabled = true;
                if (scheduleBtn) scheduleBtn.disabled = true;
                if (endBtn) endBtn.disabled = false;
            } else {
                show(questionArea, false);
                if (endBtn) endBtn.disabled = true;
                questionTicker.stop();
            }

            // upcoming preview
            if (state.upcoming) {
                show(upcomingArea, true);
                setText(nextAt, new Date(state.upcoming.nextQuestionStartTimeUtc).toLocaleTimeString());
                setText(nextQ, state.upcoming.text);
                fillOptionList(nextOpts, state.upcoming.options);

                if (!state.question) setText(statusEl, "Waiting for next question…");
                if (startNowBtn) startNowBtn.disabled = false;
                if (scheduleBtn) scheduleBtn.disabled = false;
            } else {
                show(upcomingArea, false);
                if (!state.question) {
                    setText(statusEl, "Waiting…");
                    if (startNowBtn) startNowBtn.disabled = false;
                    if (scheduleBtn) scheduleBtn.disabled = false;
                }
            }
        }

        // Wire hub events
        conn.on("SessionStateUpdated", render);
        conn.on("QuestionEnded", (summary) => {
            if (!resultsArea) return;
            const { correctIndex, optionCounts = [], leaderboard = [] } = summary;
            const total = optionCounts.reduce((a, b) => a + b, 0) || 1;
            const pct = optionCounts.map((c) => Math.round((100 * c) / total));

            resultsArea.style.display = "";
            resultsArea.innerHTML = `
        <h3>Results</h3>
        <p>Correct option: <strong>${correctIndex}</strong></p>
        <ul>${optionCounts.map((c, i) => `<li>Option ${i}: ${c} (${pct[i]}%)</li>`).join("")}</ul>
        <h4>Leaderboard (Top 10)</h4>
        <ol>${leaderboard.slice(0, 10).map(l => `<li>${l.name} — ${l.score}</li>`).join("")}</ol>
      `;
        });

        // Form submit → claim host
        form.dataset.bound = "main";
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            sessionId = sessionInput.value.trim().toUpperCase();
            await conn.start();
            await conn.invoke("ClaimHost", sessionId);
            if (startNowBtn) startNowBtn.disabled = false;
            if (scheduleBtn) scheduleBtn.disabled = false;
        });

        startNowBtn?.addEventListener("click", async () => {
            const text = qText?.value.trim();
            const options = qOpts.map((o) => o.value.trim()).filter(Boolean);
            const correct = parseInt(qCorrect?.value ?? "0", 10) || 0;
            const dur = parseInt(qDur?.value ?? "20", 10) || 20;

            if (!text || options.length < 2) {
                alert("Enter question and at least 2 options");
                return;
            }

            await conn.invoke("StartQuestionNow", sessionId, text, options, correct, dur);
        });

        // Schedule question (legacy path)
        scheduleBtn?.addEventListener("click", async () => {
            const text = qText?.value.trim();
            const options = qOpts.map((o) => o.value.trim()).filter(Boolean);
            const correct = parseInt(qCorrect?.value ?? "0", 10) || 0;
            const inSec = parseInt(qIn?.value ?? "10", 10) || 10;

            if (!text || options.length < 2) {
                alert("Enter question and at least 2 options");
                return;
            }

            await conn.invoke("ScheduleNextQuestion", sessionId, text, options, correct, inSec);
        });

        // End current question
        endBtn?.addEventListener("click", async () => {
            await conn.invoke("EndQuestion", sessionId);
        });
    })();

    // =====================================================================
    // PLAYER
    // =====================================================================


    (function playerScope() {
        const form = $("joinForm");
        if (!form) return;

        // Form + UI elements
        const nameInput = $("pName");
        const sessionInput = $("pSession");
        const playArea = $("playArea");
        const pStatus = $("pStatus");
        const pQText = $("pQText");
        const pOptions = $("pOptions");
        const waitingPane = $("waitingPane");
        const pQuestion = $("pQuestion");
        const pAnswered = $("pAnswered");
        const pScore = $("pScore");
        const pPlace = $("pPlace");
        const pTimer = $("pTimer");
        const pUpcoming = $("pUpcoming");
        const pNextIn = $("pNextIn");
        const pNextQ = $("pNextQ");
        const pNextOpts = $("pNextOpts");

        // State
        const conn = createConnection();
        let sessionId = null;
        let myName = null;
        let answered = false;
        const questionTicker = makeTicker();
        const upcomingTicker = makeTicker();

        /** Render player view given a sessionStateUpdated payload. */
        function render(state) {
            // My place / score
            const players = Array.isArray(state.players) ? state.players : [];
            const sorted = [...players].sort((a, b) => (b.score - a.score) || a.name.localeCompare(b.name));
            const myIndex = sorted.findIndex((p) => p.name === myName);
            if (myIndex >= 0) setText(pPlace, `${myIndex + 1}/${sorted.length}`);
            const me = players.find((p) => p.name === myName);
            if (me) setText(pScore, me.score);

            // Live question
            if (state.question) {
                setText(pStatus, "Answer the question!");
                show(pQuestion, true);
                show(pUpcoming, false);

                setText(pQText, state.question.text);
                pOptions.innerHTML = "";

                const iHaveAnswered = !!players.find((p) => p.name === myName)?.hasAnswered;
                answered = iHaveAnswered;
                setText(pAnswered, answered ? "Yes" : "No");

                (state.question.options || []).forEach((optText, idx) => {
                    const btn = createAnswerButton(`${idx}: ${optText}`, answered, async () => {
                        if (answered) return;
                        answered = true;
                        setText(pAnswered, "Yes");
                        Array.from(pOptions.children).forEach((b) => b.setAttribute("disabled", "true"));
                        await conn.invoke("SubmitAnswer", sessionId, idx);
                    });
                    pOptions.appendChild(btn);
                });

                // Question timer
                questionTicker.start(() => {
                    setText(
                        pTimer,
                        secondsRemainingFromUtc(state.question.questionStartTimeUtc, state.question.durationSeconds)
                    );
                }, 500);

                // Kill upcoming ticker while question is live
                upcomingTicker.stop();
            } else {
                show(pQuestion, false);
                questionTicker.stop();

                // Upcoming preview (if any)
                if (state.upcoming) {
                    show(pUpcoming, true);
                    setText(pNextQ, state.upcoming.text);
                    pNextOpts.innerHTML = "";
                    (state.upcoming.options || []).forEach((opt, idx) => {
                        const li = document.createElement("li");
                        li.textContent = `${idx}: ${opt}`;
                        pNextOpts.appendChild(li);
                    });

                    // Next-start countdown
                    const targetUtc = state.upcoming.nextQuestionStartTimeUtc;
                    const updateNext = () => setText(pNextIn, secondsUntilUtc(targetUtc));
                    updateNext();
                    upcomingTicker.start(updateNext, 500);
                } else {
                    show(pUpcoming, false);
                    upcomingTicker.stop();
                }

                setText(pStatus, "Waiting…");
            }
        }

        // Hub wiring
        conn.on("SessionStateUpdated", render);

        // Join form
        //form.dataset.bound = "main";
        //form.addEventListener("submit", async (e) => {
        //    e.preventDefault();
        //    sessionId = sessionInput.value.trim().toUpperCase();
        //    myName = nameInput.value.trim();
        //    if (!sessionId || !myName) return;

        //    await conn.start();

        //    const userId = GetFromLocalStorage(localStorageKeys.UserId);
        //    try {
        //        //console.log("Join payload", {
        //        //    sessionId,
        //        //    myName,
        //        //    userIdFromLocalStorage: localStorage.getItem("UserId"),
        //        //    origin: location.origin,
        //        //    hasWrapper: !!window.GetFromLocalStorage,
        //        //    wrapperValue: (window.GetFromLocalStorage && window.localStorageKeys)
        //        //        ? GetFromLocalStorage(localStorageKeys.UserId)
        //        //        : "(wrapper missing)"
        //        //});
        //        await conn.invoke("JoinAsPlayer", sessionId, myName, userId);
        //        // proceed to waiting UI
        //        form.style.display = "none";
        //        if (typeof waitingPane !== "undefined" && waitingPane) waitingPane.style.display = "";
        //        playArea && (playArea.style.display = "none");
        //        setText(pStatus, "Joined — waiting for host…");
        //    } catch (err) {
        //        console.error("Join failed:", err);
        //        setText(pStatus, (err && err.message) ? err.message : "Join failed — check login.");
        //    }
        //});


        function renderQuestionForPlayer(connection, gamePin, questionDto) {
            const normalized = normalizeQuestionDto(questionDto);

            // Show the question area
            const questionArea = document.getElementById("pQuestion");
            if (questionArea) {
                questionArea.style.display = "";
            }

            // Hide upcoming block if present
            const upcomingArea = document.getElementById("pUpcoming");
            if (upcomingArea) {
                upcomingArea.style.display = "none";
            }

            // Set the question text
            setText("pQText", normalized.text);

            // Build 2x2 option grid
            const optionsContainer = document.getElementById("pOptions");
            clearChildren(optionsContainer);

            let answeredAlready = false;

            normalized.options.forEach(function buildOption(option, optionIndex) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "option-card";
                button.setAttribute("data-index", String(optionIndex));

                const label = document.createElement("span");
                label.className = "option-label";
                label.textContent = String(optionIndex + 1);

                const textNode = document.createElement("span");
                textNode.textContent = " " + (option.text ?? "");

                button.appendChild(label);
                button.appendChild(textNode);

                button.addEventListener("click", async function handleClick() {
                    if (answeredAlready) {
                        return;
                    }

                    try {
                        answeredAlready = true;

                        // Disable all buttons
                        const allButtons = optionsContainer.querySelectorAll(".option-card");
                        allButtons.forEach(function disableButton(b) {
                            b.setAttribute("disabled", "true");
                        });

                        setText("pAnswered", "Yes");

                        await connection.invoke("SubmitAnswer", gamePin, optionIndex);
                    } catch (error) {
                        console.error("SubmitAnswer failed:", error);
                    }
                });

                optionsContainer.appendChild(button);
            });

            // Start/refresh the per-question timer
            const timerElement = document.getElementById("pTimer");
            if (timerElement) {
                const startUtc = normalized.questionStartTimeUtc;
                const durationSeconds = normalized.durationSeconds;

                // Paint once immediately
                timerElement.textContent = String(
                    secondsRemainingFromUtc(startUtc, durationSeconds)
                );

                // Update every 500ms until it hits 0
                const intervalId = window.setInterval(function tickTimer() {
                    const remain = secondsRemainingFromUtc(startUtc, durationSeconds);

                    timerElement.textContent = String(remain);

                    if (remain <= 0) {
                        window.clearInterval(intervalId);
                    }
                }, 500);
            }

            // Mark "Answered: No" at the start
            setText("pAnswered", "No");
        }

        conn.on("StartNextQuestion", (questionDto) => {
            if (!questionDto) return;

            renderQuestionForPlayer(conn, sessionId, questionDto);
        });

        
    })();

})();