/* 
 * Quizzy client script
 * - Host UI (legacy host page) and Player UI 
 * - Shared SignalR connection factory + small DOM helpers
 * - Clean rendering functions and timer management
 *
 * NOTE: This preserves existing server API calls and event names.
 *  - Server events listened:   "sessionStateUpdated", "QuestionEnded"
 *  - Server methods invoked:   Host: ClaimHost, StartQuestionNow, ScheduleNextQuestion, EndQuestion
 *                               Player: JoinAsPlayer, SubmitAnswer
 *
 * If you are using the new Host Lobby (/Host/Lobby), its inline script handles host actions there.
 * The host section below supports the original host page with a form + controls.
 */

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
            start(fn, ms) { this.stop(); id = setInterval(fn, ms); },
            stop() { if (id) { clearInterval(id); id = null; } }
        };
    }

    // =====================================================================
    // HOST (legacy host page with form/controls)
    // =====================================================================
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
        conn.on("sessionStateUpdated", render);
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

        // Start question now (legacy path that passes question text/options to server)
        startNowBtn?.addEventListener("click", async () => {
            const text = qText?.value.trim();
            const options = qOpts.map((o) => o.value.trim()).filter(Boolean);
            const correct = parseInt(qCorrect?.value ?? "0", 10) || 0;
            const dur = parseInt(qDur?.value ?? "20", 10) || 20;
            if (!text || options.length < 2) { alert("Enter question and at least 2 options"); return; }
            await conn.invoke("StartQuestionNow", sessionId, text, options, correct, dur);
        });

        // Schedule question (legacy path)
        scheduleBtn?.addEventListener("click", async () => {
            const text = qText?.value.trim();
            const options = qOpts.map((o) => o.value.trim()).filter(Boolean);
            const correct = parseInt(qCorrect?.value ?? "0", 10) || 0;
            const inSec = parseInt(qIn?.value ?? "10", 10) || 10;
            if (!text || options.length < 2) { alert("Enter question and at least 2 options"); return; }
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
        const sessionInput = $("psession");
        const playArea = $("playArea");
        const pStatus = $("pStatus");
        const pQText = $("pQText");
        const pOptions = $("pOptions");
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
        conn.on("sessionStateUpdated", render);

        // Join form
        form.dataset.bound = "main";
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            sessionId = sessionInput.value.trim().toUpperCase();
            myName = nameInput.value.trim();
            if (!sessionId || !myName) return;
            await conn.start();
            await conn.invoke("JoinAsPlayer", sessionId, myName);
            show(playArea, true);
            show(form, false);
            setText(pStatus, "Joined — waiting for host…");
        });
    })();

})();