'use strict';

function frame(nowMs) {
  const elapsed = Math.min(.05, (nowMs - state.lastFrameMs) / 1000); state.lastFrameMs = nowMs; state.realTime = nowMs / 1000; state.accumulator += elapsed;
  const inHitstop = state.realTime < state.hitstopUntilReal;
  if (!inHitstop) {
    let steps = 0;
    while (state.accumulator >= FIXED_DT && steps < 8) { simulationStep(FIXED_DT); state.accumulator -= FIXED_DT; steps++; }
  } else {
    state.accumulator = Math.min(state.accumulator, FIXED_DT);
  }
  updateTelemetry(); render();
  state.frameCounter++;
  if (nowMs - state.fpsWindowStart > 1000) { fpsBadge.textContent = `${SIM_HZ} Hz FIXED SIM`; state.frameCounter = 0; state.fpsWindowStart = nowMs; }
  requestAnimationFrame(frame);
}

function keyName(e) { return e.key === ' ' ? ' ' : e.key.toLowerCase(); }
window.addEventListener('keydown', (e) => {
  const k = keyName(e); if (['w', 'a', 's', 'd', 'q', 'e', 'f', 'c', 'r', ' ', 'shift'].includes(k)) e.preventDefault();
  if (!keys.has(k)) justPressed.add(k); keys.add(k); audio.ensure();
});
window.addEventListener('keyup', (e) => keys.delete(keyName(e)));
canvas.addEventListener('mousemove', (e) => { const r = canvas.getBoundingClientRect(); mouse.x = (e.clientX - r.left) / r.width * W; mouse.y = (e.clientY - r.top) / r.height * H; mouse.active = true; });
canvas.addEventListener('mouseleave', () => { mouse.active = false; });
canvas.addEventListener('mousedown', (e) => { e.preventDefault(); audio.ensure(); if (e.button === 0) pulseShot(); if (e.button === 2) riftCleave(); });
canvas.addEventListener('contextmenu', (e) => e.preventDefault());

startButton.addEventListener('click', () => { audio.ensure(); state.started = true; intro.classList.add('hidden'); setStage('cal-sight'); });

window.__mindforge = {
  state, player, boss,
  startCombat() { state.started = true; intro.classList.add('hidden'); setStage('combat'); },
  inject(target, confidence = .9, quality = .92) { applyAura(target, confidence, quality); },
  grantFlux() { player.flux = cfg.maxFlux; },
};

const params = new URLSearchParams(location.search);
if (params.get('autostart') === 'combat') {
  state.started = true; intro.classList.add('hidden'); setStage('combat'); state.sightUntil = state.gameTime + 5; state.guardUntil = state.gameTime + 3.2; player.flux = cfg.maxFlux;
}

requestAnimationFrame(frame);
