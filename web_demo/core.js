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
const fluxValue = document.getElementById('fluxValue');
const combatState = document.getElementById('combatState');
const fpsBadge = document.getElementById('fpsBadge');

const W = canvas.width;
const H = canvas.height;
const TAU = Math.PI * 2;
const SIM_HZ = 120;
const FIXED_DT = 1 / SIM_HZ;
const keys = new Set();
const justPressed = new Set();
const mouse = { x: W * 0.72, y: H * 0.48, active: false };

const cfg = {
  blueHz: 10,
  greenHz: 12,
  evidenceRate: 0.84,
  evidenceDecay: 0.64,
  evidenceThreshold: 0.86,
  sightDuration: 3.6,
  guardDuration: 3.6,
  sightDamageMultiplier: 1.58,
  guardHealPerSecond: 4.4,
  guardDamageReduction: 0.12,
  orbAngularSpeed: 0.92,
  orbRadiusX: 132,
  orbRadiusY: 64,
  playerAccel: 1850,
  playerMaxSpeed: 290,
  playerDrag: 9.5,
  dashSpeed: 860,
  dashDuration: 0.145,
  dashCooldown: 0.78,
  shotCooldown: 0.185,
  cleaveCooldown: 0.62,
  counterCooldown: 0.78,
  counterWindow: 0.18,
  bloomCooldown: 3.8,
  maxFlux: 3,
};

const state = {
  started: false,
  stage: 'intro',
  gameTime: 0,
  realTime: performance.now() / 1000,
  accumulator: 0,
  lastFrameMs: performance.now(),
  calibrationTarget: null,
  movingSelections: 0,
  blueEvidence: 0,
  greenEvidence: 0,
  sightUntil: 0,
  guardUntil: 0,
  previousAuraTarget: null,
  eventLog: [],
  particles: [],
  trails: [],
  shockwaves: [],
  damageTexts: [],
  projectiles: [],
  telegraphs: [],
  echoes: [],
  cameraShake: 0,
  flash: 0,
  chroma: 0,
  hitstopUntilReal: 0,
  bossStart: 0,
  frameCounter: 0,
  fpsWindowStart: performance.now(),
};

const player = {
  x: W * 0.25, y: H * 0.57, vx: 0, vy: 0, r: 21,
  hp: 100, maxHp: 100,
  flux: 0,
  lastShot: -99, lastCleave: -99, lastCounter: -99, lastDash: -99, lastBloom: -99,
  dashUntil: 0, invulnerableUntil: 0, parryUntil: 0,
  aimX: 1, aimY: 0,
};

const boss = {
  x: W * 0.72, y: H * 0.48, vx: 0, vy: 0, r: 64,
  hp: 1800, maxHp: 1800,
  poise: 120, maxPoise: 120,
  phase: 1, alive: false, staggerUntil: 0,
  nextAttackAt: 0, attackIndex: 0,
};

const construct = { x: W * 0.62, y: H * 0.47, r: 48 };
const arena = { cx: W * 0.52, cy: H * 0.53, rx: W * 0.44, ry: H * 0.40 };

const stars = Array.from({ length: 150 }, (_, i) => ({
  x: ((i * 7919) % 1000) / 1000 * W,
  y: ((i * 3571 + 113) % 1000) / 1000 * H,
  s: 0.4 + ((i * 97) % 100) / 100 * 1.6,
  p: ((i * 47) % 360) / 180 * Math.PI,
}));

const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
const lerp = (a, b, t) => a + (b - a) * t;
const hypot = Math.hypot;
const norm = (x, y) => { const m = Math.hypot(x, y) || 1; return [x / m, y / m]; };
const dist2 = (ax, ay, bx, by) => (ax - bx) ** 2 + (ay - by) ** 2;
const angleDelta = (a, b) => Math.atan2(Math.sin(a - b), Math.cos(a - b));
const activeSight = () => state.gameTime < state.sightUntil;
const activeGuard = () => state.gameTime < state.guardUntil;
const activeConcord = () => activeSight() && activeGuard();

function sweptCircleHit(x0, y0, x1, y1, cx, cy, radius) {
  const dx = x1 - x0, dy = y1 - y0;
  const len2 = dx * dx + dy * dy;
  let t = len2 > 0 ? ((cx - x0) * dx + (cy - y0) * dy) / len2 : 0;
  t = clamp(t, 0, 1);
  const px = x0 + dx * t, py = y0 + dy * t;
  return dist2(px, py, cx, cy) <= radius * radius;
}

