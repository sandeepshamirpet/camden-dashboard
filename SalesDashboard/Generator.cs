using System.Text.Json;

public static class Generator
{
    private static readonly JsonSerializerOptions JSON_OPTS = new() { WriteIndented = false };

    public static string Generate(DashboardData data)
    {
        string leadsDaily   = JsonSerializer.Serialize(data.LeadsDaily,   JSON_OPTS);
        string leadsSrc     = JsonSerializer.Serialize(data.LeadsSrc,     JSON_OPTS);
        string leadsAoi     = JsonSerializer.Serialize(data.LeadsAoi,     JSON_OPTS);
        string opps         = JsonSerializer.Serialize(data.Opps,         JSON_OPTS);
        string generated    = DateTime.Now.ToString("MMM dd, yyyy HH:mm");

        string html = Template
            .Replace("__LEADS_DAILY__", leadsDaily)
            .Replace("__LEADS_SRC__",   leadsSrc)
            .Replace("__LEADS_AOI__",   leadsAoi)
            .Replace("__OPPS__",        opps)
            .Replace("__DATA_AS_OF__",  data.DataAsOf)
            .Replace("__GENERATED__",   generated);

        // Use current working directory so output lands in the repo root regardless of run method
        string outFile = Path.Combine(Directory.GetCurrentDirectory(), "sales_management_dashboard_v2.html");
        File.WriteAllText(outFile, html, System.Text.Encoding.UTF8);
        return outFile;
    }

