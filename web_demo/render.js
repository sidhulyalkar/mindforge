'use strict';

function targetCenter() { return state.stage === 'combat' ? boss : construct; }
function auraPosition(kind) {
  const center = targetCenter(); const moving = state.stage === 'cal-moving' || state.stage === 'combat';
  const phase = (moving ? state.realTime * cfg.orbAngularSpeed : 0) + (kind === 'guard' ? Math.PI : 0);
  return { x: center.x + Math.cos(phase) * cfg.orbRadiusX, y: center.y + Math.sin(phase) * cfg.orbRadiusY };
}

function colorFor(kind, alpha = 1) {
  const map = { blue: [74, 158, 255], green: [70, 242, 154], red: [255, 84, 111], violet: [179, 106, 255], white: [240, 243, 255], flux: [255, 200, 83], concord: [238, 113, 255] };
  const c = map[kind] || map.white; return `rgba(${c[0]},${c[1]},${c[2]},${alpha})`;
}

function drawBackground() {
  const g = ctx.createRadialGradient(W * .55, H * .42, 30, W * .55, H * .42, W * .7);
  g.addColorStop(0, '#171b38'); g.addColorStop(.45, '#090d1d'); g.addColorStop(1, '#03050c'); ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
  for (const s of stars) { const twinkle = .45 + .55 * Math.sin(state.realTime * .8 + s.p); ctx.fillStyle = `rgba(168,178,255,${.05 + twinkle * .15})`; ctx.fillRect(s.x, s.y, s.s, s.s); }

  ctx.save(); ctx.translate(arena.cx, arena.cy); ctx.scale(1, arena.ry / arena.rx);
  for (let r = 150; r <= arena.rx; r += 95) { ctx.strokeStyle = `rgba(128,139,224,${.09 - r / arena.rx * .04})`; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.arc(0, 0, r, 0, TAU); ctx.stroke(); }
  ctx.restore();

  ctx.strokeStyle = 'rgba(123,133,210,.075)'; ctx.lineWidth = 1;
  for (let x = 0; x < W; x += 72) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, H); ctx.stroke(); }
  for (let y = 0; y < H; y += 72) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke(); }
}

function drawTelegraphs() {
  for (const t of state.telegraphs) {
    if (t.fired) continue;
    const remaining = t.fireAt - state.gameTime, total = t.fireAt - t.created, q = 1 - clamp(remaining / total, 0, 1), pulse = .25 + q * .65;
    ctx.save(); ctx.strokeStyle = `rgba(255,99,126,${pulse})`; ctx.fillStyle = `rgba(255,74,105,${.035 + q * .07})`;
    if (t.type === 'lance') {
      const a = t.angle, len = 1600; ctx.lineWidth = t.width * (0.12 + q * .14); ctx.beginPath(); ctx.moveTo(boss.x, boss.y); ctx.lineTo(boss.x + Math.cos(a) * len, boss.y + Math.sin(a) * len); ctx.stroke();
    } else if (t.type === 'radial' || t.type === 'vortex') {
      ctx.lineWidth = 3 + q * 4; ctx.beginPath(); ctx.arc(boss.x, boss.y, 78 + q * 55, 0, TAU); ctx.stroke();
    } else if (t.type === 'echo-call') {
      ctx.lineWidth = 3; for (let i = 0; i < 3; i++) { const a = i / 3 * TAU + state.realTime; ctx.beginPath(); ctx.arc(boss.x + Math.cos(a) * 170, boss.y + Math.sin(a) * 115, 22 + q * 8, 0, TAU); ctx.stroke(); }
    } else {
      const a = Math.atan2(player.y - boss.y, player.x - boss.x); ctx.lineWidth = 3 + q * 3; ctx.beginPath(); ctx.moveTo(boss.x, boss.y); ctx.lineTo(boss.x + Math.cos(a) * 360, boss.y + Math.sin(a) * 360); ctx.stroke();
    }
    ctx.restore();
  }
}

