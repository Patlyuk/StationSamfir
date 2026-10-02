const fs = require('fs');
const path = require('path');

const src = fs.readFileSync(path.join(__dirname, '..', 'Scripts', 'Level.cs'), 'utf8');
const T = 32, WW = 208, WH = 72;
const Empty = 0, Solid = 1, Plat = 2, Gate = 3, Hazard = 4;

function matchAll(re, text) {
  const out = [];
  const r = new RegExp(re.source, re.flags.includes('g') ? re.flags : re.flags + 'g');
  let m;
  while ((m = r.exec(text))) out.push(m);
  return out;
}

const between = (a, b) => src.slice(src.indexOf(a), src.indexOf(b));

const rooms = matchAll(/new Room\("([^"]+)",\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)/g,
  between('Rooms = new List<Room>', 'static readonly Link[] Links'))
  .map(m => ({ name: m[1], x: +m[2], y: +m[3], w: +m[4], h: +m[5], z: +m[6] }));

const links = matchAll(/new Link \{ Ax = (\d+), Ay = (\d+), Bx = (\d+), By = (\d+)(?:, Req = "(\w+)")?(?:, VertFirst = (true|false))? \}/g,
  between('static readonly Link[] Links', 'static readonly Gate[] GateDefs'))
  .map(m => ({ ax: +m[1], ay: +m[2], bx: +m[3], by: +m[4], req: m[5] || '', vf: m[6] === 'true' }));

const gates = [];
for (const g of between('static readonly Gate[] GateDefs', 'static readonly PlatDef[] Plats').match(/new Gate \{[^}]*\}/g) || []) {
  gates.push({
    req: (g.match(/Req = "(\w+)"/) || [])[1] || '',
    tiles: [...g.matchAll(/new Vector2i\((\d+), (\d+)\)/g)].map(m => [+m[1], +m[2]]),
  });
}

const plats = matchAll(/new PlatDef\((\d+), (\d+), (\d+)\)/g,
  between('static readonly PlatDef[] Plats', 'static readonly HazardDef[] Hazards')).map(m => [+m[1], +m[2], +m[3]]);

const hazards = matchAll(/new HazardDef\((\d+), (\d+), (\d+), (\d+)\)/g,
  between('static readonly HazardDef[] Hazards', 'public static readonly ItemDef[] Items')).map(m => [+m[1], +m[2], +m[3], +m[4]]);

const items = matchAll(/new ItemDef\("(\w+)",\s*(\d+),\s*(\d+)/g,
  between('public static readonly ItemDef[] Items', 'public static readonly StationDef[] Stations'))
  .map(m => ({ kind: m[1], tx: +m[2], ty: +m[3] }));

const stations = matchAll(/new StationDef\("(\w+)",\s*(\d+),\s*(\d+)\)/g,
  between('public static readonly StationDef[] Stations', 'public static readonly SpawnDef[] Spawns'))
  .map(m => ({ kind: m[1], tx: +m[2], ty: +m[3] }));

const spawns = matchAll(/new SpawnDef\("(\w+)",\s*(\d+),\s*(\d+)\)/g,
  between('public static readonly SpawnDef[] Spawns', 'public static readonly Vector2 StartPos'))
  .map(m => ({ type: m[1], room: +m[2], count: +m[3] }));

const startM = src.match(/StartPos = new Vector2\((\d+) \* G\.T, (\d+) \* G\.T\)/);
const start = [+startM[1], +startM[2]];

const map = new Uint8Array(WW * WH).fill(Solid);
const set = (x, y, v) => { if (x >= 0 && y >= 0 && x < WW && y < WH) map[y * WW + x] = v; };
const get = (x, y) => (x < 0 || y < 0 || x >= WW || y >= WH ? Solid : map[y * WW + x]);

for (const r of rooms)
  for (let y = r.y; y < r.y + r.h; y++)
    for (let x = r.x; x < r.x + r.w; x++) set(x, y, Empty);

const carveH = (x1, x2, y) => { for (let x = x1; x <= x2; x++) for (let d = -1; d <= 1; d++) set(x, y + d, Empty); };
const carveV = (y1, y2, x) => { for (let y = y1; y <= y2; y++) for (let d = -1; d <= 1; d++) set(x + d, y, Empty); };

for (const l of links) {
  const hx1 = Math.min(l.ax, l.bx), hx2 = Math.max(l.ax, l.bx);
  const vy1 = Math.min(l.ay, l.by), vy2 = Math.max(l.ay, l.by);
  const horizFirst = !l.vf && Math.abs(l.bx - l.ax) >= Math.abs(l.by - l.ay);
  if (horizFirst) { carveH(hx1, hx2, l.ay); carveV(vy1, vy2, (l.ax + l.bx) / 2); }
  else { carveV(vy1, vy2, l.ax); carveH(hx1, hx2, (l.ay + l.by) / 2); }
}
for (const g of gates) for (const [x, y] of g.tiles) set(x, y, Gate);
for (const [x, y, w] of plats) for (let i = 0; i < w; i++) set(x + i, y, Plat);
for (const [x, y, w, h] of hazards) for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) set(x + i, y + j, Hazard);

