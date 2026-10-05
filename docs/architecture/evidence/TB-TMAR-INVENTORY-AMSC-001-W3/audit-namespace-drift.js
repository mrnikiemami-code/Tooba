// TB-TMAR-INVENTORY-AMSC-001-W3 — scan every certified module project for path<->namespace
// mismatches (the same rule TmarCompleteReferenceStructureGateTests enforces) to size the inherited
// drift precisely before deciding on a repair.
const fs = require('fs');
const path = require('path');

const manifest = JSON.parse(
  fs.readFileSync('docs/architecture/tmar-module-structure-manifests.json', 'utf8').replace(/^\uFEFF/, ''));

const violations = [];
for (const module of manifest.modules) {
  for (const project of module.projects) {
    const projectPath = path.join('src/backend/Modules', module.module, project.projectName);
    if (!fs.existsSync(projectPath)) {
      violations.push(`${project.projectName}: MISSING PROJECT DIRECTORY`);
      continue;
    }
    const walk = (dir) => {
      for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
          if (entry.name === 'bin' || entry.name === 'obj') continue;
          walk(full);
        } else if (entry.name.endsWith('.cs')) {
          const relative = path.relative(projectPath, full);
          if (path.basename(relative).toLowerCase().startsWith('globalusings')) continue;
          const dir2 = path.dirname(relative);
          const expected = dir2 === '.' ? project.projectName : project.projectName + '.' + dir2.split(path.sep).join('.');
          const text = fs.readFileSync(full, 'utf8').replace(/^\uFEFF/, '');
          const match = text.match(/^namespace\s+([A-Za-z0-9_.]+)/m);
          if (!match) {
            violations.push(`${module.module}/${relative}: NO NAMESPACE`);
          } else if (match[1] !== expected) {
            violations.push(`${module.module}/${relative}: expected ${expected}, got ${match[1]}`);
          }
        }
      }
    };
    walk(projectPath);
  }
}

console.log(`certified modules: ${manifest.modules.length}`);
console.log(`path<->namespace violations: ${violations.length}`);
for (const v of violations) console.log(' - ' + v);