function drawWisp() {
  const combat = state.stage === 'combat';
  const wx = combat ? player.x + Math.sin(state.realTime * 2.3) * 14 : player.x + 46 + Math.sin(state.realTime * 2.2) * 8;
  const wy = combat ? player.y - 54 + Math.cos(state.realTime * 1.8) * 8 : player.y - 44 + Math.cos(state.realTime * 1.9) * 8;
  ctx.save(); ctx.globalCompositeOperation = 'lighter';
  const glow = ctx.createRadialGradient(wx, wy, 0, wx, wy, 36); glow.addColorStop(0, 'rgba(250,250,255,.98)'); glow.addColorStop(.22, 'rgba(183,191,255,.65)'); glow.addColorStop(1, 'rgba(115,128,255,0)'); ctx.fillStyle = glow; ctx.beginPath(); ctx.arc(wx, wy, 36, 0, TAU); ctx.fill();
  ctx.strokeStyle = 'rgba(190,199,255,.32)'; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(player.x, player.y - 5); ctx.quadraticCurveTo((player.x + wx) / 2 + 12, (player.y + wy) / 2, wx, wy); ctx.stroke(); ctx.restore();
}

function drawAura(target, color, hz, pos, activeUntil, evidence) {
  const phase = Math.sin(TAU * hz * state.realTime); // real clock remains independent of gameplay hitstop
  const mod = .48 + .52 * (phase * .5 + .5); const active = state.gameTime < activeUntil; const prompted = currentPrompt() === target;
  const r = 25 + 10 * mod + (active ? 7 : 0) + (prompted ? 5 : 0); const kind = color;
  ctx.save(); ctx.globalCompositeOperation = 'lighter';
  const grad = ctx.createRadialGradient(pos.x, pos.y, 2, pos.x, pos.y, r * 2.2); grad.addColorStop(0, colorFor(kind, .9 * mod + .1)); grad.addColorStop(.3, colorFor(kind, .42 * mod)); grad.addColorStop(1, colorFor(kind, 0)); ctx.fillStyle = grad; ctx.beginPath(); ctx.arc(pos.x, pos.y, r * 2.2, 0, TAU); ctx.fill();
  ctx.strokeStyle = prompted ? 'rgba(255,255,255,.98)' : colorFor(kind, .58 + .36 * mod); ctx.lineWidth = prompted ? 6 : active ? 5 : 3; ctx.beginPath(); ctx.arc(pos.x, pos.y, r, 0, TAU); ctx.stroke();
  ctx.strokeStyle = 'rgba(255,255,255,.92)'; ctx.lineWidth = 2;
  if (target === 'sight') { ctx.beginPath(); ctx.moveTo(pos.x, pos.y - 14); ctx.lineTo(pos.x + 12, pos.y + 10); ctx.lineTo(pos.x - 12, pos.y + 10); ctx.closePath(); ctx.stroke(); }
  else { ctx.beginPath(); ctx.arc(pos.x, pos.y, 11, 0, TAU); ctx.stroke(); ctx.beginPath(); ctx.moveTo(pos.x - 7, pos.y); ctx.lineTo(pos.x + 7, pos.y); ctx.moveTo(pos.x, pos.y - 7); ctx.lineTo(pos.x, pos.y + 7); ctx.stroke(); }
  ctx.restore();
  const center = targetCenter(); const [lx, ly] = norm(pos.x - center.x, pos.y - center.y); const labelX = pos.x + lx * 22; const labelY = pos.y + ly * 22 + (ly >= 0 ? 36 : -28);
  ctx.fillStyle = '#e9ecff'; ctx.font = '650 13px system-ui'; ctx.textAlign = 'center'; ctx.fillText(target === 'sight' ? `SIGHT · ${hz} Hz` : `GUARD · ${hz} Hz`, labelX, labelY);
  if (evidence > .01) { const barY = labelY + 8; ctx.fillStyle = 'rgba(255,255,255,.13)'; ctx.fillRect(labelX - 31, barY, 62, 5); ctx.fillStyle = colorFor(kind, 1); ctx.fillRect(labelX - 31, barY, 62 * evidence, 5); }
}

