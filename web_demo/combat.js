'use strict';

function gainFlux(amount, label = '') {
  const before = player.flux;
  player.flux = clamp(player.flux + amount, 0, cfg.maxFlux);
  if (player.flux > before + .01 && label) { damageText(player.x, player.y - 34, `+${amount.toFixed(2)} FLUX`, 'flux'); lastEvent.textContent = label; }
}

function aimVector() {
  let dx = mouse.active ? mouse.x - player.x : boss.x - player.x;
  let dy = mouse.active ? mouse.y - player.y : boss.y - player.y;
  if (Math.hypot(dx, dy) < 2) { dx = player.aimX; dy = player.aimY; }
  const [nx, ny] = norm(dx, dy); player.aimX = nx; player.aimY = ny; return [nx, ny];
}

function spawnProjectile(p) {
  state.projectiles.push({
    x: p.x, y: p.y, px: p.x, py: p.y,
    vx: p.vx, vy: p.vy, r: p.r ?? 6, life: p.life ?? 4,
    team: p.team ?? 'enemy', damage: p.damage ?? 10, poise: p.poise ?? 0,
    kind: p.kind ?? 'bolt', pierce: p.pierce ?? 0, reflected: p.reflected ?? false,
    nearMissed: false, curve: p.curve ?? 0, homing: p.homing ?? 0,
    trailKind: p.trailKind ?? (p.team === 'player' ? 'white' : 'red'),
  });
}

function pulseShot() {
  if (state.stage !== 'combat' || !boss.alive || state.gameTime - player.lastShot < cfg.shotCooldown) return;
  player.lastShot = state.gameTime;
  const [ax, ay] = aimVector();
  const sight = activeSight();
  const speed = sight ? 980 : 790;
  spawnProjectile({ x: player.x + ax * 25, y: player.y + ay * 25, vx: ax * speed + player.vx * .15, vy: ay * speed + player.vy * .15, r: sight ? 6.5 : 5, life: 1.8, team: 'player', damage: sight ? 19 : 13, poise: sight ? 8 : 5, pierce: sight ? 1 : 0, kind: sight ? 'sight-shot' : 'shot', trailKind: sight ? 'blue' : 'white' });
  player.vx -= ax * 18; player.vy -= ay * 18;
  audio.shot();
}

function damageBoss(amount, poise, ix = 0, iy = 0, heavy = false) {
  if (!boss.alive || state.gameTime < boss.staggerUntil - .9) return;
  const mult = activeSight() ? cfg.sightDamageMultiplier : 1;
  const dealt = amount * mult;
  boss.hp = Math.max(0, boss.hp - dealt);
  boss.poise = Math.max(0, boss.poise - poise);
  boss.vx += ix; boss.vy += iy;
  damageText(boss.x + (Math.random() - .5) * 25, boss.y - 56, Math.round(dealt), activeSight() ? 'blue' : 'white');
  particle(boss.x, boss.y, activeSight() ? 'blue' : 'white', heavy ? 16 : 7, heavy ? 210 : 130);
  if (heavy) { shake(10); hitstop(.055); flash(.12); audio.hit(true); } else audio.hit(false);
  if (boss.poise <= 0 && state.gameTime >= boss.staggerUntil) staggerBoss();
  if (boss.hp <= 0) { boss.alive = false; particle(boss.x, boss.y, 'blue', 120, 260); shockwave(boss.x, boss.y, 'concord', 330, .8, 8); setStage('win'); }
}

function damageEcho(e, amount, poise, ix = 0, iy = 0) {
  if (!e.alive) return;
  const mult = activeSight() ? cfg.sightDamageMultiplier : 1;
  e.hp -= amount * mult; e.vx += ix; e.vy += iy; e.poise -= poise;
  particle(e.x, e.y, activeSight() ? 'blue' : 'violet', 8, 140);
  if (e.poise <= 0) { e.stunUntil = state.gameTime + .65; e.poise = e.maxPoise; }
  if (e.hp <= 0) { e.alive = false; gainFlux(.35, 'ECHO SHATTER'); particle(e.x, e.y, 'violet', 30, 190); shockwave(e.x, e.y, 'violet', 90, .35, 3); }
}