let problems = 0;
const fail = (msg) => { problems++; console.log('  ! ' + msg); };
const roomOf = (x, y) => rooms.find(r => x >= r.x && x < r.x + r.w && y >= r.y && y < r.y + r.h) || null;
const gateReqAt = (x, y) => (get(x, y) === Gate ? (gates.find(g => g.tiles.some(t => t[0] === x && t[1] === y)) || {}).req : null);

console.log(`parsed: rooms=${rooms.length} links=${links.length} gates=${gates.length} plats=${plats.length} hazards=${hazards.length} items=${items.length} stations=${stations.length} spawnGroups=${spawns.length}`);

console.log('\n-- geometry sanity --');
for (let i = 0; i < rooms.length; i++)
  for (let j = i + 1; j < rooms.length; j++) {
    const a = rooms[i], b = rooms[j];
    if (a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h)
      fail(`rooms overlap: ${a.name} & ${b.name}`);
  }
for (const [x, y, w] of plats) {
  if (get(x, y) !== Plat) fail(`plat (${x},${y},${w}) sits outside open space (tile=${get(x, y)})`);
  let openAbove = 0;
  for (let i = 0; i < w; i++) if (get(x + i, y - 1) !== Solid && get(x + i, y - 1) !== Gate) openAbove++;
  if (openAbove === 0) fail(`plat (${x},${y},${w}) is fully walled in from above`);
}
function support(kind, tx, ty, label) {
  const below = get(tx, ty), free = get(tx, ty - 1);
  if (below !== Solid && below !== Plat) fail(`${label} ${kind}@${tx},${ty}: no support (tile=${below})`);
  if (free === Solid || free === Gate) console.log(`    note: ${label} ${kind}@${tx},${ty} sits under a low ceiling (tile=${free})`);
  if (!roomOf(tx, ty - 1) && !roomOf(tx, ty)) fail(`${label} ${kind}@${tx},${ty}: outside any room`);
}
for (const it of items) support(it.kind, it.tx, it.ty, 'item');
for (const st of stations) support(st.kind, st.tx, st.ty, 'station');

const byKind = {};
for (const it of items) byKind[it.kind] = (byKind[it.kind] || 0) + 1;
for (const k of ['blue', 'red', 'double_jump', 'dash', 'rifle'])
  if (byKind[k] !== 1) fail(`'${k}' placed ${byKind[k] || 0} time(s), expected exactly 1`);

console.log('\n-- platforming reachability --');

function passableFor(standing, v, keys) {
  if (v === Solid) return false;
  if (v === Gate) return keys.includes(gateReqAt(standing.x, standing.y) ?? '');
  return true;
}

function standable(x, y, keys) {
  const s = get(x, y), f = get(x, y - 1);
  const solidSupport = s === Plat || (s === Solid);
  const gateSupport = s === Gate && keys.includes(gateReqAt(x, y) ?? '');
  if (!solidSupport && !gateSupport) return false;
  if (f === Solid || f === Gate && !keys.includes(gateReqAt(x, y - 1) ?? '')) return false;
  return true;
}