function drawConstruct() {
  if (!state.stage.startsWith('cal-')) return;
  const pulse = .5 + .5 * Math.sin(state.realTime * 1.6); ctx.fillStyle = `rgba(95,101,168,${.46 + pulse * .12})`; ctx.beginPath(); ctx.arc(construct.x, construct.y, construct.r, 0, TAU); ctx.fill(); ctx.strokeStyle = 'rgba(215,220,255,.28)'; ctx.lineWidth = 4; ctx.beginPath(); ctx.arc(construct.x, construct.y, construct.r + 8, 0, TAU); ctx.stroke();
}

function drawEchoes() {
  for (const e of state.echoes) {
    if (!e.alive) continue;
    const stunned = state.gameTime < e.stunUntil;
    ctx.save(); ctx.translate(e.x, e.y); ctx.rotate(state.realTime * 1.7 + e.orbitPhase); ctx.fillStyle = stunned ? 'rgba(215,205,255,.82)' : 'rgba(147,91,225,.82)';
    ctx.beginPath(); for (let i = 0; i < 6; i++) { const a = i / 6 * TAU; const r = i % 2 ? e.r * .72 : e.r; const x = Math.cos(a) * r, y = Math.sin(a) * r; if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y); } ctx.closePath(); ctx.fill();
    ctx.strokeStyle = 'rgba(235,225,255,.5)'; ctx.lineWidth = 2; ctx.stroke(); ctx.restore();
  }
}

function drawBoss() {
  if (!boss.alive) return;
  const pulse = .5 + .5 * Math.sin(state.realTime * (2 + boss.phase)); const staggered = state.gameTime < boss.staggerUntil;
  ctx.save(); ctx.translate(boss.x, boss.y); ctx.rotate(state.realTime * .18);
  const glow = ctx.createRadialGradient(0, 0, 12, 0, 0, boss.r * 1.8); glow.addColorStop(0, staggered ? 'rgba(238,221,255,.5)' : 'rgba(166,98,240,.42)'); glow.addColorStop(1, 'rgba(110,64,220,0)'); ctx.fillStyle = glow; ctx.beginPath(); ctx.arc(0, 0, boss.r * 1.8, 0, TAU); ctx.fill();
  ctx.fillStyle = staggered ? '#c9c3de' : ['#000', '#633b9d', '#7443ae', '#853f9e'][boss.phase]; ctx.beginPath(); ctx.arc(0, 0, boss.r, 0, TAU); ctx.fill();
  ctx.strokeStyle = staggered ? 'rgba(255,255,255,.75)' : `rgba(218,190,255,${.34 + .34 * pulse})`; ctx.lineWidth = 6; ctx.beginPath(); ctx.arc(0, 0, boss.r + 10 + pulse * 6, 0, TAU); ctx.stroke();
  for (let i = 0; i < 6; i++) { const a = i / 6 * TAU; ctx.strokeStyle = 'rgba(217,196,255,.22)'; ctx.lineWidth = 3; ctx.beginPath(); ctx.moveTo(Math.cos(a) * (boss.r + 10), Math.sin(a) * (boss.r + 10)); ctx.lineTo(Math.cos(a) * (boss.r + 34), Math.sin(a) * (boss.r + 34)); ctx.stroke(); }
  ctx.rotate(-state.realTime * .18); ctx.strokeStyle = 'rgba(240,230,255,.72)'; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(-12, 0); ctx.lineTo(0, -12); ctx.lineTo(12, 0); ctx.lineTo(0, 12); ctx.closePath(); ctx.stroke(); ctx.restore();
}