function riftCleave() {
  if (state.stage !== 'combat' || state.gameTime - player.lastCleave < cfg.cleaveCooldown) return;
  player.lastCleave = state.gameTime;
  const [ax, ay] = aimVector();
  const aimA = Math.atan2(ay, ax);
  const range = activeSight() ? 138 : 116;
  const arc = activeSight() ? 1.35 : 1.12;
  let hit = false;
  const attackEntity = (e, bossEntity) => {
    const dx = e.x - player.x, dy = e.y - player.y, d = Math.hypot(dx, dy);
    if (d > range + e.r) return;
    const a = Math.atan2(dy, dx);
    if (Math.abs(angleDelta(a, aimA)) > arc / 2) return;
    const [nx, ny] = norm(dx, dy);
    if (bossEntity) damageBoss(29, activeSight() ? 34 : 28, nx * 115, ny * 115, true);
    else damageEcho(e, 34, 28, nx * 190, ny * 190);
    hit = true;
  };
  if (boss.alive) attackEntity(boss, true);
  state.echoes.forEach((e) => e.alive && attackEntity(e, false));
  state.shockwaves.push({ x: player.x, y: player.y, kind: activeSight() ? 'blue' : 'white', r: 18, maxR: range, life: .18, maxLife: .18, width: 8, arcStart: aimA - arc / 2, arcEnd: aimA + arc / 2 });
  player.vx -= ax * 70; player.vy -= ay * 70;
  if (hit) gainFlux(.08, 'CLEAVE IMPACT');
  audio.slash();
}

function counterPulse() {
  if (state.stage !== 'combat' || state.gameTime - player.lastCounter < cfg.counterCooldown) return;
  player.lastCounter = state.gameTime;
  player.parryUntil = state.gameTime + cfg.counterWindow;
  shockwave(player.x, player.y, activeGuard() ? 'green' : 'white', 82, cfg.counterWindow, 3);
  audio.ping(310, .06, .025, 'triangle', 1.7);
}

function phaseDash() {
  if (state.stage !== 'combat' || state.gameTime - player.lastDash < cfg.dashCooldown) return;
  player.lastDash = state.gameTime;
  let dx = (keys.has('d') ? 1 : 0) - (keys.has('a') ? 1 : 0);
  let dy = (keys.has('s') ? 1 : 0) - (keys.has('w') ? 1 : 0);
  if (!dx && !dy) [dx, dy] = aimVector(); else [dx, dy] = norm(dx, dy);
  player.vx = dx * cfg.dashSpeed; player.vy = dy * cfg.dashSpeed;
  player.dashUntil = state.gameTime + cfg.dashDuration;
  player.invulnerableUntil = player.dashUntil;
  particle(player.x, player.y, activeGuard() ? 'green' : activeSight() ? 'blue' : 'white', 18, 180);
  shake(4); audio.dash();
}

function gravityBloom() {
  if (state.stage !== 'combat' || player.flux < cfg.maxFlux - 1e-6 || state.gameTime - player.lastBloom < cfg.bloomCooldown) return;
  player.lastBloom = state.gameTime;
  player.flux = 0;
  const concord = activeConcord();
  state.bloom = { x: player.x, y: player.y, start: state.gameTime, end: state.gameTime + (concord ? 1.0 : .82), captured: [], concord };
  shockwave(player.x, player.y, concord ? 'concord' : activeSight() ? 'blue' : 'green', concord ? 270 : 220, .65, concord ? 8 : 5);
  particle(player.x, player.y, concord ? 'concord' : 'violet', concord ? 70 : 42, 210);
  shake(concord ? 12 : 8); hitstop(concord ? .085 : .055); audio.bloom(concord);
}

