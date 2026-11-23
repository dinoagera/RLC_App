import { impedance, frequencySamples } from './math.mjs';

export const colors = { voltage: '#fbbf60', current: '#48dcc8', violet: '#ab9bff',
  blue: '#65adff', muted: '#93a9c2', grid: '#26364d', text: '#dce8f7' };
const tau = Math.PI * 2;
const formatters = new Map();
export const fmt = (n, digits = 3) => {
  if (!formatters.has(digits)) formatters.set(digits, new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: digits, useGrouping: false
  }));
  return formatters.get(digits).format(Math.abs(n) < 1e-12 ? 0 : n);
};
const tick = n => Math.abs(n) >= 10000 || (Math.abs(n) > 0 && Math.abs(n) < .001)
  ? n.toExponential(1) : fmt(n, Math.abs(n) < .1 ? 4 : Math.abs(n) < 5 ? 2 : 1);

export class PlotRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.cache = null;
    this.pointer = null;
    this.width = this.height = 0;
  }
  invalidate() { this.cache = null; }
  resize() {
    const r = this.canvas.getBoundingClientRect(), dpr = Math.min(devicePixelRatio || 1, 2.5);
    const w = Math.round(r.width * dpr), h = Math.round(r.height * dpr);
    if (this.canvas.width !== w || this.canvas.height !== h) {
      this.canvas.width = w; this.canvas.height = h;
    }
    this.width = r.width; this.height = r.height;
    this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }
  line(x1, y1, x2, y2, color, width = 1, dash = []) {
    const c = this.ctx;
    c.beginPath(); c.strokeStyle = color; c.lineWidth = width; c.setLineDash(dash);
    c.moveTo(x1, y1); c.lineTo(x2, y2); c.stroke(); c.setLineDash([]);
  }
  text(text, x, y, color = colors.muted, align = 'left', size = 10) {
    const c = this.ctx;
    c.fillStyle = color; c.font = `${size}px LabSans, sans-serif`; c.textAlign = align;
    c.textBaseline = 'middle'; c.fillText(text, x, y);
  }
  path(points, color, width = 2) {
    const c = this.ctx;
    c.beginPath(); c.strokeStyle = color; c.lineWidth = width;
    points.forEach(([x, y], i) => i ? c.lineTo(x, y) : c.moveTo(x, y));
    c.stroke();
  }
  arrow(x, y, dx, dy, color, width = 2.2) {
    this.line(x, y, x + dx, y + dy, color, width);
    const len = Math.hypot(dx, dy);
    if (len < 1) return;
    const a = Math.atan2(dy, dx), k = Math.min(10, Math.max(5, len * .25));
    this.line(x + dx, y + dy, x + dx - Math.cos(a - .45) * k, y + dy - Math.sin(a - .45) * k, color, width);
    this.line(x + dx, y + dy, x + dx - Math.cos(a + .45) * k, y + dy - Math.sin(a + .45) * k, color, width);
  }
  draw(p, r, state) {
    this.resize();
    const c = this.ctx;
    c.clearRect(0, 0, this.width, this.height);
    if (this.width < 50 || this.height < 50) return;
    if (state.mode === 'time') this.time(p, r, state);
    else if (state.mode === 'phasor') this.phasor(p, r);
    else this.frequency(p, r);
  }
  time(p, r, state) {
    const { width: w, height: h } = this;
    const left = w < 500 ? 48 : 64, right = w - 18, top = 27, bottom = h - 39;
    const dx = right - left, dy = bottom - top;
    const max = state.normalized ? 1.2 : Math.max(p.U, r.im) * 1.2;
    this.bounds = { left, right, top, bottom };
    this.text(state.normalized ? 'u / Uₘ · i / Iₘ' : 'u, В · i, А', left, 10);
    for (let i = 0; i <= 4; i++) {
      const y = top + dy * i / 4;
      this.line(left, y, right, y, colors.grid, i === 2 ? 1.4 : .7);
      this.text(tick(max * (1 - i / 2)), left - 12, y, colors.muted, 'right');
    }
    for (let i = 0; i <= 12; i++) {
      const x = left + dx * i / 12;
      this.line(x, top, x, bottom, i % 2 ? '#1c2a3d' : colors.grid, .7);
      if (i % (w < 500 ? 4 : 2) === 0)
        this.text(tick((state.cycles + i / 4) / p.f * 1000), x, bottom + 16, colors.muted, 'center');
    }
    this.text('t, мс', right, h - 7, colors.muted, 'right');
    const u = [], current = [];
    for (let i = 0; i <= 760; i++) {
      const theta = tau * (state.cycles + 3 * i / 760), x = left + dx * i / 760;
      u.push([x, top + dy / 2 - Math.sin(theta) * (state.normalized ? 1 : p.U) / max * dy / 2]);
      current.push([x, top + dy / 2 - Math.sin(theta - r.phi) * (state.normalized ? 1 : r.im) / max * dy / 2]);
    }
    this.path(u, colors.voltage, 2.6);
    // A dashed current remains distinguishable when normalized signals coincide at resonance.
    this.ctx.setLineDash([6, 4]);
    this.path(current, colors.current, 1.8);
    this.ctx.setLineDash([]);
    if (this.pointer && this.pointer.x >= left && this.pointer.x <= right) {
      const x = this.pointer.x;
      this.line(x, top, x, bottom, '#a9bddc77', 1, [3, 4]);
    }
  }
  frequency(p, r) {
    const { width: w, height: h } = this;
    const left = w < 500 ? 49 : 66, right = w - 18, top = 25, bottom = h - 40;
    const gap = 52, dy = (bottom - top - gap) / 2, lower = top + dy + gap, dx = right - left;
    this.bounds = { left, right, top, bottom };
    if (!this.cache) {
      const values = frequencySamples(p).map(f => {
        const z = impedance(p, f); return { f, z, im: p.U / z };
      });
      this.cache = { values, imax: Math.max(...values.map(x => x.im)) * 1.12,
        zmax: Math.max(...values.map(x => x.z)) * 1.12 };
    }
    const { values, imax, zmax } = this.cache;
    this.text('Iₘ, А', left, 10, colors.current);
    this.text('Z, Ом', left, lower - 16, colors.violet);
    for (let j = 0; j < 2; j++) {
      const y0 = j ? lower : top, max = j ? zmax : imax;
      for (let i = 0; i <= 2; i++) {
        const y = y0 + dy * i / 2;
        this.line(left, y, right, y, colors.grid, .7);
        this.text(tick(max * (1 - i / 2)), left - 10, y, colors.muted, 'right', 9);
      }
    }
    const ticks = w < 500 ? [1, 10, 100, 1000] : [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000];
    for (const f of ticks) {
      const x = left + Math.log10(f) / 3 * dx;
      this.line(x, top, x, top + dy, colors.grid, .7);
      this.line(x, lower, x, bottom, colors.grid, .7);
      this.text(String(f), x, bottom + 16, colors.muted, 'center');
    }
    this.text('f, Гц · log', right, h - 7, colors.muted, 'right');
    this.path(values.map(a => [left + Math.log10(a.f) / 3 * dx, top + dy - a.im / imax * dy]), colors.current);
    this.path(values.map(a => [left + Math.log10(a.f) / 3 * dx, lower + dy - a.z / zmax * dy]), colors.violet);
    const currentX = left + Math.log10(p.f) / 3 * dx;
    this.line(currentX, top, currentX, bottom, '#93a9c255', 1);
    if (r.f0 >= 1 && r.f0 <= 1000) {
      const x = left + Math.log10(r.f0) / 3 * dx;
      this.line(x, top, x, bottom, colors.voltage, 1.2, [4, 5]);
      this.text(`f₀ = ${fmt(r.f0)} Гц`, Math.max(left, Math.min(right - 110, x + 9)), top + dy + 23, colors.voltage);
    } else this.text('f₀ вне диапазона 1–1000 Гц', left + 4, top + dy + 23, colors.voltage);
    if (this.pointer && this.pointer.x >= left && this.pointer.x <= right)
      this.line(this.pointer.x, top, this.pointer.x, bottom, '#a9bddc88', 1, [3, 4]);
  }
  phasor(p, r) {
    const { width: w, height: h, ctx: c } = this;
    const radius = Math.min((h - 100) / 2, w * .26);
    const ox = w * .43, oy = (h - 30) / 2;
    const scale = radius / Math.max(p.U, r.ur, r.ul, r.uc);
    this.bounds = null;
    for (const fraction of [.5, 1]) {
      c.beginPath(); c.arc(ox, oy, radius * fraction, 0, tau);
      c.strokeStyle = colors.grid; c.lineWidth = .8; c.stroke();
    }
    this.line(ox - radius - 20, oy, ox + radius + 25, oy, colors.grid);
    this.line(ox, oy - radius - 20, ox, oy + radius + 15, colors.grid);
    this.text('Re', ox + radius + 32, oy);
    this.text('Im', ox - 10, oy - radius - 20, colors.muted, 'right');
    this.arrow(ox, oy, 0, -r.ul * scale, colors.blue);
    this.arrow(ox, oy, 0, r.uc * scale, colors.violet);
    this.arrow(ox, oy, r.ur * scale, 0, colors.muted, 4);
    this.arrow(ox, oy, r.ur * scale, -(r.ul - r.uc) * scale, colors.voltage, 2.5);
    // Canvas angles are clockwise; negative physical angle gives the correct CCW orientation.
    c.beginPath(); c.arc(ox, oy, radius * .32, 0, -r.phi, r.phi > 0);
    c.strokeStyle = colors.voltage; c.lineWidth = 1; c.stroke();
    const labelSize = w < 500 ? 9 : 11;
    this.text(`Uₘ = ${fmt(p.U)} В`, Math.min(w - 120, ox + r.ur * scale + 12),
      Math.max(17, oy - (r.ul - r.uc) * scale - 17), colors.voltage, 'left', labelSize);
    this.text(`Uᵣ = ${fmt(r.ur)} В`, Math.min(w - 115, ox + r.ur * scale + 12),
      oy + 18, colors.muted, 'left', labelSize);
    this.text(`Uₗ = ${fmt(r.ul)} В`, ox - 12, Math.max(13, oy - r.ul * scale - 14), colors.blue, 'right', labelSize);
    this.text(`U_C = ${fmt(r.uc)} В`, ox - 12, Math.min(h - 58, oy + r.uc * scale + 15), colors.violet, 'right', labelSize);
    this.text(`φ = ${fmt(r.degrees, 2)}°`, Math.min(w - 90, ox + radius * .45), oy - 42, colors.voltage, 'left', labelSize);
    const ix = Math.max(32, ox - radius), iy = h - 35;
    this.arrow(ix, iy, Math.min(radius * 1.45, w - ix - 160), 0, colors.current);
    this.text(`Iₘ = ${fmt(r.im, 4)} А · опорная фаза 0°`, ix, h - 13, colors.current, 'left', labelSize);
    this.text('U = Uᵣ + Uₗ + U_C', w - 15, 12, colors.muted, 'right', labelSize);
  }
  probe(p, r, state, x) {
    const b = this.bounds;
    if (!b || x < b.left || x > b.right) return null;
    const ratio = (x - b.left) / (b.right - b.left);
    if (state.mode === 'time') {
      const cycles = state.cycles + ratio * 3, theta = cycles * tau;
      return `t = ${fmt(cycles / p.f * 1000, 3)} мс\nu = ${fmt(p.U * Math.sin(theta), 4)} В\ni = ${fmt(r.im * Math.sin(theta - r.phi), 5)} А`;
    }
    const f = 1000 ** ratio, z = impedance(p, f);
    return `f = ${fmt(f)} Гц\nIₘ = ${fmt(p.U / z, 5)} А\n|Z| = ${fmt(z)} Ом\nЩелчок — установить частоту`;
  }
  frequencyAt(x) {
    const b = this.bounds;
    if (!b || x < b.left || x > b.right) return null;
    return 1000 ** ((x - b.left) / (b.right - b.left));
  }
}