function drawPlayer() {
  const speed = Math.hypot(player.vx, player.vy); const dash = state.gameTime < player.dashUntil;
  ctx.save(); ctx.translate(player.x, player.y); const a = Math.atan2(player.aimY, player.aimX); ctx.rotate(a);
  if (activeGuard()) { ctx.strokeStyle = colorFor('green', .58); ctx.lineWidth = 4; ctx.beginPath(); ctx.arc(0, 0, 31 + Math.sin(state.realTime * 4) * 2, 0, TAU); ctx.stroke(); }
  if (activeSight()) { ctx.strokeStyle = colorFor('blue', .5); ctx.lineWidth = 2; ctx.beginPath(); ctx.arc(0, 0, 25, -1.1, 1.1); ctx.stroke(); }
  ctx.fillStyle = dash ? '#ffffff' : '#e9ecff'; ctx.beginPath(); ctx.arc(0, 0, player.r, 0, TAU); ctx.fill();
  ctx.fillStyle = activeSight() ? '#65aaff' : '#7c86d5'; ctx.beginPath(); ctx.moveTo(8, -11); ctx.lineTo(31 + Math.min(10, speed * .02), 0); ctx.lineTo(8, 11); ctx.closePath(); ctx.fill();
  if (state.gameTime < player.parryUntil) { ctx.strokeStyle = 'rgba(255,255,255,.8)'; ctx.lineWidth = 3; ctx.beginPath(); ctx.arc(0, 0, 53, -.8, .8); ctx.stroke(); }
  ctx.restore();
}

function drawProjectiles() {
  for (const p of state.projectiles) {
    const kind = p.trailKind || (p.team === 'enemy' ? 'red' : 'white'); ctx.save(); ctx.globalCompositeOperation = 'lighter'; const g = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.r * 2.6); g.addColorStop(0, colorFor(kind, .95)); g.addColorStop(1, colorFor(kind, 0)); ctx.fillStyle = g; ctx.beginPath(); ctx.arc(p.x, p.y, p.r * 2.6, 0, TAU); ctx.fill(); ctx.fillStyle = colorFor(kind, 1); ctx.beginPath(); ctx.arc(p.x, p.y, p.r, 0, TAU); ctx.fill(); ctx.restore();
  }
}

function drawFx() {
  ctx.save(); ctx.globalCompositeOperation = 'lighter';
  for (const t of state.trails) { const a = clamp(t.life / t.maxLife, 0, 1); ctx.strokeStyle = colorFor(t.kind, a * .58); ctx.lineWidth = t.width * a; ctx.lineCap = 'round'; ctx.beginPath(); ctx.moveTo(t.x0, t.y0); ctx.lineTo(t.x1, t.y1); ctx.stroke(); }
  for (const p of state.particles) { const a = clamp(p.life / p.maxLife, 0, 1); ctx.fillStyle = colorFor(p.kind, a); ctx.beginPath(); ctx.arc(p.x, p.y, p.size * a, 0, TAU); ctx.fill(); }
  ctx.restore();
  for (const s of state.shockwaves) { const a = clamp(s.life / s.maxLife, 0, 1); ctx.strokeStyle = colorFor(s.kind, a * .72); ctx.lineWidth = s.width * a; ctx.beginPath(); if (s.arcStart !== undefined) ctx.arc(s.x, s.y, s.r, s.arcStart, s.arcEnd); else ctx.arc(s.x, s.y, s.r, 0, TAU); ctx.stroke(); }
  for (const d of state.damageTexts) { const a = clamp(d.life / d.maxLife, 0, 1); ctx.fillStyle = colorFor(d.kind, a); ctx.font = `800 ${d.kind === 'flux' ? 12 : 15}px system-ui`; ctx.textAlign = 'center'; ctx.fillText(d.text, d.x, d.y); }
}

function drawBloom() {
  if (!state.bloom) return;
  const b = state.bloom, q = clamp((state.gameTime - b.start) / (b.end - b.start), 0, 1), r = lerp(35, b.concord ? 180 : 145, q);
  ctx.save(); ctx.globalCompositeOperation = 'lighter'; ctx.strokeStyle = colorFor(b.concord ? 'concord' : 'violet', .62); ctx.lineWidth = 4 + q * 5; ctx.beginPath(); ctx.arc(b.x, b.y, r, 0, TAU); ctx.stroke(); ctx.strokeStyle = colorFor(activeGuard() ? 'green' : 'blue', .35); ctx.beginPath(); ctx.arc(b.x, b.y, r * .63, 0, TAU); ctx.stroke(); ctx.restore();
}