function detonateBloom(bloom) {
  const count = Math.max(5, bloom.captured.length + (bloom.concord ? 6 : 2));
  for (let i = 0; i < count; i++) {
    const lead = 0.16;
    const tx = boss.x + boss.vx * lead, ty = boss.y + boss.vy * lead;
    let [dx, dy] = norm(tx - bloom.x, ty - bloom.y);
    const spread = (i - (count - 1) / 2) * (bloom.concord ? .035 : .06);
    const c = Math.cos(spread), s = Math.sin(spread), rx = dx * c - dy * s, ry = dx * s + dy * c;
    spawnProjectile({ x: bloom.x, y: bloom.y, vx: rx * (bloom.concord ? 930 : 790), vy: ry * (bloom.concord ? 930 : 790), r: bloom.concord ? 7 : 5.5, life: 1.8, team: 'player', damage: bloom.concord ? 34 : 23, poise: bloom.concord ? 16 : 10, pierce: bloom.concord ? 2 : 0, reflected: true, kind: bloom.concord ? 'eclipse' : 'reflected', trailKind: bloom.concord ? 'concord' : 'violet' });
  }
  if (bloom.concord) damageBoss(52, 36, 0, 0, true);
  shockwave(bloom.x, bloom.y, bloom.concord ? 'concord' : 'violet', bloom.concord ? 340 : 260, .56, 8);
  flash(bloom.concord ? .32 : .18); shake(bloom.concord ? 15 : 10);
}

function staggerBoss() {
  boss.poise = boss.maxPoise;
  boss.staggerUntil = state.gameTime + 1.15;
  boss.vx *= .25; boss.vy *= .25;
  gainFlux(.5, 'SIGNAL BREAK');
  shockwave(boss.x, boss.y, 'violet', 180, .46, 6); particle(boss.x, boss.y, 'violet', 46, 220);
  hitstop(.075); shake(14); flash(.2); combatState.textContent = 'SIGNAL BREAK';
}

function spawnEnemyProjectile(angle, speed, kind = 'enemy', opts = {}) {
  spawnProjectile({ x: boss.x + Math.cos(angle) * 72, y: boss.y + Math.sin(angle) * 72, vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed, r: opts.r ?? 8, life: opts.life ?? 5, team: 'enemy', damage: opts.damage ?? 10, kind, curve: opts.curve ?? 0, homing: opts.homing ?? 0, trailKind: kind === 'void' ? 'violet' : 'red' });
}

function scheduleTelegraph(type, delay, data = {}) {
  state.telegraphs.push({ type, created: state.gameTime, fireAt: state.gameTime + delay, fired: false, ...data });
}

function bossAttackPattern() {
  if (!boss.alive || state.gameTime < boss.staggerUntil) return;
  boss.attackIndex++;
  const p = boss.phase;
  const index = boss.attackIndex % (p === 1 ? 3 : p === 2 ? 4 : 5);
  if (index === 0) scheduleTelegraph('aim-fan', .56 - p * .05, { count: 2 + p, spread: .20 + p * .035 });
  else if (index === 1) scheduleTelegraph('radial', .62 - p * .05, { count: 10 + p * 3, curve: p >= 2 ? .38 : 0 });
  else if (index === 2) scheduleTelegraph('lance', .48, { angle: Math.atan2(player.y - boss.y, player.x - boss.x), width: 38 + p * 4 });
  else if (index === 3) scheduleTelegraph('echo-call', .7, { count: p });
  else scheduleTelegraph('vortex', .72, { count: 14 });
  boss.nextAttackAt = state.gameTime + [0, 1.35, 1.05, .82][p];
}

