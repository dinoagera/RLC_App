export const defaults = Object.freeze({ R: 100, L: 100, C: 100, f: 50, U: 10 });
export const limits = Object.freeze({ R: [1, 1000], L: [1, 500], C: [1, 1000], f: [1, 1000], U: [1, 100] });
export function valid(p) {
  return p != null && Object.entries(limits).every(([key, [min, max]]) =>
    Number.isFinite(p[key]) && p[key] >= min && p[key] <= max);
}
export function parseNumber(text) {
  const s = String(text).trim().replace(',', '.');
  if (!/^[+-]?(?:\d+\.?\d*|\.\d+)(?:e[+-]?\d+)?$/i.test(s)) return null;
  const value = Number(s);
  return Number.isFinite(value) ? value : null;
}
export function calculate(p) {
  if (!valid(p)) throw new RangeError('Параметры вне допустимых диапазонов');
  const omega = 2 * Math.PI * p.f;
  const l = p.L * 1e-3, c = p.C * 1e-6;
  const xl = omega * l, xc = 1 / (omega * c);
  const z = Math.hypot(p.R, xl - xc), im = p.U / z;
  const phi = Math.atan2(xl - xc, p.R);
  const f0 = 1 / (2 * Math.PI * Math.sqrt(l * c));
  return { omega, xl, xc, z, im, phi, f0, degrees: phi * 180 / Math.PI,
    ur: im * p.R, ul: im * xl, uc: im * xc, power: im * im * p.R / 2,
    pf: p.R / z, q: Math.sqrt(l / c) / p.R };
}
export function impedance(p, f) {
  if (!valid(p) || !Number.isFinite(f) || f <= 0) throw new RangeError('Недопустимая частота');
  const omega = 2 * Math.PI * f;
  return Math.hypot(p.R, omega * p.L * 1e-3 - 1 / (omega * p.C * 1e-6));
}
export function nearResonance(f, f0) {
  return Number.isFinite(f) && Number.isFinite(f0) && f > 0 && f0 > 0 && Math.abs(f - f0) / f0 <= .02 + 1e-14;
}
export function frequencySamples(p, count = 560) {
  if (count < 2 || !Number.isInteger(count)) throw new RangeError('count');
  const values = new Set(Array.from({ length: count }, (_, i) => 1000 ** (i / (count - 1))));
  values.add(p.f);
  const { f0 } = calculate(p), bandwidth = p.R / (2 * Math.PI * p.L * 1e-3);
  for (let i = -48; i <= 48; i++) {
    const f = f0 + bandwidth * i / 16;
    if (f >= 1 && f <= 1000) values.add(f);
  }
  return [...values].sort((a, b) => a - b);
}