function drawHud() {
  ctx.textAlign = 'left'; ctx.font = '750 15px system-ui'; ctx.fillStyle = '#eef0ff'; ctx.fillText('GUARDIAN', 34, 37);
  ctx.fillStyle = 'rgba(255,255,255,.12)'; ctx.fillRect(34, 48, 250, 12); ctx.fillStyle = '#55efa5'; ctx.fillRect(34, 48, 250 * player.hp / player.maxHp, 12); ctx.fillStyle = '#eef0ff'; ctx.fillText(`HP ${Math.ceil(player.hp)}`, 34, 82);

  ctx.fillStyle = 'rgba(255,255,255,.12)'; ctx.fillRect(34, 94, 250, 8); ctx.fillStyle = '#ffc853'; ctx.fillRect(34, 94, 250 * (player.flux / cfg.maxFlux), 8); ctx.fillStyle = '#ffc853'; ctx.font = '700 12px system-ui'; ctx.fillText(`FLUX ${player.flux.toFixed(2)} / ${cfg.maxFlux}`, 34, 119);

  ctx.textAlign = 'center';
  if (state.stage === 'combat') {
    ctx.fillStyle = '#f2edff'; ctx.font = '800 15px system-ui'; ctx.fillText(`FRACTURED SIGNAL · PHASE ${boss.phase}`, W / 2, 32);
    ctx.fillStyle = 'rgba(255,255,255,.11)'; ctx.fillRect(W / 2 - 245, 44, 490, 11); ctx.fillStyle = '#ad77ff'; ctx.fillRect(W / 2 - 245, 44, 490 * boss.hp / boss.maxHp, 11);
    ctx.fillStyle = 'rgba(255,255,255,.08)'; ctx.fillRect(W / 2 - 170, 61, 340, 5); ctx.fillStyle = '#e0ccff'; ctx.fillRect(W / 2 - 170, 61, 340 * boss.poise / boss.maxPoise, 5);
  } else if (state.stage.startsWith('cal-')) { ctx.fillStyle = '#f2edff'; ctx.font = '800 15px system-ui'; ctx.fillText('WISP ATTUNEMENT', W / 2, 35); }
  else if (state.stage === 'win') { ctx.fillStyle = '#f2edff'; ctx.font = '900 22px system-ui'; ctx.fillText('SIGNAL RESTORED', W / 2, 42); }

  const s = Math.max(0, state.sightUntil - state.gameTime), g = Math.max(0, state.guardUntil - state.gameTime);
  ctx.textAlign = 'right'; ctx.font = '750 14px system-ui'; ctx.fillStyle = s > 0 ? '#61acff' : '#777d9f'; ctx.fillText(`SIGHT ×${cfg.sightDamageMultiplier.toFixed(2)}  ${s.toFixed(1)}s`, W - 34, 37); ctx.fillStyle = g > 0 ? '#5cf2a5' : '#777d9f'; ctx.fillText(`GUARD +${cfg.guardHealPerSecond.toFixed(1)} HP/s  ${g.toFixed(1)}s`, W - 34, 60);
  if (activeConcord()) { ctx.fillStyle = '#ee88ff'; ctx.font = '900 13px system-ui'; ctx.fillText('CONCORD · TWIN ECLIPSE ARMED', W - 34, 84); }

  if (state.stage === 'combat') {
    const abilityY = H - 30; const items = [
      ['SPACE', 'SHOT', Math.max(0, cfg.shotCooldown - (state.gameTime - player.lastShot)), cfg.shotCooldown],
      ['F', 'CLEAVE', Math.max(0, cfg.cleaveCooldown - (state.gameTime - player.lastCleave)), cfg.cleaveCooldown],
      ['C', 'COUNTER', Math.max(0, cfg.counterCooldown - (state.gameTime - player.lastCounter)), cfg.counterCooldown],
      ['SHIFT', 'DASH', Math.max(0, cfg.dashCooldown - (state.gameTime - player.lastDash)), cfg.dashCooldown],
      ['R', activeConcord() ? 'TWIN ECLIPSE' : 'GRAVITY BLOOM', player.flux >= cfg.maxFlux ? 0 : 1, 1],
    ];
    ctx.textAlign = 'center'; ctx.font = '650 11px system-ui';
    items.forEach((it, i) => { const x = W / 2 + (i - 2) * 126; const ready = it[2] <= 0; ctx.fillStyle = ready ? 'rgba(19,25,48,.9)' : 'rgba(8,10,21,.68)'; ctx.fillRect(x - 55, abilityY - 32, 110, 34); ctx.strokeStyle = ready ? 'rgba(178,188,255,.42)' : 'rgba(100,106,150,.18)'; ctx.strokeRect(x - 55, abilityY - 32, 110, 34); ctx.fillStyle = ready ? '#e8ebff' : '#676e96'; ctx.fillText(`${it[0]} · ${it[1]}`, x, abilityY - 13); });
  }
}