function fireTelegraph(t) {
  t.fired = true;
  if (t.type === 'aim-fan') {
    const center = Math.atan2(player.y - boss.y, player.x - boss.x);
    for (let i = 0; i < t.count; i++) {
      const a = center + (i - (t.count - 1) / 2) * t.spread;
      spawnEnemyProjectile(a, 330 + boss.phase * 38, 'needle', { r: 7, homing: boss.phase === 3 ? .35 : 0 });
    }
  } else if (t.type === 'radial') {
    for (let i = 0; i < t.count; i++) spawnEnemyProjectile(i / t.count * TAU + state.gameTime * .3, 245 + boss.phase * 22, 'petal', { r: 7, curve: (i % 2 ? 1 : -1) * t.curve });
  } else if (t.type === 'lance') {
    const a = t.angle;
    for (let i = -1; i <= 1; i++) spawnEnemyProjectile(a + i * .045, 560, 'void', { r: 9, damage: 14, life: 3.2 });
    shake(6);
  } else if (t.type === 'echo-call') {
    const alive = state.echoes.filter((e) => e.alive).length;
    for (let i = alive; i < Math.min(3, alive + t.count); i++) {
      const a = i / 3 * TAU + state.gameTime;
      state.echoes.push({ x: boss.x + Math.cos(a) * 160, y: boss.y + Math.sin(a) * 110, vx: 0, vy: 0, r: 20, hp: 95, maxHp: 95, poise: 38, maxPoise: 38, stunUntil: 0, alive: true, fireAt: state.gameTime + .7 + i * .2, orbitPhase: a });
    }
  } else if (t.type === 'vortex') {
    for (let i = 0; i < t.count; i++) spawnEnemyProjectile(i / t.count * TAU, 210, 'void', { r: 8, curve: (i % 2 ? .7 : -.7), homing: .18, life: 6 });
  }
}

function updateTelegraphs() {
  for (const t of state.telegraphs) if (!t.fired && state.gameTime >= t.fireAt) fireTelegraph(t);
  state.telegraphs = state.telegraphs.filter((t) => state.gameTime - t.fireAt < .35);
}

function updateEchoes(dt) {
  for (const e of state.echoes) {
    if (!e.alive) continue;
    const targetPhase = e.orbitPhase + state.gameTime * .42;
    const tx = boss.x + Math.cos(targetPhase) * 175;
    const ty = boss.y + Math.sin(targetPhase) * 116;
    const ax = (tx - e.x) * 6 - e.vx * 4.5, ay = (ty - e.y) * 6 - e.vy * 4.5;
    e.vx += ax * dt; e.vy += ay * dt; e.x += e.vx * dt; e.y += e.vy * dt;
    if (state.gameTime >= e.fireAt && state.gameTime >= e.stunUntil) {
      e.fireAt = state.gameTime + 1.45 - boss.phase * .12;
      const a = Math.atan2(player.y - e.y, player.x - e.x);
      spawnProjectile({ x: e.x, y: e.y, vx: Math.cos(a) * 300, vy: Math.sin(a) * 300, r: 6.5, life: 4, team: 'enemy', damage: 8, kind: 'echo', trailKind: 'violet' });
    }
  }
  state.echoes = state.echoes.filter((e) => e.alive || e.hp > -100);
}

function updatePlayer(dt) {
  if (state.stage !== 'combat') return;
  let ix = (keys.has('d') ? 1 : 0) - (keys.has('a') ? 1 : 0);
  let iy = (keys.has('s') ? 1 : 0) - (keys.has('w') ? 1 : 0);
  if (ix || iy) [ix, iy] = norm(ix, iy);
  if (state.gameTime >= player.dashUntil) {
    player.vx += ix * cfg.playerAccel * dt; player.vy += iy * cfg.playerAccel * dt;
    const drag = Math.exp(-cfg.playerDrag * dt); player.vx *= drag; player.vy *= drag;
    const speed = Math.hypot(player.vx, player.vy);
    if (speed > cfg.playerMaxSpeed) { player.vx = player.vx / speed * cfg.playerMaxSpeed; player.vy = player.vy / speed * cfg.playerMaxSpeed; }
  } else {
    trail(player.x - player.vx * dt * 2, player.y - player.vy * dt * 2, player.x, player.y, activeGuard() ? 'green' : activeSight() ? 'blue' : 'white', 12, .12);
  }
  player.x += player.vx * dt; player.y += player.vy * dt;
  player.x = clamp(player.x, 42, W - 42); player.y = clamp(player.y, 62, H - 46);
  if (activeGuard()) player.hp = Math.min(player.maxHp, player.hp + cfg.guardHealPerSecond * dt);
  if (keys.has(' ')) pulseShot();
  if (justPressed.has('f')) riftCleave();
  if (justPressed.has('c')) counterPulse();
  if (justPressed.has('shift')) phaseDash();
  if (justPressed.has('r')) gravityBloom();
  aimVector();
}