class AudioDirector {
  constructor() { this.ac = null; this.enabled = true; }
  ensure() {
    if (!this.enabled) return null;
    if (!this.ac) this.ac = new (window.AudioContext || window.webkitAudioContext)();
    if (this.ac.state === 'suspended') this.ac.resume();
    return this.ac;
  }
  ping(freq, duration = 0.08, gain = 0.045, type = 'sine', slide = 1) {
    const ac = this.ensure(); if (!ac) return;
    const o = ac.createOscillator(), g = ac.createGain();
    o.type = type; o.frequency.setValueAtTime(freq, ac.currentTime); o.frequency.exponentialRampToValueAtTime(Math.max(30, freq * slide), ac.currentTime + duration);
    g.gain.setValueAtTime(gain, ac.currentTime); g.gain.exponentialRampToValueAtTime(0.0001, ac.currentTime + duration);
    o.connect(g).connect(ac.destination); o.start(); o.stop(ac.currentTime + duration);
  }
  shot() { this.ping(activeSight() ? 580 : 420, .055, .022, 'triangle', 1.5); }
  slash() { this.ping(activeSight() ? 190 : 145, .11, .04, 'sawtooth', 2.2); }
  dash() { this.ping(115, .09, .035, 'sine', 2.8); }
  parry() { this.ping(820, .14, .055, 'triangle', .52); }
  aura(target) { this.ping(target === 'sight' ? 660 : 390, .18, .05, 'sine', target === 'sight' ? 1.35 : .76); }
  bloom(concord) { this.ping(concord ? 92 : 120, .42, .07, 'sawtooth', concord ? 5 : 3); }
  hit(heavy = false) { this.ping(heavy ? 90 : 125, heavy ? .09 : .045, heavy ? .045 : .022, 'square', .7); }
}
const audio = new AudioDirector();

function setStage(stage) {
  state.stage = stage;
  state.blueEvidence = 0;
  state.greenEvidence = 0;
  if (stage === 'cal-sight') {
    state.calibrationTarget = 'sight'; neuralState.textContent = 'CALIBRATE SIGHT'; lastEvent.textContent = 'Attend blue'; combatState.textContent = 'WISP LINK';
  } else if (stage === 'cal-guard') {
    state.calibrationTarget = 'guard'; neuralState.textContent = 'CALIBRATE GUARD'; lastEvent.textContent = 'Attend green';
  } else if (stage === 'cal-moving') {
    state.calibrationTarget = 'sight'; state.movingSelections = 0; neuralState.textContent = 'MOVING VALIDATION'; lastEvent.textContent = 'Blue first';
  } else if (stage === 'combat') {
    state.calibrationTarget = null;
    boss.alive = true; boss.hp = boss.maxHp; boss.poise = boss.maxPoise; boss.phase = 1; boss.staggerUntil = 0; boss.nextAttackAt = state.gameTime + 0.7; boss.attackIndex = 0;
    player.hp = player.maxHp; player.flux = 0; state.bossStart = state.gameTime;
    state.projectiles.length = 0; state.echoes.length = 0; state.telegraphs.length = 0;
    neuralState.textContent = 'DUAL AURA READY'; lastEvent.textContent = 'Choose your aura'; combatState.textContent = 'PHASE I · PRESSURE';
  } else if (stage === 'win') {
    boss.alive = false; neuralState.textContent = 'SIGNAL RESTORED'; combatState.textContent = 'VICTORY'; state.projectiles.length = 0;
  } else if (stage === 'fail') {
    neuralState.textContent = 'GUARDIAN LOST'; combatState.textContent = 'DEFEAT';
  }
}

function recordEvent(type, target, confidence, quality, reason = null) {
  state.eventLog.push({ t: state.gameTime, type, target, confidence, quality, reason });
  if (state.eventLog.length > 160) state.eventLog.shift();
}

function particle(x, y, kind, count = 10, speed = 130) {
  for (let i = 0; i < count; i++) {
    const a = Math.random() * TAU, s = speed * (0.25 + Math.random());
    state.particles.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, life: .22 + Math.random() * .48, maxLife: .7, kind, size: 1.5 + Math.random() * 4 });
  }
}

function shockwave(x, y, kind, maxR = 120, life = .36, width = 4) {
  state.shockwaves.push({ x, y, kind, r: 4, maxR, life, maxLife: life, width });
}

function trail(x0, y0, x1, y1, kind, width = 5, life = .16) {
  state.trails.push({ x0, y0, x1, y1, kind, width, life, maxLife: life });
}

function damageText(x, y, text, kind = 'white') {
  state.damageTexts.push({ x, y, text, kind, life: .72, maxLife: .72, vy: -34 });
}

function shake(amount) { state.cameraShake = Math.max(state.cameraShake, amount); }
function flash(amount) { state.flash = Math.max(state.flash, amount); }
function hitstop(seconds) { state.hitstopUntilReal = Math.max(state.hitstopUntilReal, performance.now() / 1000 + seconds); }