function drawCalibrationInstruction() {
  if (!state.stage.startsWith('cal-')) return;
  const prompt = currentPrompt(); let subtitle = '';
  if (state.stage === 'cal-sight') subtitle = 'Hold Q to simulate attending BLUE / Sight.';
  if (state.stage === 'cal-guard') subtitle = 'Hold E to simulate attending GREEN / Guard.';
  if (state.stage === 'cal-moving') subtitle = `${state.movingSelections}/4 moving selections · ${prompt === 'sight' ? 'BLUE / Q' : 'GREEN / E'} next`;
  ctx.textAlign = 'center'; ctx.fillStyle = 'rgba(7,9,20,.78)'; ctx.fillRect(W / 2 - 360, H - 118, 720, 76); ctx.fillStyle = '#f4f5ff'; ctx.font = '850 18px system-ui'; ctx.fillText('The Forge is learning your Wisp.', W / 2, H - 88); ctx.fillStyle = '#b9bfdc'; ctx.font = '500 14px system-ui'; ctx.fillText(subtitle, W / 2, H - 61);
}

function render() {
  const shakeX = (Math.random() - .5) * state.cameraShake, shakeY = (Math.random() - .5) * state.cameraShake;
  ctx.save(); ctx.translate(shakeX, shakeY);
  drawBackground(); drawTelegraphs(); drawWisp(); drawConstruct(); drawBoss(); drawEchoes();
  if (state.stage.startsWith('cal-') || state.stage === 'combat') {
    drawAura('sight', 'blue', cfg.blueHz, auraPosition('sight'), state.sightUntil, state.blueEvidence);
    drawAura('guard', 'green', cfg.greenHz, auraPosition('guard'), state.guardUntil, state.greenEvidence);
  }
  drawBloom(); drawProjectiles(); drawPlayer(); drawFx(); drawHud(); drawCalibrationInstruction();
  ctx.restore();
  if (state.flash > .005) { ctx.fillStyle = `rgba(255,245,255,${clamp(state.flash, 0, .34)})`; ctx.fillRect(0, 0, W, H); }
  const vignette = ctx.createRadialGradient(W / 2, H / 2, H * .22, W / 2, H / 2, H * .72); vignette.addColorStop(0, 'rgba(0,0,0,0)'); vignette.addColorStop(1, 'rgba(0,0,0,.52)'); ctx.fillStyle = vignette; ctx.fillRect(0, 0, W, H);
}

function updateTelemetry() {
  blueMeter.value = state.blueEvidence; greenMeter.value = state.greenEvidence; blueValue.textContent = state.blueEvidence.toFixed(2); greenValue.textContent = state.greenEvidence.toFixed(2); fluxValue.textContent = `${player.flux.toFixed(2)} / ${cfg.maxFlux}`;
  if (state.stage === 'combat' && activeConcord()) combatState.textContent = 'CONCORD · DUAL AURA OVERLAP';
  else if (state.stage === 'combat' && state.gameTime < boss.staggerUntil) combatState.textContent = 'SIGNAL BREAK';
  else if (state.stage === 'combat') combatState.textContent = boss.phase === 1 ? 'PHASE I · PRESSURE' : boss.phase === 2 ? 'PHASE II · ATTRITION' : 'PHASE III · INTERFERENCE';
}