function updateBoss(dt) {
  if (state.stage !== 'combat' || !boss.alive) return;
  const oldPhase = boss.phase;
  boss.phase = boss.hp > boss.maxHp * .68 ? 1 : boss.hp > boss.maxHp * .34 ? 2 : 3;
  if (boss.phase !== oldPhase) {
    shockwave(boss.x, boss.y, 'violet', 250, .65, 8); particle(boss.x, boss.y, 'violet', 55, 230); flash(.16); shake(10);
    combatState.textContent = boss.phase === 2 ? 'PHASE II · ATTRITION' : 'PHASE III · INTERFERENCE';
  }
  if (state.gameTime < boss.staggerUntil) {
    boss.vx *= Math.exp(-10 * dt); boss.vy *= Math.exp(-10 * dt);
    boss.x += boss.vx * dt; boss.y += boss.vy * dt;
    return;
  }
  const anchorX = W * (.70 + Math.sin(state.gameTime * .37) * .035);
  const anchorY = H * (.48 + Math.cos(state.gameTime * .29) * .055);
  boss.vx += ((anchorX - boss.x) * 1.45 - boss.vx * 2.1) * dt;
  boss.vy += ((anchorY - boss.y) * 1.45 - boss.vy * 2.1) * dt;
  boss.x += boss.vx * dt; boss.y += boss.vy * dt;
  boss.poise = Math.min(boss.maxPoise, boss.poise + dt * 5.0);
  if (state.gameTime >= boss.nextAttackAt) bossAttackPattern();
}

function reflectProjectile(p) {
  p.team = 'player'; p.reflected = true; p.kind = 'reflected'; p.trailKind = activeConcord() ? 'concord' : activeGuard() ? 'green' : 'white';
  const lead = .13, [dx, dy] = norm(boss.x + boss.vx * lead - p.x, boss.y + boss.vy * lead - p.y);
  const speed = Math.max(620, Math.hypot(p.vx, p.vy) * 1.35); p.vx = dx * speed; p.vy = dy * speed; p.damage = activeConcord() ? 42 : 30; p.poise = activeConcord() ? 28 : 21; p.pierce = activeSight() ? 1 : 0;
  gainFlux(.52, 'PERFECT COUNTER');
  if (activeGuard()) player.hp = Math.min(player.maxHp, player.hp + 2.4);
  particle(player.x, player.y, activeGuard() ? 'green' : 'white', 24, 220); shockwave(player.x, player.y, 'white', 105, .28, 6); hitstop(.055); shake(9); audio.parry();
}

