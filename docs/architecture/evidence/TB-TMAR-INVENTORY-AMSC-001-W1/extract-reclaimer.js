const fs = require('fs');
const path = require('path');

const dir = process.argv[2];
const mainPath = path.join(dir, 'InventoryDirectory.cs');
const src = fs.readFileSync(mainPath, 'utf8');
const eol = src.includes('\r\n') ? '\r\n' : '\n';
const lines = src.split(/\r?\n/);

const idxOf = (pred, from = 0) => {
  for (let i = from; i < lines.length; i++) { if (pred(lines[i])) return i; }
  return -1;
};

const startMethod = idxOf(l => l.includes('public async Task<int> ReleaseExpiredHoldsAsync('));
const endMethod = idxOf(l => l.includes('public async Task<ReservationReceipt?> FindReservationAsync('));
if (startMethod < 0 || endMethod < 0 || endMethod <= startMethod) throw new Error('anchors not found');

let regionStart = startMethod;
while (regionStart > 0 && lines[regionStart - 1].trim().startsWith('///')) regionStart--;

let regionEnd = endMethod;                       // exclusive
while (regionEnd > regionStart && lines[regionEnd - 1].trim() === '') regionEnd--;

const region = lines.slice(regionStart, regionEnd);

// remove the region plus exactly one preceding blank separator line from the main file
let removeStart = regionStart;
while (removeStart > 0 && lines[removeStart - 1].trim() === '') { removeStart--; break; }

const header = [
  'using Tooba.Inventory.Application.Ports;',
  'using Microsoft.EntityFrameworkCore;',
  '',
  'namespace Tooba.Inventory.Infrastructure.Directories;',
  '',
  '/// <summary>',
  '/// Expired-hold reclaimer for <see cref="InventoryDirectory"/>: batch-wise release of Held',
  '/// reservations whose TTL elapsed, using PostgreSQL FOR UPDATE SKIP LOCKED inside one transaction',
  '/// per batch. Extracted into a cohesive partial of the same class so behavior is unchanged.',
  '/// </summary>',
  'public sealed partial class InventoryDirectory',
  '{',
];
fs.writeFileSync(
  path.join(dir, 'InventoryDirectory.Reclaimer.cs'),
  [...header, ...region, '}', ''].join(eol),
  'utf8');

const classIdx = idxOf(l => l.includes('public sealed class InventoryDirectory :'));
if (classIdx < 0) throw new Error('class declaration not found');
lines[classIdx] = lines[classIdx].replace(
  'public sealed class InventoryDirectory :',
  'public sealed partial class InventoryDirectory :');

const out = [...lines.slice(0, removeStart), ...lines.slice(regionEnd)];
fs.writeFileSync(mainPath, out.join(eol), 'utf8');

console.log('region lines:', region.length, '| main before:', lines.length, '| main after:', out.length);