    // ── HTML Template ────────────────────────────────────────────────────────
    private const string Template = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Sales Management Dashboard – Camden Homes</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',system-ui,sans-serif;background:#f0f2f5;color:#1a1a2e;min-height:100vh}
.header{background:linear-gradient(135deg,#1a1a2e 0%,#16213e 100%);color:#fff;padding:18px 30px;display:flex;align-items:center;justify-content:space-between}
.header h1{font-size:1.5rem;font-weight:700;letter-spacing:.5px}
.header .sub{font-size:.78rem;color:#8892b0;margin-top:3px}
.header .brand{font-size:1.1rem;font-weight:600;color:#64ffda}
.date-bar{background:#fff;padding:10px 30px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;border-bottom:1px solid #e2e8f0;box-shadow:0 1px 3px rgba(0,0,0,.06)}
.date-bar label{font-size:.72rem;font-weight:700;color:#64748b;text-transform:uppercase;letter-spacing:.8px;margin-right:4px}
.dbtn{padding:5px 13px;border:1.5px solid #cbd5e1;border-radius:20px;background:#fff;font-size:.78rem;font-weight:500;cursor:pointer;transition:all .2s;color:#475569}
.dbtn:hover{border-color:#3b82f6;color:#3b82f6}
.dbtn.active{background:#1e40af;border-color:#1e40af;color:#fff;font-weight:600}
.showing{margin-left:auto;font-size:.75rem;color:#64748b}
.showing strong{color:#1e40af}
.kpi-row{display:grid;grid-template-columns:repeat(4,1fr);gap:14px;padding:18px 30px 6px}
.kpi{background:#fff;border-radius:12px;padding:18px 22px;box-shadow:0 2px 8px rgba(0,0,0,.06)}
.kpi .label{font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.8px;color:#64748b;margin-bottom:8px}
.kpi .val{font-size:2.2rem;font-weight:800;line-height:1}
.kpi .range{font-size:.72rem;color:#94a3b8;margin-top:6px}
.kpi.leads  {border-top:3px solid #3b82f6} .kpi.leads  .val{color:#1e40af}
.kpi.opps   {border-top:3px solid #8b5cf6} .kpi.opps   .val{color:#6d28d9}
.kpi.cw     {border-top:3px solid #10b981} .kpi.cw     .val{color:#065f46}
.kpi.cl     {border-top:3px solid #ef4444} .kpi.cl     .val{color:#991b1b}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:14px;padding:6px 30px 22px}
.card{background:#fff;border-radius:12px;padding:18px;box-shadow:0 2px 8px rgba(0,0,0,.06)}
.card-title{font-size:.78rem;font-weight:700;text-transform:uppercase;letter-spacing:.6px;color:#374151;margin-bottom:14px;display:flex;justify-content:space-between;align-items:center}
.card-title .tag{font-size:.68rem;color:#6366f1;font-weight:600;background:#eef2ff;padding:2px 8px;border-radius:10px}
#map{height:320px;border-radius:8px}
.chart-wrap{position:relative;height:280px}
.footer{text-align:center;padding:10px;font-size:.72rem;color:#94a3b8;border-top:1px solid #e2e8f0}
@media(max-width:1100px){.grid{grid-template-columns:1fr}}
@media(max-width:900px){.kpi-row{grid-template-columns:repeat(2,1fr)}}
</style>
</head>
<body>

<div class="header">
  <div>
    <div class="h1">Sales Management Dashboard</div>
    <div class="sub">Leads &middot; Opportunities &middot; Closed Won &middot; Closed Lost</div>
  </div>
  <div class="brand">Camden Homes</div>
</div>

<div class="date-bar">
  <label>DATE RANGE</label>
  <button class="dbtn" data-r="last-month">Last Month</button>
  <button class="dbtn" data-r="cur-month">Current Month</button>
  <button class="dbtn" data-r="last-week">Last Week</button>
  <button class="dbtn" data-r="last-10">Last 10 Days</button>
  <button class="dbtn" data-r="last-20">Last 20 Days</button>
  <button class="dbtn" data-r="last-30">Last 30 Days</button>
  <button class="dbtn" data-r="last-60">Last 60 Days</button>
  <button class="dbtn" data-r="last-90">Last 90 Days</button>
  <button class="dbtn" data-r="ytd">YTD</button>
  <button class="dbtn" data-r="last-year">Last Year</button>
  <div class="showing" id="showing"></div>
</div>

<div class="kpi-row">
  <div class="kpi leads"><div class="label">Leads Created</div><div class="val" id="kLeads">-</div><div class="range" id="kLeadsRange"></div></div>
  <div class="kpi opps"><div class="label">Opportunities Created</div><div class="val" id="kOpps">-</div><div class="range" id="kOppsRange"></div></div>
  <div class="kpi cw"><div class="label">Closed Won</div><div class="val" id="kCw">-</div><div class="range" id="kCwRange"></div></div>
  <div class="kpi cl"><div class="label">Closed Lost</div><div class="val" id="kCl">-</div><div class="range" id="kClRange"></div></div>
</div>

<div class="grid">
  <div class="card">
    <div class="card-title">Leads by Community (Area of Interest)<span class="tag">Geo Map</span></div>
    <div id="map"></div>
  </div>
  <div class="card">
    <div class="card-title">Leads by Source<span class="tag">Lead Source</span></div>
    <div class="chart-wrap"><canvas id="ch-source"></canvas></div>
  </div>
  <div class="card">
    <div class="card-title">Opportunities Created by Owner<span class="tag">Opportunities</span></div>
    <div class="chart-wrap"><canvas id="ch-owner"></canvas></div>
  </div>
  <div class="card">
    <div class="card-title">Closed Won by Community<span class="tag">Won</span></div>
    <div class="chart-wrap"><canvas id="ch-cw"></canvas></div>
  </div>
  <div class="card" style="grid-column:1/-1">
    <div class="card-title">Closed Lost / Cancellations by Source<span class="tag">Lost</span></div>
    <div class="chart-wrap" style="height:220px"><canvas id="ch-cl"></canvas></div>
  </div>
</div>

<div class="footer">
  Data source: Salesforce (Camden Homes) &nbsp;&middot;&nbsp; Data as of: <strong>__DATA_AS_OF__</strong> &nbsp;&middot;&nbsp; Generated: __GENERATED__
</div>

<script>
// ── Data injected by C# ──────────────────────────────────────────────────
// LEADS_DAILY: [{Date,Count}]  daily lead totals in CST (exact from Salesforce)
// LEADS_SRC:   [{Month,Src,Count}]  monthly leads by lead source
// LEADS_AOI:   [{Month,Aoi,Count}]  monthly leads by area of interest
// OPPS:        [{Date,Owner,Stage,Comm,Src}]  every opportunity (CST date)
const LEADS_DAILY = __LEADS_DAILY__;
const LEADS_SRC   = __LEADS_SRC__;
const LEADS_AOI   = __LEADS_AOI__;
const OPPS        = __OPPS__;

// ── Geo coordinates for communities ────────────────────────────────────
const GEO = {
  "Fort Worth":     [32.7555, -97.3308],
  "Dallas":         [32.7767, -96.7970],
  "Frisco":         [33.1507, -96.8236],
  "McKinney":       [33.1972, -96.6398],
  "Prosper":        [33.2362, -96.8003],
  "Celina":         [33.3248, -96.7836],
  "Allen":          [33.1032, -96.6705],
  "Plano":          [33.0198, -96.6989],
  "Arlington":      [32.7357, -97.1081],
  "Mansfield":      [32.5632, -97.1417],
  "Burleson":       [32.5421, -97.3208],
  "Midlothian":     [32.4821, -96.9939],
  "Waxahachie":     [32.3868, -96.8489],
  "Weatherford":    [32.7590, -97.7975],
  "Denton":         [33.2148, -97.1331],
  "Flower Mound":   [33.0146, -97.0961],
  "Little Elm":     [33.1629, -96.9375],
  "Aubrey":         [33.3076, -96.9886],
  "Gunter":         [33.4432, -96.7453],
  "Sherman":        [33.6357, -96.6089],
  "Wylie":          [33.0151, -96.5388],
  "Rockwall":       [32.9290, -96.4597],
  "Garland":        [32.9126, -96.6389],
  "Rowlett":        [32.9029, -96.5639],
  "Sachse":         [32.9765, -96.5786],
  "Murphy":         [33.0126, -96.6113],
  "Forney":         [32.7474, -96.4697],
  "Terrell":        [32.7357, -96.2752],
  "Kaufman":        [32.5874, -96.3063],
  "Azle":           [32.8957, -97.5467],
  "Keller":         [32.9343, -97.2294],
  "Southlake":      [32.9440, -97.1342],
  "Grapevine":      [32.9343, -97.0781],
  "Euless":         [32.8371, -97.0819],
  "Hurst":          [32.8232, -97.1886],
  "Bedford":        [32.8443, -97.1436],
  "North Richland Hills": [32.8343, -97.2289],
  "Haltom City":    [32.7993, -97.2697],
  "Saginaw":        [32.8593, -97.3644],
  "Lake Worth":     [32.8057, -97.4328],
  "Aledo":          [32.6974, -97.6025],
  "Cleburne":       [32.3501, -97.3886],
  "Granbury":       [32.4418, -97.7947],
  "Stephenville":   [32.2207, -98.2023],
  "Crowley":        [32.5793, -97.3622],
  "Kennedale":      [32.6457, -97.2264],
  "Duncanville":    [32.6526, -96.9083],
  "DeSoto":         [32.5896, -96.8572],
  "Cedar Hill":     [32.5885, -96.9561],
  "Lancaster":      [32.5924, -96.7561],
  "Ennis":          [32.3290, -96.6252],
  "Italy":          [32.1818, -96.8861],
  "Waco":           [31.5493, -97.1467],
  "Temple":         [31.0982, -97.3428],
  "Killeen":        [31.1171, -97.7278],
  "Georgetown":     [30.6333, -97.6789],
  "Round Rock":     [30.5083, -97.6789],
  "Cedar Park":     [30.5052, -97.8203],
  "Leander":        [30.5788, -97.8531],
  "Pflugerville":   [30.4388, -97.6200],
  "Hutto":          [30.5427, -97.5461],
  "Kyle":           [29.9891, -97.8772],
  "Buda":           [30.0852, -97.8408],
  "San Marcos":     [29.8833, -97.9414],
  "New Braunfels":  [29.7030, -98.1245],
  "Seguin":         [29.5688, -97.9642],
  "Conroe":         [30.3119, -95.4561],
  "Magnolia":       [30.2099, -95.7527],
  "Montgomery":     [30.3877, -95.6966],
  "Tomball":        [30.0974, -95.6155],
  "Spring":         [30.0799, -95.4172],
  "Katy":           [29.7858, -95.8244],
  "Sugar Land":     [29.6197, -95.6349],
  "Missouri City":  [29.6185, -95.5377],
  "Pearland":       [29.5636, -95.2860],
  "League City":    [29.5075, -95.0949],
  "Friendswood":    [29.5294, -95.2010],
  "Manvel":         [29.4613, -95.3577],
  "Alvin":          [29.4238, -95.2438],
  "Angleton":       [29.1694, -95.4316],
  "Lake Jackson":   [29.0344, -95.4344],
  "Clute":          [29.0194, -95.3977]
};

// ── Palette ─────────────────────────────────────────────────────────────
const PALETTE = ['#3b82f6','#10b981','#f59e0b','#8b5cf6','#ef4444',
                 '#06b6d4','#f97316','#84cc16','#ec4899','#14b8a6',
                 '#a855f7','#6366f1'];

// ── Chart instances ──────────────────────────────────────────────────────
let charts = {};

// ── Helpers ──────────────────────────────────────────────────────────────
function fmt(n) { return n.toLocaleString(); }
function fmtDate(d) {
  return d.toLocaleDateString('en-US', {month:'short', day:'numeric', year:'numeric'});
}
function ym(d) { return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`; }

function topN(map, n) {
  return Object.entries(map).sort((a,b) => b[1]-a[1]).slice(0,n);
}

function buildBar(id, labels, values, colors, unit, existing) {
  if (existing) existing.destroy();
  const ctx = document.getElementById(id);
  if (!ctx) return null;
  return new Chart(ctx, {
    type: 'bar',
    data: {
      labels,
      datasets: [{ data: values, backgroundColor: colors.slice(0, labels.length), borderRadius: 4 }]
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: c => ` ${fmt(c.parsed.x)} ${unit}` } }
      },
      scales: {
        x: { grid: { color: '#f1f5f9' }, ticks: { font: { size: 11 } } },
        y: { grid: { display: false }, ticks: { font: { size: 11 } } }
      }
    }
  });
}

// ── Map setup ────────────────────────────────────────────────────────────
let map = null, mapMarkers = [];

function initMap() {
  map = L.map('map', { zoomControl: true }).setView([32.8, -97.1], 7);
  L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; <a href="https://carto.com/">CartoDB</a>',
    maxZoom: 18
  }).addTo(map);
}

function updateMap(aoiMap) {
  mapMarkers.forEach(m => map.removeLayer(m));
  mapMarkers = [];
  if (!aoiMap || Object.keys(aoiMap).length === 0) return;

  const maxVal = Math.max(...Object.values(aoiMap));
  for (const [name, cnt] of Object.entries(aoiMap)) {
    if (cnt === 0) continue;
    const coords = GEO[name] || findGeoFuzzy(name);
    if (!coords) continue;
    const r = 8 + Math.round((cnt / maxVal) * 28);
    const circle = L.circleMarker(coords, {
      radius: r, color: '#2563eb', fillColor: '#3b82f6',
      fillOpacity: 0.55, weight: 1.5
    }).bindPopup(`<b>${name}</b><br>${fmt(cnt)} leads`);
    circle.addTo(map);
    mapMarkers.push(circle);
  }
}

function findGeoFuzzy(name) {
  const lc = name.toLowerCase();
  for (const [k, v] of Object.entries(GEO))
    if (k.toLowerCase().includes(lc) || lc.includes(k.toLowerCase())) return v;
  return null;
}

// ── Date range calculator ────────────────────────────────────────────────
function getRange(r) {
  let t = new Date(); t.setHours(23, 59, 59, 999);
  let s = new Date();

  if (r === 'last-month') {
    s = new Date(t.getFullYear(), t.getMonth() - 1, 1);
    t = new Date(t.getFullYear(), t.getMonth(), 0, 23, 59, 59, 999);
  } else if (r === 'cur-month') {
    s = new Date(t.getFullYear(), t.getMonth(), 1);
  } else if (r === 'last-week') {
    const dow = t.getDay();
    const daysSinceMon = dow === 0 ? 6 : dow - 1;
    const thisMon = new Date(t); thisMon.setDate(t.getDate() - daysSinceMon); thisMon.setHours(0,0,0,0);
    const lastSun = new Date(thisMon); lastSun.setDate(thisMon.getDate() - 1); lastSun.setHours(23,59,59,999);
    const lastMon = new Date(lastSun); lastMon.setDate(lastSun.getDate() - 6); lastMon.setHours(0,0,0,0);
    const ms = new Set([ym(lastMon)]); if (ym(lastMon) !== ym(lastSun)) ms.add(ym(lastSun));
    return { start: lastMon, end: lastSun, months: ms };
  } else if (r === 'last-10') { s = new Date(t); s.setDate(t.getDate() - 9);
  } else if (r === 'last-20') { s = new Date(t); s.setDate(t.getDate() - 19);
  } else if (r === 'last-30') { s = new Date(t); s.setDate(t.getDate() - 29);
  } else if (r === 'last-60') { s = new Date(t); s.setDate(t.getDate() - 59);
  } else if (r === 'last-90') { s = new Date(t); s.setDate(t.getDate() - 89);
  } else if (r === 'ytd')     { s = new Date(t.getFullYear(), 0, 1);
  } else if (r === 'last-year') {
    s = new Date(t.getFullYear() - 1, 0, 1);
    t = new Date(t.getFullYear() - 1, 11, 31, 23, 59, 59, 999);
  }

  s.setHours(0, 0, 0, 0);

  // Build months set
  const ms = new Set();
  const cur = new Date(s.getFullYear(), s.getMonth(), 1);
  const last = new Date(t.getFullYear(), t.getMonth(), 1);
  while (cur <= last) { ms.add(ym(cur)); cur.setMonth(cur.getMonth() + 1); }

  return { start: s, end: t, months: ms };
}

// ── Main update ──────────────────────────────────────────────────────────
function update(r) {
  const { start, end, months } = getRange(r);

  // ── KPI: Leads (from LEADS_DAILY — exact CST dates, no UTC adjustment needed) ──
  const fLeadsDaily = LEADS_DAILY.filter(d => {
    const dt = new Date(d.Date + 'T00:00:00');
    return dt >= start && dt <= end;
  });
  const totalLeads = fLeadsDaily.reduce((s, d) => s + d.Count, 0);

  // ── KPI: Opportunities, Closed Won, Closed Lost (from OPPS — CST dates) ──
  const fOpps = OPPS.filter(d => {
    const dt = new Date(d.Date + 'T00:00:00');
    return dt >= start && dt <= end;
  });
  const totalOpps = fOpps.length;
  const fCw = fOpps.filter(d => d.Stage === 'Closed Won');
  const fCl = fOpps.filter(d => d.Stage === 'Closed Lost');
  const totalCW = fCw.length;
  const totalCL = fCl.length;

  // ── Source chart (from LEADS_SRC — monthly granularity) ─────────────
  const srcMap = {};
  for (const d of LEADS_SRC.filter(d => months.has(d.Month)))
    srcMap[d.Src] = (srcMap[d.Src] || 0) + d.Count;

  // ── Map (from LEADS_AOI — monthly granularity) ───────────────────────
  const aoiMap = {};
  for (const d of LEADS_AOI.filter(d => months.has(d.Month)))
    aoiMap[d.Aoi] = (aoiMap[d.Aoi] || 0) + d.Count;

  // ── Opps by Owner ────────────────────────────────────────────────────
  const ownerMap = {};
  for (const d of fOpps) ownerMap[d.Owner] = (ownerMap[d.Owner] || 0) + 1;

  // ── CW by Community ─────────────────────────────────────────────────
  const cwMap = {};
  for (const d of fCw) cwMap[d.Comm] = (cwMap[d.Comm] || 0) + 1;

  // ── CL by Source ─────────────────────────────────────────────────────
  const clMap = {};
  for (const d of fCl) clMap[d.Src] = (clMap[d.Src] || 0) + 1;

  // ── Update KPI cards ─────────────────────────────────────────────────
  const rangeLabel = `${fmtDate(start)} – ${fmtDate(end)}`;
  document.getElementById('kLeads').textContent    = fmt(totalLeads);
  document.getElementById('kLeadsRange').textContent = rangeLabel;
  document.getElementById('kOpps').textContent     = fmt(totalOpps);
  document.getElementById('kOppsRange').textContent  = rangeLabel;
  document.getElementById('kCw').textContent       = fmt(totalCW);
  document.getElementById('kCwRange').textContent    = rangeLabel;
  document.getElementById('kCl').textContent       = fmt(totalCL);
  document.getElementById('kClRange').textContent    = rangeLabel;

  // Date range label in toolbar
  const btnLabel = document.querySelector(`.dbtn[data-r="${r}"]`)?.textContent || r;
  document.getElementById('showing').innerHTML =
    `Showing: <strong>${btnLabel}</strong> | ${fmtDate(start)} &mdash; ${fmtDate(end)}`;

  // ── Update charts ─────────────────────────────────────────────────────
  const srcTop = topN(srcMap, 12);
  charts.source = buildBar('ch-source', srcTop.map(x=>x[0]), srcTop.map(x=>x[1]),
                           PALETTE, 'leads', charts.source);

  const owTop = topN(ownerMap, 12);
  charts.owner = buildBar('ch-owner', owTop.map(x=>x[0]), owTop.map(x=>x[1]),
                          PALETTE.slice(3), 'opps', charts.owner);

  const cwTop = topN(cwMap, 12);
  charts.cw = buildBar('ch-cw', cwTop.map(x=>x[0]), cwTop.map(x=>x[1]),
                       ['#10b981'], 'won', charts.cw);

  const clTop = topN(clMap, 12);
  charts.cl = buildBar('ch-cl', clTop.map(x=>x[0]), clTop.map(x=>x[1]),
                       ['#ef4444'], 'lost', charts.cl);

  // ── Update map ────────────────────────────────────────────────────────
  updateMap(aoiMap);
}

// ── Init ─────────────────────────────────────────────────────────────────
initMap();

document.querySelectorAll('.dbtn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.dbtn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    update(btn.dataset.r);
  });
});

// Default: Current Month
document.querySelector('[data-r="cur-month"]').click();
</script>
</body>
</html>
""";
}