function updateProjectiles(dt) {
  for (const p of state.projectiles) {
    p.px = p.x; p.py = p.y;
    if (p.curve) {
      const speed = Math.hypot(p.vx, p.vy); const a = Math.atan2(p.vy, p.vx) + p.curve * dt; p.vx = Math.cos(a) * speed; p.vy = Math.sin(a) * speed;
    }
    if (p.homing && p.team === 'enemy') {
      const current = Math.atan2(p.vy, p.vx); const desired = Math.atan2(player.y - p.y, player.x - p.x); const a = current + clamp(angleDelta(desired, current), -p.homing * dt, p.homing * dt); const speed = Math.hypot(p.vx, p.vy); p.vx = Math.cos(a) * speed; p.vy = Math.sin(a) * speed;
    }
    if (state.bloom && p.team === 'enemy') {
      const b = state.bloom, age = state.gameTime - b.start;
      if (age >= 0 && state.gameTime < b.end) {
        const dx = b.x - p.x, dy = b.y - p.y, d = Math.hypot(dx, dy) || 1;
        if (d < (b.concord ? 320 : 260)) {
          const pull = (b.concord ? 1100 : 820) * (1 - d / (b.concord ? 340 : 280)); p.vx += dx / d * pull * dt; p.vy += dy / d * pull * dt;
          if (d < 38) { p.life = -1; b.captured.push(p); }
        }
      }
    }
    p.x += p.vx * dt; p.y += p.vy * dt; p.life -= dt;
    trail(p.px, p.py, p.x, p.y, p.trailKind, p.r * 1.45, .11);
    if (p.team === 'enemy') {
      const d = Math.hypot(p.x - player.x, p.y - player.y);
      if (state.gameTime < player.parryUntil && d < 76) { reflectProjectile(p); continue; }
      if (!p.nearMissed && d < 54 && d > player.r + p.r + 3) {
        if (state.gameTime < player.dashUntil || Math.hypot(player.vx, player.vy) > 210) { p.nearMissed = true; gainFlux(.18, 'THREAD THE NEEDLE'); particle(player.x, player.y, 'flux', 5, 75); }
      }
      if (state.gameTime >= player.invulnerableUntil && sweptCircleHit(p.px, p.py, p.x, p.y, player.x, player.y, player.r + p.r)) {
        const reduction = activeGuard() ? 1 - cfg.guardDamageReduction : 1;
        player.hp = Math.max(0, player.hp - p.damage * reduction); p.life = -1; particle(player.x, player.y, 'red', 16, 170); shake(8); flash(.15); hitstop(.025); damageText(player.x, player.y - 34, `-${Math.round(p.damage * reduction)}`, 'red');
        if (player.hp <= 0) setStage('fail');
      }
    } else {
      if (boss.alive && sweptCircleHit(p.px, p.py, p.x, p.y, boss.x, boss.y, boss.r + p.r)) {
        const [nx, ny] = norm(p.vx, p.vy); damageBoss(p.damage, p.poise, nx * (p.reflected ? 90 : 28), ny * (p.reflected ? 90 : 28), p.reflected);
        if (p.pierce > 0) p.pierce--; else p.life = -1;
      }
      for (const e of state.echoes) {
        if (!e.alive || p.life <= 0) continue;
        if (sweptCircleHit(p.px, p.py, p.x, p.y, e.x, e.y, e.r + p.r)) {
          const [nx, ny] = norm(p.vx, p.vy); damageEcho(e, p.damage, p.poise, nx * 85, ny * 85); if (p.pierce > 0) p.pierce--; else p.life = -1;
        }
      }
    }
  }
  state.projectiles = state.projectiles.filter((p) => p.life > 0 && p.x > -120 && p.x < W + 120 && p.y > -120 && p.y < H + 120);
}

function updateBloom() {
  if (!state.bloom) return;
  state.bloom.x = player.x; state.bloom.y = player.y;
  if (state.gameTime >= state.bloom.end) { const b = state.bloom; state.bloom = null; detonateBloom(b); }
}

function updateFx(dt) {
  for (const p of state.particles) { p.x += p.vx * dt; p.y += p.vy * dt; p.vx *= Math.exp(-3 * dt); p.vy *= Math.exp(-3 * dt); p.life -= dt; }
  state.particles = state.particles.filter((p) => p.life > 0);
  for (const t of state.trails) t.life -= dt; state.trails = state.trails.filter((t) => t.life > 0);
  for (const s of state.shockwaves) { const q = 1 - s.life / s.maxLife; s.r = lerp(4, s.maxR, q); s.life -= dt; } state.shockwaves = state.shockwaves.filter((s) => s.life > 0);
  for (const d of state.damageTexts) { d.y += d.vy * dt; d.life -= dt; } state.damageTexts = state.damageTexts.filter((d) => d.life > 0);
  state.cameraShake *= Math.exp(-12 * dt); state.flash *= Math.exp(-11 * dt); state.chroma *= Math.exp(-8 * dt);
}

function simulationStep(dt) {
  if (!state.started) return;
  state.gameTime += dt;
  updateEvidence(dt);
  if (state.stage === 'combat') {
    updatePlayer(dt); updateBoss(dt); updateEchoes(dt); updateTelegraphs(); updateBloom(); updateProjectiles(dt);
  }
  updateFx(dt);
  justPressed.clear();
}
