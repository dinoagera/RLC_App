import { defaults, limits, valid, parseNumber, calculate, impedance, nearResonance, frequencySamples } from './math.mjs';
import { PlotRenderer, colors, fmt } from './plots.mjs';

const $ = id => document.getElementById(id);
const storageKey = 'rlc-lab.v1';
let stored = {};
try { stored = JSON.parse(localStorage.getItem(storageKey) || '{}') || {}; } catch { /* private mode / corrupted data */ }
let parameters = { ...defaults };
if (stored.p && valid(stored.p)) parameters = { ...stored.p };
let result = calculate(parameters);
const state = {
  running: !matchMedia('(prefers-reduced-motion: reduce)').matches,
  speed: Number.isFinite(stored.speed) ? Math.max(.1, Math.min(4, stored.speed)) : 1,
  normalized: typeof stored.normalized === 'boolean' ? stored.normalized : true,
  mode: ['time', 'phasor', 'frequency'].includes(stored.mode) ? stored.mode : 'time',
  cycles: 0
};
const plot = new PlotRenderer($('graph'));
let dirty = true, saveTimer, lastTime = null;
function save() {
  clearTimeout(saveTimer);
  saveTimer = setTimeout(flushSettings, 250);
}
function flushSettings() {
  try { localStorage.setItem(storageKey, JSON.stringify({
    p: parameters, speed: state.speed, normalized: state.normalized, mode: state.mode
  })); } catch {
    feedback('Хранилище недоступно: настройки действуют до закрытия окна.', 'error');
  }
}
window.addEventListener('pagehide', flushSettings);
const names = { R: ['R', 'Сопротивление', 'Ом'], L: ['L', 'Индуктивность', 'мГн'],
  C: ['C', 'Ёмкость', 'мкФ'], f: ['f', 'Частота', 'Гц'], U: ['Uₘ', 'Амплитуда', 'В'] };