function completeCalibrationSelection(target) {
  if (state.stage === 'cal-sight' && target === 'sight') {
    recordEvent('CAL_ACCEPT', 'sight', .94, .94); particle(construct.x, construct.y, 'blue', 42, 160); audio.aura('sight'); setStage('cal-guard'); return true;
  }
  if (state.stage === 'cal-guard' && target === 'guard') {
    recordEvent('CAL_ACCEPT', 'guard', .93, .94); particle(construct.x, construct.y, 'green', 42, 160); audio.aura('guard'); setStage('cal-moving'); return true;
  }
  if (state.stage === 'cal-moving' && target === state.calibrationTarget) {
    state.movingSelections += 1; recordEvent('CAL_MOVING_ACCEPT', target, .91, .92); particle(construct.x, construct.y, target === 'sight' ? 'blue' : 'green', 26, 145); audio.aura(target);
    if (state.movingSelections >= 4) setStage('combat');
    else { state.calibrationTarget = target === 'sight' ? 'guard' : 'sight'; lastEvent.textContent = state.calibrationTarget === 'sight' ? 'Now blue' : 'Now green'; }
    return true;
  }
  return false;
}

function applyAura(target, confidence = 1, quality = 1) {
  if (!state.started || state.stage === 'intro') return;
  if (confidence < .55 || quality < .55) {
    recordEvent('ABSTAIN', target, confidence, quality, 'QUALITY'); neuralState.textContent = 'ABSTAIN'; lastEvent.textContent = 'Signal uncertain'; return;
  }
  if (completeCalibrationSelection(target)) { state.blueEvidence = 0; state.greenEvidence = 0; return; }
  if (state.stage !== 'combat') return;

  const switched = state.previousAuraTarget && state.previousAuraTarget !== target;
  if (target === 'sight') {
    state.sightUntil = Math.max(state.sightUntil, state.gameTime + cfg.sightDuration); state.blueEvidence = 0;
    particle(boss.x, boss.y, 'blue', 38, 180); shockwave(player.x, player.y, 'blue', 105, .34, 4); neuralState.textContent = 'NEURAL SIGHT';
  } else if (target === 'guard') {
    state.guardUntil = Math.max(state.guardUntil, state.gameTime + cfg.guardDuration); state.greenEvidence = 0;
    particle(player.x, player.y, 'green', 38, 180); shockwave(player.x, player.y, 'green', 105, .34, 4); neuralState.textContent = 'NEURAL GUARD';
  }
  if (switched) gainFlux(.13, 'ATTENTION SWITCH');
  state.previousAuraTarget = target;
  lastEvent.textContent = `${target.toUpperCase()} ${confidence.toFixed(2)}`;
  recordEvent('AURA_SELECTED', target, confidence, quality); audio.aura(target);
}

window.addEventListener('mindforge-neural-event', (e) => {
  const d = e.detail || {};
  if (d.event === 'AURA_SELECTED') applyAura(d.target, Number(d.confidence ?? 0), Number(d.quality ?? 0));
  if (d.event === 'ABSTAIN') { neuralState.textContent = 'ABSTAIN'; lastEvent.textContent = d.reason || 'ABSTAIN'; recordEvent('ABSTAIN', null, Number(d.confidence ?? 0), Number(d.quality ?? 0), d.reason || null); }
});

function currentPrompt() {
  if (state.stage === 'cal-sight') return 'sight';
  if (state.stage === 'cal-guard') return 'guard';
  if (state.stage === 'cal-moving') return state.calibrationTarget;
  return null;
}

function updateEvidence(dt) {
  const b = keys.has('q'), g = keys.has('e');
  if (b && !g) { state.blueEvidence = clamp(state.blueEvidence + cfg.evidenceRate * dt, 0, 1); state.greenEvidence = clamp(state.greenEvidence - cfg.evidenceDecay * dt, 0, 1); }
  else if (g && !b) { state.greenEvidence = clamp(state.greenEvidence + cfg.evidenceRate * dt, 0, 1); state.blueEvidence = clamp(state.blueEvidence - cfg.evidenceDecay * dt, 0, 1); }
  else { state.blueEvidence = clamp(state.blueEvidence - cfg.evidenceDecay * dt, 0, 1); state.greenEvidence = clamp(state.greenEvidence - cfg.evidenceDecay * dt, 0, 1); }
  if (state.blueEvidence >= cfg.evidenceThreshold) applyAura('sight', .90, .92);
  if (state.greenEvidence >= cfg.evidenceThreshold) applyAura('guard', .90, .92);
}
