(() => {
  'use strict';

  const canvas = document.getElementById('game');
  const ctx = canvas.getContext('2d');
  const intro = document.getElementById('intro');
  const startButton = document.getElementById('startButton');
  const blueMeter = document.getElementById('blueEvidence');
  const greenMeter = document.getElementById('greenEvidence');
  const blueValue = document.getElementById('blueValue');
  const greenValue = document.getElementById('greenValue');
  const neuralState = document.getElementById('neuralState');
  const lastEvent = document.getElementById('lastEvent');

  const W = canvas.width;
  const H = canvas.height;
  const TAU = Math.PI * 2;
  const keys = new Set();

  const cfg = {
    blueHz: 10,
    greenHz: 12,
    evidenceRate: 0.82,
    evidenceDecay: 0.62,
    threshold: 0.86,
    sightDuration: 3.4,
    guardDuration: 3.4,
    sightMultiplier: 1.65,
    guardHealPerSecond: 4.2,
    orbAngularSpeed: 0.92,
    orbRadiusX: 116,
    orbRadiusY: 56,
    playerSpeed: 260,
    dashSpeed: 610,
    dashDuration: 0.18,
    dashCooldown: 1.1,
    fireCooldown: 0.22,
  };

  const state = {
    started: false,
    t: 0,
    stage: 'intro',
    calibrationTarget: null,
    movingSelections: 0,
    lastShot: -99,
    lastDash: -99,
    blueEvidence: 0,
    greenEvidence: 0,
    sightUntil: 0,
    guardUntil: 0,
    particles: [],
    shots: [],
    enemyShots: [],
    eventLog: [],
    bossStartedAt: 0,
  };

  const player = { x: W * 0.24, y: H * 0.56, r: 18, hp: 100, maxHp: 100, dashUntil: 0 };
  const boss = { x: W * 0.72, y: H * 0.48, r: 62, hp: 860, maxHp: 860, phase: 1, alive: false, shotClock: 0 };
  const construct = { x: W * 0.62, y: H * 0.47, r: 48 };
  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));

  function setStage(stage) {
    state.stage = stage;
    state.blueEvidence = 0;
    state.greenEvidence = 0;
    if (stage === 'cal-sight') {
      state.calibrationTarget = 'sight';
      neuralState.textContent = 'CALIBRATE SIGHT';
      lastEvent.textContent = 'Attend blue';
    } else if (stage === 'cal-guard') {
      state.calibrationTarget = 'guard';
      neuralState.textContent = 'CALIBRATE GUARD';
      lastEvent.textContent = 'Attend green';
    } else if (stage === 'cal-moving') {
      state.calibrationTarget = 'sight';
      state.movingSelections = 0;
      neuralState.textContent = 'MOVING VALIDATION';
      lastEvent.textContent = 'Blue first';
    } else if (stage === 'combat') {
      state.calibrationTarget = null;
      boss.alive = true;
      boss.hp = boss.maxHp;
      boss.shotClock = 0.5;
      state.bossStartedAt = state.t;
      neuralState.textContent = 'LIVE COMBAT';
      lastEvent.textContent = 'Choose your aura';
    } else if (stage === 'win') neuralState.textContent = 'SIGNAL RESTORED';
    else if (stage === 'fail') neuralState.textContent = 'GUARDIAN LOST';
  }

  function recordEvent(type, target, confidence, quality, reason = null) {
    state.eventLog.push({ t: state.t, type, target, confidence, quality, reason });
    if (state.eventLog.length > 100) state.eventLog.shift();
  }

  function emitParticle(x, y, kind, n = 10) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * TAU;
      const s = 35 + Math.random() * 120;
      state.particles.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, life: 0.35 + Math.random() * 0.45, max: 0.8, kind });
    }
  }

  function completeCalibrationSelection(target) {
    if (state.stage === 'cal-sight' && target === 'sight') {
      recordEvent('CAL_ACCEPT', 'sight', 0.94, 0.94);
      emitParticle(construct.x, construct.y, 'blue', 42);
      setStage('cal-guard');
      return true;
    }
    if (state.stage === 'cal-guard' && target === 'guard') {
      recordEvent('CAL_ACCEPT', 'guard', 0.93, 0.94);
      emitParticle(construct.x, construct.y, 'green', 42);
      setStage('cal-moving');
      return true;
    }
    if (state.stage === 'cal-moving' && target === state.calibrationTarget) {
      state.movingSelections += 1;
      recordEvent('CAL_MOVING_ACCEPT', target, 0.91, 0.92);
      emitParticle(construct.x, construct.y, target === 'sight' ? 'blue' : 'green', 24);
      if (state.movingSelections >= 4) setStage('combat');
      else {
        state.calibrationTarget = target === 'sight' ? 'guard' : 'sight';
        lastEvent.textContent = state.calibrationTarget === 'sight' ? 'Now blue' : 'Now green';
      }
      return true;
    }
    return false;
  }

  function applyAura(target, confidence = 1, quality = 1) {
    if (!state.started || state.stage === 'intro') return;
    if (confidence < 0.55 || quality < 0.55) {
      recordEvent('ABSTAIN', target, confidence, quality, 'QUALITY');
      lastEvent.textContent = `ABSTAIN ${target ?? ''}`;
      neuralState.textContent = 'UNCERTAIN';
      return;
    }
    if (completeCalibrationSelection(target)) {
      state.blueEvidence = 0;
      state.greenEvidence = 0;
      return;
    }
    if (state.stage !== 'combat') return;
    if (target === 'sight') {
      state.sightUntil = Math.max(state.sightUntil, state.t + cfg.sightDuration);
      state.blueEvidence = 0;
      emitParticle(boss.x, boss.y, 'blue', 34);
      lastEvent.textContent = `SIGHT ${confidence.toFixed(2)}`;
      neuralState.textContent = 'NEURAL SIGHT';
      recordEvent('AURA_SELECTED', 'sight', confidence, quality);
    } else if (target === 'guard') {
      state.guardUntil = Math.max(state.guardUntil, state.t + cfg.guardDuration);
      state.greenEvidence = 0;
      emitParticle(player.x, player.y, 'green', 34);
      lastEvent.textContent = `GUARD ${confidence.toFixed(2)}`;
      neuralState.textContent = 'NEURAL GUARD';
      recordEvent('AURA_SELECTED', 'guard', confidence, quality);
    }
  }

  window.addEventListener('mindforge-neural-event', (e) => {
    const d = e.detail || {};
    if (d.event === 'AURA_SELECTED') applyAura(d.target, Number(d.confidence ?? 0), Number(d.quality ?? 0));
    if (d.event === 'ABSTAIN') {
      neuralState.textContent = 'ABSTAIN';
      lastEvent.textContent = d.reason || 'ABSTAIN';
      recordEvent('ABSTAIN', null, Number(d.confidence ?? 0), Number(d.quality ?? 0), d.reason || null);
    }
  });

  function currentPrompt() {
    if (state.stage === 'cal-sight') return 'sight';
    if (state.stage === 'cal-guard') return 'guard';
    if (state.stage === 'cal-moving') return state.calibrationTarget;
    return null;
  }

  function updateEvidence(dt) {
    const b = keys.has('q');
    const g = keys.has('e');
    if (b && !g) {
      state.blueEvidence = clamp(state.blueEvidence + cfg.evidenceRate * dt, 0, 1);
      state.greenEvidence = clamp(state.greenEvidence - cfg.evidenceDecay * dt, 0, 1);
    } else if (g && !b) {
      state.greenEvidence = clamp(state.greenEvidence + cfg.evidenceRate * dt, 0, 1);
      state.blueEvidence = clamp(state.blueEvidence - cfg.evidenceDecay * dt, 0, 1);
    } else {
      state.blueEvidence = clamp(state.blueEvidence - cfg.evidenceDecay * dt, 0, 1);
      state.greenEvidence = clamp(state.greenEvidence - cfg.evidenceDecay * dt, 0, 1);
    }
    if (state.blueEvidence >= cfg.threshold) applyAura('sight', 0.90, 0.92);
    if (state.greenEvidence >= cfg.threshold) applyAura('guard', 0.90, 0.92);
  }

  function shoot() {
    if (state.stage !== 'combat' || state.t - state.lastShot < cfg.fireCooldown || !boss.alive) return;
    state.lastShot = state.t;
    const dx = boss.x - player.x;
    const dy = boss.y - player.y;
    const m = Math.hypot(dx, dy) || 1;
    state.shots.push({ x: player.x, y: player.y, vx: (dx / m) * 700, vy: (dy / m) * 700, life: 1.4 });
  }

  function dash() {
    if (state.stage !== 'combat' || state.t - state.lastDash < cfg.dashCooldown) return;
    state.lastDash = state.t;
    player.dashUntil = state.t + cfg.dashDuration;
    emitParticle(player.x, player.y, 'white', 14);
  }

  function updatePlayer(dt) {
    if (state.stage !== 'combat') return;
    let dx = (keys.has('d') ? 1 : 0) - (keys.has('a') ? 1 : 0);
    let dy = (keys.has('s') ? 1 : 0) - (keys.has('w') ? 1 : 0);
    const m = Math.hypot(dx, dy) || 1;
    const speed = state.t < player.dashUntil ? cfg.dashSpeed : cfg.playerSpeed;
    if (dx || dy) { dx /= m; dy /= m; }
    player.x = clamp(player.x + dx * speed * dt, 38, W - 38);
    player.y = clamp(player.y + dy * speed * dt, 56, H - 44);
    if (keys.has(' ')) shoot();
    if (state.t < state.guardUntil) player.hp = clamp(player.hp + cfg.guardHealPerSecond * dt, 0, player.maxHp);
  }

  function updateBoss(dt) {
    if (state.stage !== 'combat' || !boss.alive) return;
    boss.phase = boss.hp > boss.maxHp * 0.66 ? 1 : boss.hp > boss.maxHp * 0.33 ? 2 : 3;
    boss.shotClock -= dt;
    if (boss.shotClock <= 0) {
      boss.shotClock = [0, 1.10, 0.78, 0.52][boss.phase];
      for (let i = 0; i < boss.phase; i++) {
        const aim = Math.atan2(player.y - boss.y, player.x - boss.x) + (i - (boss.phase - 1) / 2) * 0.22;
        state.enemyShots.push({ x: boss.x, y: boss.y, vx: Math.cos(aim) * (250 + boss.phase * 25), vy: Math.sin(aim) * (250 + boss.phase * 25), r: 8, life: 4 });
      }
    }
  }

  function updateProjectiles(dt) {
    for (const s of state.shots) {
      s.x += s.vx * dt; s.y += s.vy * dt; s.life -= dt;
      if (boss.alive && s.life > 0 && (s.x - boss.x) ** 2 + (s.y - boss.y) ** 2 < (boss.r + 5) ** 2) {
        const damage = 18 * (state.t < state.sightUntil ? cfg.sightMultiplier : 1);
        boss.hp = Math.max(0, boss.hp - damage);
        s.life = 0;
        emitParticle(s.x, s.y, state.t < state.sightUntil ? 'blue' : 'white', 8);
        if (boss.hp <= 0) { boss.alive = false; emitParticle(boss.x, boss.y, 'blue', 90); setStage('win'); }
      }
    }
    for (const s of state.enemyShots) {
      s.x += s.vx * dt; s.y += s.vy * dt; s.life -= dt;
      if (s.life > 0 && (s.x - player.x) ** 2 + (s.y - player.y) ** 2 < (s.r + player.r) ** 2 && state.t >= player.dashUntil) {
        player.hp = Math.max(0, player.hp - 10);
        s.life = 0;
        emitParticle(player.x, player.y, 'red', 12);
        if (player.hp <= 0) setStage('fail');
      }
    }
    state.shots = state.shots.filter((s) => s.life > 0);
    state.enemyShots = state.enemyShots.filter((s) => s.life > 0);
  }

  function updateParticles(dt) {
    for (const p of state.particles) { p.x += p.vx * dt; p.y += p.vy * dt; p.vx *= 0.97; p.vy *= 0.97; p.life -= dt; }
    state.particles = state.particles.filter((p) => p.life > 0);
  }

  function targetCenter() { return state.stage === 'combat' ? boss : construct; }
  function auraPosition(kind) {
    const center = targetCenter();
    const moving = state.stage === 'cal-moving' || state.stage === 'combat';
    const phase = (moving ? state.t * cfg.orbAngularSpeed : 0) + (kind === 'guard' ? Math.PI : 0);
    return { x: center.x + Math.cos(phase) * cfg.orbRadiusX, y: center.y + Math.sin(phase) * cfg.orbRadiusY };
  }

  function drawBackground() {
    const g = ctx.createRadialGradient(W * 0.55, H * 0.45, 20, W * 0.55, H * 0.45, W * 0.65);
    g.addColorStop(0, '#171a36'); g.addColorStop(1, '#050710'); ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
    ctx.strokeStyle = 'rgba(140,150,220,.10)';
    for (let x = 0; x < W; x += 64) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, H); ctx.stroke(); }
    for (let y = 0; y < H; y += 64) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke(); }
  }

  function drawWisp() {
    const combat = state.stage === 'combat';
    const wx = combat ? player.x + (boss.x - player.x) * 0.18 : player.x + 42 + Math.sin(state.t * 2.2) * 8;
    const wy = combat ? player.y - 55 : player.y - 44 + Math.cos(state.t * 1.9) * 8;
    ctx.save(); ctx.globalCompositeOperation = 'lighter';
    const glow = ctx.createRadialGradient(wx, wy, 0, wx, wy, 30);
    glow.addColorStop(0, 'rgba(241,242,255,.95)'); glow.addColorStop(0.35, 'rgba(160,170,255,.45)'); glow.addColorStop(1, 'rgba(120,130,255,0)');
    ctx.fillStyle = glow; ctx.beginPath(); ctx.arc(wx, wy, 30, 0, TAU); ctx.fill(); ctx.restore();
  }

  function drawAura(target, color, hz, pos, activeUntil, evidence) {
    const phase = Math.sin(TAU * hz * state.t);
    const mod = 0.52 + 0.48 * (phase * 0.5 + 0.5);
    const active = state.t < activeUntil;
    const prompted = currentPrompt() === target;
    const r = 24 + 9 * mod + (active ? 7 : 0) + (prompted ? 5 : 0);
    const rgb = color === 'blue' ? '75,154,255' : '69,239,154';
    ctx.save(); ctx.globalCompositeOperation = 'lighter';
    const grad = ctx.createRadialGradient(pos.x, pos.y, 2, pos.x, pos.y, r * 1.9);
    grad.addColorStop(0, `rgba(${rgb},${0.88 * mod + 0.12})`); grad.addColorStop(0.34, `rgba(${rgb},${0.45 * mod})`); grad.addColorStop(1, `rgba(${rgb},0)`);
    ctx.fillStyle = grad; ctx.beginPath(); ctx.arc(pos.x, pos.y, r * 1.9, 0, TAU); ctx.fill();
    ctx.strokeStyle = prompted ? 'rgba(255,255,255,.98)' : `rgba(${rgb},${0.55 + 0.4 * mod})`;
    ctx.lineWidth = prompted ? 6 : active ? 5 : 3; ctx.beginPath(); ctx.arc(pos.x, pos.y, r, 0, TAU); ctx.stroke();
    ctx.strokeStyle = 'rgba(255,255,255,.9)'; ctx.lineWidth = 2;
    if (target === 'sight') { ctx.beginPath(); ctx.moveTo(pos.x, pos.y - 13); ctx.lineTo(pos.x + 11, pos.y + 9); ctx.lineTo(pos.x - 11, pos.y + 9); ctx.closePath(); ctx.stroke(); }
    else { ctx.beginPath(); ctx.arc(pos.x, pos.y, 11, 0, TAU); ctx.stroke(); ctx.beginPath(); ctx.moveTo(pos.x - 7, pos.y); ctx.lineTo(pos.x + 7, pos.y); ctx.moveTo(pos.x, pos.y - 7); ctx.lineTo(pos.x, pos.y + 7); ctx.stroke(); }
    ctx.restore();
    ctx.fillStyle = '#e9ecff'; ctx.font = '600 13px system-ui'; ctx.textAlign = 'center';
    ctx.fillText(target === 'sight' ? `SIGHT · ${hz} Hz` : `GUARD · ${hz} Hz`, pos.x, pos.y + 50);
    if (evidence > 0.02) { ctx.fillStyle = 'rgba(255,255,255,.15)'; ctx.fillRect(pos.x - 28, pos.y + 57, 56, 5); ctx.fillStyle = color === 'blue' ? '#55a3ff' : '#55efa5'; ctx.fillRect(pos.x - 28, pos.y + 57, 56 * evidence, 5); }
  }

  function drawConstruct() {
    if (!state.stage.startsWith('cal-')) return;
    const pulse = 0.5 + 0.5 * Math.sin(state.t * 1.6);
    ctx.fillStyle = `rgba(100,104,165,${0.5 + pulse * 0.15})`; ctx.beginPath(); ctx.arc(construct.x, construct.y, construct.r, 0, TAU); ctx.fill();
    ctx.strokeStyle = 'rgba(210,215,255,.35)'; ctx.lineWidth = 4; ctx.beginPath(); ctx.arc(construct.x, construct.y, construct.r + 8, 0, TAU); ctx.stroke();
  }

  function drawPlayer() {
    ctx.save(); ctx.translate(player.x, player.y);
    if (state.t < state.guardUntil) { ctx.strokeStyle = 'rgba(74,239,157,.65)'; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(0, 0, 31, 0, TAU); ctx.stroke(); }
    ctx.fillStyle = '#e9ecff'; ctx.beginPath(); ctx.arc(0, 0, player.r, 0, TAU); ctx.fill();
    ctx.fillStyle = '#7c86d5'; ctx.beginPath(); ctx.moveTo(10, -12); ctx.lineTo(29, 0); ctx.lineTo(10, 12); ctx.closePath(); ctx.fill(); ctx.restore();
  }

  function drawBoss() {
    if (!boss.alive) return;
    const pulse = 0.5 + 0.5 * Math.sin(state.t * (2 + boss.phase));
    ctx.save(); ctx.translate(boss.x, boss.y); ctx.fillStyle = `rgba(${90 + boss.phase * 22},65,150,.95)`; ctx.beginPath(); ctx.arc(0, 0, boss.r, 0, TAU); ctx.fill();
    ctx.strokeStyle = `rgba(210,190,255,${0.35 + 0.35 * pulse})`; ctx.lineWidth = 6; ctx.beginPath(); ctx.arc(0, 0, boss.r + 8 + pulse * 5, 0, TAU); ctx.stroke();
    ctx.fillStyle = '#f0ebff'; ctx.font = '800 18px system-ui'; ctx.textAlign = 'center'; ctx.fillText('THE FRACTURED SIGNAL', 0, 6); ctx.restore();
  }

  function drawProjectiles() {
    ctx.fillStyle = state.t < state.sightUntil ? '#5aa8ff' : '#f5f6ff'; for (const s of state.shots) { ctx.beginPath(); ctx.arc(s.x, s.y, state.t < state.sightUntil ? 7 : 5, 0, TAU); ctx.fill(); }
    ctx.fillStyle = '#ff657d'; for (const s of state.enemyShots) { ctx.beginPath(); ctx.arc(s.x, s.y, s.r, 0, TAU); ctx.fill(); }
  }

  function drawParticles() {
    for (const p of state.particles) { const a = clamp(p.life / p.max, 0, 1); const c = p.kind === 'blue' ? `rgba(78,165,255,${a})` : p.kind === 'green' ? `rgba(72,240,157,${a})` : p.kind === 'red' ? `rgba(255,91,112,${a})` : `rgba(245,247,255,${a})`; ctx.fillStyle = c; ctx.beginPath(); ctx.arc(p.x, p.y, 3, 0, TAU); ctx.fill(); }
  }

  function drawHud() {
    ctx.textAlign = 'left'; ctx.font = '700 15px system-ui'; ctx.fillStyle = '#eef0ff'; ctx.fillText('GUARDIAN', 34, 38);
    ctx.fillStyle = 'rgba(255,255,255,.14)'; ctx.fillRect(34, 48, 240, 12); ctx.fillStyle = '#55efa5'; ctx.fillRect(34, 48, 240 * player.hp / player.maxHp, 12); ctx.fillStyle = '#eef0ff'; ctx.fillText(`HP ${Math.ceil(player.hp)}`, 34, 82);
    ctx.textAlign = 'center';
    if (state.stage === 'combat') { ctx.fillText(`FRACTURED SIGNAL · PHASE ${boss.phase}`, W / 2, 38); ctx.fillStyle = 'rgba(255,255,255,.14)'; ctx.fillRect(W / 2 - 220, 48, 440, 10); ctx.fillStyle = '#ad77ff'; ctx.fillRect(W / 2 - 220, 48, 440 * boss.hp / boss.maxHp, 10); }
    else if (state.stage.startsWith('cal-')) ctx.fillText('WISP ATTUNEMENT', W / 2, 38);
    else if (state.stage === 'win') ctx.fillText('SIGNAL RESTORED', W / 2, 38);
    else if (state.stage === 'fail') ctx.fillText('GUARDIAN LOST · RELOAD TO RETRY', W / 2, 38);
    const sight = Math.max(0, state.sightUntil - state.t); const guard = Math.max(0, state.guardUntil - state.t);
    ctx.textAlign = 'right'; ctx.fillStyle = sight > 0 ? '#61acff' : '#777d9f'; ctx.fillText(`SIGHT ×${cfg.sightMultiplier.toFixed(2)} ${sight.toFixed(1)}s`, W - 34, 38);
    ctx.fillStyle = guard > 0 ? '#5cf2a5' : '#777d9f'; ctx.fillText(`GUARD +${cfg.guardHealPerSecond.toFixed(1)} HP/s ${guard.toFixed(1)}s`, W - 34, 62);
  }

  function drawStageInstruction() {
    if (!state.stage.startsWith('cal-')) return;
    const prompt = currentPrompt();
    let subtitle = '';
    if (state.stage === 'cal-sight') subtitle = 'Hold Q to simulate visually attending the BLUE Sight aura.';
    if (state.stage === 'cal-guard') subtitle = 'Hold E to simulate visually attending the GREEN Guard aura.';
    if (state.stage === 'cal-moving') subtitle = `${state.movingSelections}/4 moving selections · ${prompt === 'sight' ? 'BLUE / Q' : 'GREEN / E'} next`;
    ctx.textAlign = 'center'; ctx.fillStyle = 'rgba(7,9,20,.76)'; ctx.fillRect(W / 2 - 330, H - 112, 660, 76);
    ctx.fillStyle = '#f4f5ff'; ctx.font = '800 18px system-ui'; ctx.fillText('The Forge is learning your Wisp.', W / 2, H - 82);
    ctx.fillStyle = '#b9bfdc'; ctx.font = '500 14px system-ui'; ctx.fillText(subtitle, W / 2, H - 57);
  }

  function render() {
    drawBackground(); drawWisp(); drawConstruct(); drawBoss();
    if (state.stage.startsWith('cal-') || state.stage === 'combat') { drawAura('sight', 'blue', cfg.blueHz, auraPosition('sight'), state.sightUntil, state.blueEvidence); drawAura('guard', 'green', cfg.greenHz, auraPosition('guard'), state.guardUntil, state.greenEvidence); }
    drawPlayer(); drawProjectiles(); drawParticles(); drawHud(); drawStageInstruction();
  }

  function updateTelemetry() {
    blueMeter.value = state.blueEvidence; greenMeter.value = state.greenEvidence; blueValue.textContent = state.blueEvidence.toFixed(2); greenValue.textContent = state.greenEvidence.toFixed(2);
    if (state.stage === 'combat' && state.t >= state.sightUntil && state.t >= state.guardUntil && !keys.has('q') && !keys.has('e')) neuralState.textContent = 'READY';
  }

  let last = performance.now();
  function frame(now) {
    const dt = Math.min(0.033, (now - last) / 1000); last = now;
    if (state.started) { state.t += dt; updateEvidence(dt); updatePlayer(dt); updateBoss(dt); updateProjectiles(dt); updateParticles(dt); updateTelemetry(); }
    render(); requestAnimationFrame(frame);
  }

  function start() { state.started = true; intro.classList.add('hidden'); setStage('cal-sight'); }

  window.addEventListener('keydown', (e) => { const k = e.key.toLowerCase(); if (['w', 'a', 's', 'd', 'q', 'e', ' ', 'shift'].includes(k)) e.preventDefault(); keys.add(k); if (e.key === 'Shift') dash(); });
  window.addEventListener('keyup', (e) => keys.delete(e.key.toLowerCase()));
  startButton.addEventListener('click', start);

  window.__mindforge = { state, start, setStage, inject(target, confidence = 0.9, quality = 0.92) { applyAura(target, confidence, quality); } };
  const params = new URLSearchParams(location.search);
  if (params.get('autostart') === 'combat') { state.started = true; intro.classList.add('hidden'); setStage('combat'); state.sightUntil = 3.4; state.guardUntil = 2.2; }

  requestAnimationFrame(frame);
})();