function feedback(message, kind = '') {
  $('feedback').textContent = message; $('feedback').className = kind;
}
function rangeProgress(input) {
  input.style.setProperty('--progress', `${(Number(input.value) - Number(input.min)) / (Number(input.max) - Number(input.min)) * 100}%`);
}
for (const [key, [symbol, name, unit]] of Object.entries(names)) {
  const row = document.createElement('div'); row.className = 'parameter';
  row.innerHTML = `<div class="parameter-top"><label for="input-${key}"><span class="parameter-symbol">${symbol}</span>${name}</label>
    <div class="number-wrap" id="wrap-${key}"><input id="input-${key}" inputmode="decimal" autocomplete="off" spellcheck="false"
      maxlength="28" aria-label="${name}, ${unit}" aria-describedby="limits-${key}"><span>${unit}</span></div></div>
    <input type="range" id="slider-${key}" min="1" max="${limits[key][1]}" step="any" aria-label="${name}, ползунок">
    <div class="range-labels" id="limits-${key}"><span>1 ${unit}</span><span>${limits[key][1]} ${unit}</span></div>`;
  $('parameter-fields').append(row);
  const field = $(`input-${key}`), slider = $(`slider-${key}`);
  field.addEventListener('change', () => {
    const number = parseNumber(field.value), [, max] = limits[key];
    if (number === null || number < 1 || number > max) {
      $(`wrap-${key}`).classList.add('error'); field.setAttribute('aria-invalid', 'true');
      feedback(number === null ? 'Введите конечное число: например 50 или 50,5.' : `${name}: допустимо от 1 до ${max} ${unit}.`, 'error');
      field.value = fmt(parameters[key], 9);
      return;
    }
    apply(key, number); feedback('Параметры обновлены', 'success');
  });
  field.addEventListener('keydown', event => {
    if (event.key === 'Enter') field.blur();
    if (event.key === 'Escape') { field.value = fmt(parameters[key], 9); field.blur(); }
  });
  slider.addEventListener('input', () => {
    apply(key, Number(slider.value)); feedback('Параметры обновлены', 'success');
  });
}
function apply(key, value) {
  if (!Number.isFinite(value) || value < 1 || value > limits[key][1]) return false;
  parameters[key] = value;
  refresh(); save(); return true;
}
function refresh() {
  result = calculate(parameters);
  for (const key of Object.keys(names)) {
    $(`slider-${key}`).value = parameters[key]; rangeProgress($(`slider-${key}`));
    if (document.activeElement !== $(`input-${key}`)) $(`input-${key}`).value = fmt(parameters[key], 9);
    $(`wrap-${key}`).classList.remove('error'); $(`input-${key}`).removeAttribute('aria-invalid');
  }
  $('scheme-R').textContent = `R  ·  ${fmt(parameters.R)} Ом`;
  $('scheme-L').textContent = `L  ·  ${fmt(parameters.L)} мГн`;
  $('scheme-C-value').textContent = `${fmt(parameters.C)} мкФ`;
  $('scheme-U').textContent = `${fmt(parameters.U)} В`;
  $('scheme-f').textContent = `f = ${fmt(parameters.f)} Гц`;
  for (const [id, value, digits] of [['z', result.z, 2], ['im', result.im, 4],
    ['phi', result.degrees, 2], ['f0', result.f0, 3], ['xl', result.xl, 2], ['xc', result.xc, 2]]) {
    const display = Math.abs(value) >= 10000 ? value.toExponential(2).replace('.', ',')
      : id === 'phi' && value > .005 ? '+' + fmt(value, digits) : fmt(value, digits);
    $('result-' + id).textContent = display;
    $('result-' + id).title = fmt(value, 9);
  }
  $('secondary-results').innerHTML = `<span>ω <b>${fmt(result.omega, 2)}</b> рад/с</span>
    <span>P <b>${fmt(result.power)}</b> Вт</span><span>cos φ <b>${fmt(result.pf)}</b></span>
    <span>Q <b>${fmt(result.q)}</b></span><span>I<sub>действ</sub> <b>${fmt(result.im / Math.SQRT2, 4)}</b> А</span>`;
  const near = nearResonance(parameters.f, result.f0);
  $('resonance').classList.toggle('active', near);
  $('resonance-text').textContent = near ? 'Область резонанса' : result.phi > 0 ? 'Ток отстаёт от напряжения' : 'Ток опережает напряжение';
  $('tune').disabled = result.f0 < 1 || result.f0 > 1000;
  $('tune').title = $('tune').disabled ? `f₀ = ${fmt(result.f0)} Гц вне диапазона 1–1000 Гц. Измените L или C.` : `Установить точную частоту ${fmt(result.f0, 9)} Гц`;
  $('graph').setAttribute('aria-label', `${$('graph-title').textContent}. Импеданс ${fmt(result.z)} Ом, амплитуда тока ${fmt(result.im, 4)} А, фаза ${fmt(result.degrees, 2)} градусов, резонанс ${fmt(result.f0)} Гц.`);
  playbackStatus(); plot.invalidate(); dirty = true;
}
function playbackStatus() {
  $('playback-status').textContent = `${state.running ? '● Анимация запущена' : 'Ⅱ Анимация на паузе'} · Источник ${fmt(parameters.f)} Гц`;
  document.querySelector('.playback-status').classList.toggle('paused', !state.running);
  $('start').setAttribute('aria-pressed', String(state.running));
  $('pause').setAttribute('aria-pressed', String(!state.running));
}
function run(value) { state.running = value; lastTime = null; playbackStatus(); dirty = true; }
$('start').addEventListener('click', () => run(true));
$('pause').addEventListener('click', () => run(false));
$('reset').addEventListener('click', reset);
function reset() {
  document.activeElement?.blur();
  parameters = { ...defaults };
  state.cycles = 0; state.speed = 1; state.normalized = true;
  $('speed').value = 1; updateSpeed(); run(true); changeMode('time');
  refresh(); feedback('Исходные настройки восстановлены', 'success'); save();
}
function updateSpeed() {
  state.speed = Number($('speed').value);
  $('speed-value').textContent = fmt(state.speed, 1) + '×';
  rangeProgress($('speed')); save();
}
$('speed').value = state.speed; updateSpeed();
$('speed').addEventListener('input', updateSpeed);
$('tune').addEventListener('click', () => {
  if (apply('f', result.f0)) feedback('Точный резонанс: Z = R, φ = 0°, Iₘ = Uₘ/R', 'success');
});
for (const [id, f] of [['preset-c', 20], ['preset-l', 150]]) {
  $(id).addEventListener('click', () => {
    parameters = { ...defaults, R: 30, f }; refresh(); save();
    feedback(f === 20 ? 'Ёмкостный режим: ток опережает напряжение' : 'Индуктивный режим: ток отстаёт от напряжения', 'success');
  });
}
function changeMode(mode) {
  state.mode = mode;
  document.querySelectorAll('[role=tab]').forEach(button => {
    const selected = button.dataset.mode === mode;
    button.setAttribute('aria-selected', String(selected)); button.tabIndex = selected ? 0 : -1;
  });
  $('plot-area').setAttribute('aria-labelledby', 'tab-' + mode);
  $('graph-title').textContent = { time: 'Временные сигналы', phasor: 'Фазовая диаграмма', frequency: 'Частотный отклик' }[mode];
  $('normalize').hidden = mode !== 'time';
  $('normalize').setAttribute('aria-pressed', String(state.normalized));
  const legendItems = mode === 'time' ? [['u(t), напряжение', 'voltage'], ['i(t), ток', 'current', true]]
    : mode === 'phasor' ? [['U', 'voltage'], ['I', 'current'], ['Uᵣ', 'muted'], ['Uₗ', 'blue'], ['U<sub>C</sub>', 'violet']]
    : [['Iₘ(f)', 'current'], ['|Z(f)|', 'violet'], ['f₀', 'voltage', true]];
  $('legend').innerHTML = legendItems.map(([label, color, dashed]) =>
    `<span><i style="--line:${colors[color]}" ${dashed ? 'class="dashed"' : ''}></i>${label}</span>`).join('');
  $('graph-info').textContent = mode === 'time' ? '3 ПЕРИОДА · ОБЩАЯ ОСЬ ВРЕМЕНИ'
    : mode === 'frequency' ? '1–1000 ГЦ · ЛОГАРИФМИЧЕСКАЯ ОСЬ' : 'ТОК — ОПОРНАЯ ФАЗА 0°';
  $('graph-note').textContent = mode === 'time'
    ? (state.normalized ? 'Нормированные амплитуды · развёртка замедлена для наблюдения' : 'Общая числовая шкала: u — В, i — А · нормализация помогает сравнить фазы')
    : mode === 'phasor' ? 'Напряжения в едином масштабе · ток показан на отдельной опорной оси'
    : 'Независимые вертикальные шкалы · щёлкните на графике, чтобы установить частоту';
  plot.pointer = null; $('plot-tooltip').hidden = true;
  lastTime = null; dirty = true; save();
}
document.querySelectorAll('[role=tab]').forEach(button => {
  button.addEventListener('click', () => changeMode(button.dataset.mode));
  button.addEventListener('keydown', event => {
    const modes = ['time', 'phasor', 'frequency'], index = modes.indexOf(state.mode);
    const direction = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (direction) {
      event.preventDefault(); const next = modes[(index + direction + 3) % 3];
      changeMode(next); $('tab-' + next).focus();
    }
  });
});
$('normalize').addEventListener('click', () => {
  state.normalized = !state.normalized; changeMode(state.mode);
});
const help = $('help');
$('help-open').addEventListener('click', () => help.showModal());
$('help-close').addEventListener('click', () => help.close());
$('help-done').addEventListener('click', () => help.close());
help.addEventListener('click', event => { if (event.target === help) {
  const r = help.getBoundingClientRect();
  if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) help.close();
}});
document.addEventListener('keydown', event => {
  if (event.key === 'F1') {
    event.preventDefault(); help.open ? help.close() : help.showModal(); return;
  }
  if (help.open || event.target.closest('input, textarea, select, [contenteditable]')) return;
  if (event.code === 'Space') { event.preventDefault(); run(!state.running); }
  if (event.code === 'KeyR' && !event.ctrlKey && !event.metaKey) { event.preventDefault(); reset(); }
  if (/^[123]$/.test(event.key)) changeMode(['time', 'phasor', 'frequency'][Number(event.key) - 1]);
});
const graphCanvas = $('graph'), tooltip = $('plot-tooltip');
function updateProbe() {
  if (!plot.pointer || state.mode === 'phasor') { tooltip.hidden = true; return; }
  const { x, y } = plot.pointer, content = plot.probe(parameters, result, state, x);
  tooltip.hidden = !content;
  if (content) {
    tooltip.textContent = content;
    tooltip.style.left = `${Math.max(0, Math.min(plot.width - tooltip.offsetWidth - 5, x + 14))}px`;
    tooltip.style.top = `${Math.max(0, Math.min(plot.height - tooltip.offsetHeight - 5, y + 12))}px`;
  }
}
graphCanvas.addEventListener('pointermove', event => {
  const r = graphCanvas.getBoundingClientRect();
  plot.pointer = { x: event.clientX - r.left, y: event.clientY - r.top }; dirty = true;
});
graphCanvas.addEventListener('pointerleave', () => { plot.pointer = null; tooltip.hidden = true; dirty = true; });
graphCanvas.addEventListener('click', event => {
  if (state.mode !== 'frequency') return;
  const f = plot.frequencyAt(event.clientX - graphCanvas.getBoundingClientRect().left);
  if (f !== null) apply('f', f);
});
$('export').addEventListener('click', () => {
  const csv = '\uFEFF' + ['f_Hz;Im_A;Z_Ohm;phi_deg;is_f0', ...frequencySamples(parameters).map(f => {
    const z = impedance(parameters, f), w = 2 * Math.PI * f;
    const phi = Math.atan2(w * parameters.L * 1e-3 - 1 / (w * parameters.C * 1e-6), parameters.R) * 180 / Math.PI;
    return [f, parameters.U / z, z, phi, Math.abs(f - result.f0) < 1e-10 ? 1 : 0].join(';');
  })].join('\r\n');
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  const a = document.createElement('a'); a.href = url; a.download = 'RLC-frequency-response.csv'; a.click();
  setTimeout(() => URL.revokeObjectURL(url), 2000);
});
new ResizeObserver(() => { dirty = true; }).observe($('plot-area'));
document.fonts.ready.then(() => { dirty = true; });
document.addEventListener('visibilitychange', () => { lastTime = null; dirty = true; });
function frame(now) {
  const delta = lastTime === null ? 0 : Math.min((now - lastTime) / 1000, .1);
  lastTime = now;
  if (!document.hidden) {
    if (state.running && !help.open) {
      state.cycles = (state.cycles + delta * state.speed * .35) % 1000;
      if (state.mode === 'time') dirty = true;
    }
    if (dirty || (state.running && !help.open)) {
      const instant = Math.sin(2 * Math.PI * state.cycles - result.phi);
      $('flow-arrow').style.opacity = .2 + Math.abs(instant) * .8;
      $('flow-arrow').setAttribute('transform', instant >= 0 ? '' : 'translate(1032 0) scale(-1 1)');
    }
    if (dirty) { plot.draw(parameters, result, state); updateProbe(); dirty = false; }
  }
  requestAnimationFrame(frame);
}
changeMode(state.mode); refresh(); requestAnimationFrame(frame);
