function createConnection() {
  return new signalR.HubConnectionBuilder().withUrl("/gamehub").withAutomaticReconnect().build();
}

// Shared timer utilities
function secondsRemainingFromUtc(startUtc, durationSeconds) {
  if(!startUtc) return 0;
  const start = new Date(startUtc);
  const elapsed = (Date.now() - start.getTime())/1000;
  return Math.max(0, Math.floor(durationSeconds - elapsed));
}

function secondsUntilUtc(targetUtc) {
  if(!targetUtc) return 0;
  const target = new Date(targetUtc);
  const diff = (target.getTime() - Date.now())/1000;
  return Math.max(0, Math.floor(diff));
}

// ---------------- HOST ----------------
(function hostScope(){
  const form = document.getElementById('hostForm');
  if(!form) return;

  const roomInput = document.getElementById('roomId');
  const roomLabel = document.getElementById('roomLabel');
  const playerCountEl = document.getElementById('playerCount');
  const statusEl = document.getElementById('status');
  const timerEl = document.getElementById('timer');
  const startNowBtn = document.getElementById('startNowBtn');
  const scheduleBtn = document.getElementById('scheduleBtn');
  const endBtn = document.getElementById('endBtn');
  const qText = document.getElementById('qText');
  const qDur = document.getElementById('qDur');
  const qIn = document.getElementById('qIn');
  const qCorrect = document.getElementById('qCorrect');
  const qOpts = Array.from(document.querySelectorAll('.opt'));
  const questionArea = document.getElementById('questionArea');
  const liveQ = document.getElementById('liveQ');
  const liveOptions = document.getElementById('liveOptions');
  const resultsArea = document.getElementById('resultsArea');
  const upcomingArea = document.getElementById('upcomingArea');
  const nextAt = document.getElementById('nextAt');
  const nextQ = document.getElementById('nextQ');
  const nextOpts = document.getElementById('nextOpts');

  let roomId = null;
  let countdown = null;
  const conn = createConnection();

  function render(state){
    roomLabel.textContent = state.roomId;
    playerCountEl.textContent = state.players.length;

    // Question live
    if(state.question){
      statusEl.textContent = "Question Live";
      questionArea.style.display = '';
      resultsArea.style.display = 'none';
      liveQ.textContent = state.question.text;
      liveOptions.innerHTML = '';
      (state.question.options || []).forEach((o,i)=>{
        const li = document.createElement('li'); li.textContent = `${i}: ${o}`; liveOptions.appendChild(li);
      });
      // timer
      clearInterval(countdown);
      countdown = setInterval(()=>{
        timerEl.textContent = secondsRemainingFromUtc(state.question.questionStartTimeUtc, state.question.durationSeconds);
      }, 500);
      startNowBtn.disabled = true; scheduleBtn.disabled = true; endBtn.disabled = false;
    } else {
      questionArea.style.display = 'none';
      endBtn.disabled = true;
    }

    // Upcoming preview
    if(state.upcoming){
      upcomingArea.style.display = '';
      nextAt.textContent = new Date(state.upcoming.nextQuestionStartTimeUtc).toLocaleTimeString();
      nextQ.textContent = state.upcoming.text;
      nextOpts.innerHTML = '';
      (state.upcoming.options||[]).forEach((o,i)=>{
        const li = document.createElement('li'); li.textContent = `${i}: ${o}`; nextOpts.appendChild(li);
      });
      if(!state.question) statusEl.textContent = "Waiting for next question…";
      startNowBtn.disabled = false; scheduleBtn.disabled = false;
    } else {
      upcomingArea.style.display = 'none';
      if(!state.question) { statusEl.textContent = "Waiting…"; startNowBtn.disabled = false; scheduleBtn.disabled = false; }
    }
  }

  conn.on('RoomStateUpdated', render);

  conn.on('QuestionEnded', summary => {
    resultsArea.style.display = '';
    const { correctIndex, optionCounts, leaderboard } = summary;
    const total = (optionCounts||[]).reduce((a,b)=>a+b,0)||1;
    const pct = optionCounts.map(c => Math.round(100*c/total));
    resultsArea.innerHTML = `
      <h3>Results</h3>
      <p>Correct option: <strong>${correctIndex}</strong></p>
      <ul>${optionCounts.map((c,i)=>`<li>Option ${i}: ${c} (${pct[i]}%)</li>`).join('')}</ul>
      <h4>Leaderboard (Top 10)</h4>
      <ol>${leaderboard.slice(0,10).map(l=>`<li>${l.name} — ${l.score}</li>`).join('')}</ol>
    `;
  });

  form.addEventListener('submit', async (e)=>{
    e.preventDefault(); roomId = roomInput.value.trim().toUpperCase();
    await conn.start(); await conn.invoke('ClaimHost', roomId); startNowBtn.disabled = false; scheduleBtn.disabled = false;
  });

  startNowBtn?.addEventListener('click', async ()=>{
    const text = qText.value.trim(); const options = qOpts.map(o=>o.value.trim()).filter(Boolean);
    const correct = parseInt(qCorrect.value,10) || 0; const dur = parseInt(qDur.value,10) || 20;
    if(!text || options.length < 2) { alert('Enter question and at least 2 options'); return; }
    await conn.invoke('StartQuestionNow', roomId, text, options, correct, dur);
  });

  scheduleBtn?.addEventListener('click', async ()=>{
    const text = qText.value.trim(); const options = qOpts.map(o=>o.value.trim()).filter(Boolean);
    const correct = parseInt(qCorrect.value,10) || 0; const inSec = parseInt(qIn.value,10) || 10;
    if(!text || options.length < 2) { alert('Enter question and at least 2 options'); return; }
    await conn.invoke('ScheduleNextQuestion', roomId, text, options, correct, inSec);
  });

  endBtn?.addEventListener('click', async ()=>{ await conn.invoke('EndQuestion', roomId); });
})();

