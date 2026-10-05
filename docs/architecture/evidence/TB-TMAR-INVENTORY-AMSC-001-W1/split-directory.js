const fs = require('fs');
const path = require('path');

const dir = process.argv[2];
const mainPath = path.join(dir, 'InventoryDirectory.cs');
const src = fs.readFileSync(mainPath, 'utf8');
const eol = src.includes('\r\n') ? '\r\n' : '\n';
const lines = src.split(/\r?\n/);

const idx = (pred, from = 0) => {
  for (let i = from; i < lines.length; i++) { if (pred(lines[i])) return i; }
  return -1;
};

// ---- 1. extract OpenInventoryUseCaseGuard ----
const guardClass = idx(l => l.includes('public sealed class OpenInventoryUseCaseGuard'));
const guardDoc = guardClass - 3;            // /// <summary> ... /// </summary>
const guardEnd = idx(l => l.trim() === '}', guardClass + 1);
if (guardClass < 0 || guardEnd < 0) throw new Error('guard block not found');

const guardLines = lines.slice(guardDoc, guardEnd + 1);
const guardHeader = [
  'using Tooba.Inventory.Application.Ports;',
  '',
  'namespace Tooba.Inventory.Infrastructure.Directories;',
  '',
];
fs.writeFileSync(
  path.join(dir, 'OpenInventoryUseCaseGuard.cs'),
  guardHeader.join(eol) + guardLines.join(eol) + eol,
  'utf8');

// remove guard block + the single following blank line
const afterGuard = guardEnd + 1;
const dropEnd = (lines[afterGuard] === '') ? afterGuard + 1 : afterGuard;
const withoutGuard = lines.slice(0, guardDoc).concat(lines.slice(dropEnd));

// ---- 2. extract the order-supply engine region ----
const supplyStart = idx(l => l.includes('public async Task<OrderSupplyStatus> GetOrderSupplyStatusAsync('));
const supplyEnd = idx(l => l.includes('private static bool IsReservationIdempotencyConflict('));
if (supplyStart < 0 || supplyEnd < 0) throw new Error('supply region not found');

// include the preceding "/// <inheritdoc />" line and drop the following blank line
let regionStart = supplyStart;
while (regionStart > 0 && withoutGuard[regionStart - 1].trim().startsWith('///')) regionStart--;
let regionEnd = supplyEnd;
while (regionEnd > regionStart && withoutGuard[regionEnd - 1].trim() === '') regionEnd--;

const supplyLines = withoutGuard.slice(regionStart, regionEnd);
const remaining = withoutGuard.slice(0, regionStart)
  .concat(withoutGuard.slice(supplyEnd));

fs.writeFileSync(mainPath, remaining.join(eol), 'utf8');

const supplyHeader = [
  'using Tooba.BuildingBlocks;',
  'using Tooba.Inventory.Application.Orders;',
  'using Tooba.Inventory.Application.Ports;',
  'using Tooba.Inventory.Domain.ValueObjects;',
  'using Tooba.Inventory.Infrastructure.Persistence;',
  'using Microsoft.EntityFrameworkCore;',
  '',
  'namespace Tooba.Inventory.Infrastructure.Directories;',
  '',
  '/// <summary>',
  '/// ترجمهٔ تأمین سفارش (order-supply) روی موقعیت‌های موجودی.',
  '/// هم‌کار cohesive با InventoryDirectory: انتخاب محل/رزرو/آزادسازی در دایرکتوری می‌ماند.',
  '/// </summary>',
  'public sealed partial class InventoryDirectory',
  '{',
];
// convert "public sealed class InventoryDirectory" already absent in the partial region,
// so wrap the members inside the partial class body.
const supplyBody = supplyLines.map(l => (l.trim() === '' ? l : '    ' + l));
fs.writeFileSync(
  path.join(dir, 'InventoryDirectory.OrderSupply.cs'),
  supplyHeader.join(eol) + eol + supplyBody.join(eol) + eol + '}' + eol,
  'utf8');

console.log('guard block lines:', guardLines.length);
console.log('supply region lines:', supplyLines.length);
console.log('main remaining lines:', remaining.length);