function explore(keys, doubleJump, startSpot) {
  const seen = new Set();
  const key = (x, y) => y * WW + x;
  if (!standable(startSpot.x, startSpot.y, keys)) return { seen, startBlocked: true };
  const q = [startSpot];
  seen.add(key(startSpot.x, startSpot.y));
  const maxRise = doubleJump ? 4 : 2;
  while (q.length) {
    const c = q.pop();
    const push = (x, y) => {
      if (x < 0 || y < 0 || x >= WW || y >= WH) return;
      if (!standable(x, y, keys)) return;
      const k = key(x, y);
      if (seen.has(k)) return;
      seen.add(k);
      q.push({ x, y });
    };
    push(c.x - 1, c.y);
    push(c.x + 1, c.y);
    for (let dy = 1; dy <= 34; dy++)
      for (let dx = -3; dx <= 3; dx++)
        if (standable(c.x + dx, c.y + dy, keys)) { push(c.x + dx, c.y + dy); dx = 4; dy = 34; break; }
    for (let rise = 1; rise <= maxRise; rise++)
      for (let dx = -4; dx <= 4; dx++)
        if (Math.abs(dx) <= (rise <= 2 ? 4 : 2)) push(c.x + dx, c.y - rise);
  }
  return { seen, startBlocked: false };
}

const spots = [];
const spotKey = (x, y) => y * WW + x;

function accumulate(keys, doubleJump, acc) {
  const res = explore(keys, doubleJump, { x: start[0], y: start[1] });
  if (res.startBlocked) fail(`start spot (${start[0]},${start[1]}) is not standable with keys=[${keys}]`);
  for (const k of res.seen) acc.seen.add(k);
  acc.spots.push(...[...res.seen].map(k => ({ x: k % WW, y: Math.floor(k / WW) })));
  return res;
}

function reachableAt(acc, tx, ty) {
  for (let dx = -1; dx <= 1; dx++)
    if (acc.has(spotKey(tx + dx, ty))) return true;
  return false;
}

const acc0 = { seen: new Set(), spots: [] };
const accBlue = { seen: new Set(), spots: [] };
const accDj = { seen: new Set(), spots: [] };
const accRed = { seen: new Set(), spots: [] };

accumulate([], false, acc0);
accumulate(['blue'], false, accBlue);
accumulate(['blue'], true, accDj);
accumulate(['blue', 'red'], true, accRed);

function report(label, acc) {
  const missed = [];
  for (const it of items) if (!reachableAt(acc.seen, it.tx, it.ty)) missed.push(`${it.kind}@${it.tx},${it.ty}`);
  for (const st of stations) if (!reachableAt(acc.seen, st.tx, st.ty)) missed.push(`${st.kind}#${st.tx},${st.ty}`);
  const reachedRooms = rooms.filter(r => {
    for (let y = r.y; y < r.y + r.h; y++)
      for (let x = r.x; x < r.x + r.w; x++)
        if (acc.seen.has(spotKey(x, y))) return true;
    return false;
  }).map(r => r.name);
  console.log(`${label}: stand spots=${acc.seen.size}, rooms=${reachedRooms.length}/${rooms.length}` +
    (missed.length ? `\n    unreachable: ${missed.join(', ')}` : ' — all items/stations reachable'));
  return missed;
}

const m0 = report('no keys            ', acc0);
const mB = report('with BLUE          ', accBlue);
const mD = report('BLUE + double jump ', accDj);
const mR = report('BLUE + RED + dj    ', accRed);

for (const miss of m0) fail(`unreachable without keys: ${miss}`);
for (const miss of mB) fail(`unreachable with BLUE: ${miss}`);
for (const miss of mD) fail(`unreachable with BLUE+double jump: ${miss}`);
for (const miss of mR) fail(`unreachable with BLUE+RED+double jump: ${miss}`);

console.log('\n-- enemy spawn sanity --');
for (const sp of spawns) {
  const r = rooms[sp.room];
  for (let i = 0; i < sp.count; i++) {
    const x = r.x + 1 + ((i + 1) * 3 + sp.type.length) % Math.max(1, r.w - 2);
    let floor = -1;
    for (let y = r.y; y <= r.y + r.h + 2 && y < WH; y++) if (get(x, y) === Solid || get(x, y) === Plat) { floor = y; break; }
    if (floor < 0) fail(`spawn ${sp.type} in ${r.name} at x=${x} has no ground below`);
  }
}

console.log('\n' + (problems ? `FAILED: ${problems} problem(s)` : 'OK: level is consistent and fully completable'));
process.exit(problems ? 1 : 0);