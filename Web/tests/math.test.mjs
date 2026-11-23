import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { defaults, limits, valid, parseNumber, calculate, impedance, nearResonance, frequencySamples } from '../math.mjs';

const near = (a, b, epsilon = 1e-10) => assert.ok(Math.abs(a - b) <= epsilon * Math.max(1, Math.abs(b)), `${a} ≠ ${b}`);
test('Контрольный пример ТЗ', () => {
  const r = calculate(defaults);
  near(r.z, 100.0008613789517); near(r.degrees, -.23781168998129457);
  near(r.f0, 50.32921210448704);
});
test('Точный резонанс и знаки фаз', () => {
  const { f0 } = calculate(defaults);
  const r = calculate({ ...defaults, f: f0 });
  near(r.z, defaults.R); near(r.im, defaults.U / defaults.R); near(r.phi, 0);
  assert.ok(calculate({ ...defaults, f: 10 }).phi < 0);
  assert.ok(calculate({ ...defaults, f: 100 }).phi > 0);
});
test('Границы резонансной области ±2%', () => {
  for (const factor of [.98, 1, 1.02]) assert.ok(nearResonance(50 * factor, 50));
  for (const factor of [.97999, 1.02001]) assert.ok(!nearResonance(50 * factor, 50));
  assert.ok(!nearResonance(0, 0)); assert.ok(!nearResonance(Infinity, 50));
});
test('Валидация чисел и диапазонов', () => {
  assert.equal(parseNumber(' 1,25 '), 1.25); assert.equal(parseNumber('1.25'), 1.25);
  assert.equal(parseNumber('1e2'), 100);
  for (const bad of ['', 'NaN', 'Infinity', '1e999', '1 000', '1,2,3', '0x10', 'abc'])
    assert.equal(parseNumber(bad), null);
  for (const value of [0, -1, NaN, Infinity, 1001]) {
    assert.ok(!valid({ ...defaults, f: value }));
    assert.throws(() => calculate({ ...defaults, f: value }), RangeError);
  }
  assert.throws(() => impedance(defaults, 0), RangeError);
});
test('Все 32 сочетания крайних значений', () => {
  const keys = Object.keys(limits);
  for (let mask = 0; mask < 32; mask++) {
    const p = Object.fromEntries(keys.map((key, i) => [key, limits[key][(mask >> i) & 1]]));
    const r = calculate(p);
    assert.ok(Object.values(r).every(Number.isFinite));
    assert.ok(r.z >= p.R - 1e-10); assert.ok(r.im <= p.U / p.R + 1e-10);
    near(r.ur ** 2 + (r.ul - r.uc) ** 2, p.U ** 2);
    near(impedance(p, p.f), r.z);
  }
});
test('Узкий резонанс включён в сетку частот', () => {
  const p = { ...defaults, R: 1, L: 500, C: 1 }, { f0 } = calculate(p);
  const samples = frequencySamples(p);
  assert.ok(samples.includes(f0)); near(Math.max(...samples.map(f => p.U / impedance(p, f))), p.U / p.R);
  assert.ok(samples.every((f, i) => f >= 1 && f <= 1000 && (!i || f > samples[i - 1])));
  const outside = { ...defaults, L: 1, C: 1 };
  assert.ok(calculate(outside).f0 > 1000);
  assert.ok(frequencySamples(outside).every(f => f >= 1 && f <= 1000));
});
test('JavaScript совпадает с C# на 96 контрольных цепях', () => {
  const fixtures = JSON.parse(readFileSync(new URL('./csharp-fixtures.json', import.meta.url)));
  assert.equal(fixtures.length, 96);
  for (const { p, expected } of fixtures) {
    const actual = calculate(p);
    for (const [key, value] of Object.entries(expected)) near(actual[key], value, 1e-12);
  }
});