// ---------------- PLAYER ----------------
(function playerScope(){
  const form = document.getElementById('joinForm');
  if(!form) return;

  const nameInput = document.getElementById('pName');
  const roomInput = document.getElementById('pRoom');
  const playArea = document.getElementById('playArea');
  const pStatus = document.getElementById('pStatus');
  const pQText = document.getElementById('pQText');
  const pOptions = document.getElementById('pOptions');
  const pQuestion = document.getElementById('pQuestion');
  const pAnswered = document.getElementById('pAnswered');
  const pScore = document.getElementById('pScore');
  const pPlace = document.getElementById('pPlace');
  const pTimer = document.getElementById('pTimer');
  const pUpcoming = document.getElementById('pUpcoming');
  const pNextIn = document.getElementById('pNextIn');
  const pNextQ = document.getElementById('pNextQ');
  const pNextOpts = document.getElementById('pNextOpts');

  let roomId = null;
  let answered = false;
  let myName = null;

  const conn = createConnection();

  function render(state){
    // Leaderboard place: compute my ranking
    const sorted = [...state.players].sort((a,b)=> b.score - a.score || a.name.localeCompare(b.name));
    const myIndex = sorted.findIndex(p => p.name === myName);
    if(myIndex >= 0) pPlace.textContent = `${myIndex+1}/${sorted.length}`;

    if(state.question){
      pStatus.textContent = "Answer the question!";
      pQuestion.style.display = ''; pUpcoming.style.display = 'none';
      pQText.textContent = state.question.text;
      pOptions.innerHTML = '';
      answered = state.players.find(p => p.name === myName)?.hasAnswered ?? false;
      pAnswered.textContent = answered ? 'Yes' : 'No';
      (state.question.options||[]).forEach((o,i)=>{
        const btn = document.createElement('button');
        btn.className = 'big';
        btn.textContent = `${i}: ${o}`;
        if(answered) btn.setAttribute('disabled','true');
        btn.onclick = async ()=>{
          if(answered) return;
          answered = true; pAnswered.textContent = 'Yes';
          Array.from(pOptions.children).forEach(b=>b.setAttribute('disabled','true'));
          await conn.invoke('SubmitAnswer', roomId, i);
        };
        pOptions.appendChild(btn);
      });
      // timer
      pTimer.textContent = secondsRemainingFromUtc(state.question.questionStartTimeUtc, state.question.durationSeconds);
      const iv = setInterval(()=>{
        const remain = secondsRemainingFromUtc(state.question.questionStartTimeUtc, state.question.durationSeconds);
        pTimer.textContent = remain;
        if(remain <= 0) clearInterval(iv);
      }, 500);
    } else {
      pQuestion.style.display = 'none';
      // Upcoming preview
      if(state.upcoming){
        pUpcoming.style.display = '';
        pNextQ.textContent = state.upcoming.text;
        pNextOpts.innerHTML = '';
        (state.upcoming.options||[]).forEach((o,i)=>{
          const li = document.createElement('li'); li.textContent = `${i}: ${o}`; pNextOpts.appendChild(li);
        });
        const updateNext = ()=>{ pNextIn.textContent = secondsUntilUtc(state.upcoming.nextQuestionStartTimeUtc); };
        updateNext();
        const iv2 = setInterval(()=>{
          updateNext();
        }, 500);
      } else {
        pUpcoming.style.display = 'none';
      }
      pStatus.textContent = "Waiting…";
    }

    // Update my score from state
    const me = state.players.find(p => p.name === myName);
    if(me) pScore.textContent = me.score;
  }

  conn.on('RoomStateUpdated', render);

  form.addEventListener('submit', async (e)=>{
    e.preventDefault();
    roomId = roomInput.value.trim().toUpperCase();
    myName = nameInput.value.trim();
    if(!roomId || !myName) return;
    await conn.start();
    await conn.invoke('JoinAsPlayer', roomId, myName);
    playArea.style.display = ''; form.style.display = 'none';
    pStatus.textContent = 'Joined — waiting for host…';
  });
})();