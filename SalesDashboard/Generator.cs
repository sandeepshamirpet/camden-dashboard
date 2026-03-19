using System.Text.Json;

public static class Generator
{
    private static readonly JsonSerializerOptions JSON_OPTS = new() { WriteIndented = false };

    public static string Generate(DashboardData data)
    {
        string leadsDaily   = JsonSerializer.Serialize(data.LeadsDaily,   JSON_OPTS);
        string leadsSrc     = JsonSerializer.Serialize(data.LeadsSrc,     JSON_OPTS);
        string leadsAoi     = JsonSerializer.Serialize(data.LeadsAoi,     JSON_OPTS);
        string apptsDaily   = JsonSerializer.Serialize(data.ApptsDaily,   JSON_OPTS);
        string apptsAoi     = JsonSerializer.Serialize(data.ApptsAoi,     JSON_OPTS);
        string apptsSrc     = JsonSerializer.Serialize(data.ApptsSrc,     JSON_OPTS);
        string opps         = JsonSerializer.Serialize(data.Opps,         JSON_OPTS);
        string generated    = DateTime.Now.ToString("MMM dd, yyyy HH:mm");

        string html = Template
            .Replace("__LEADS_DAILY__", leadsDaily)
            .Replace("__LEADS_SRC__",   leadsSrc)
            .Replace("__LEADS_AOI__",   leadsAoi)
            .Replace("__APPTS_DAILY__", apptsDaily)
            .Replace("__APPTS_AOI__",   apptsAoi)
            .Replace("__APPTS_SRC__",   apptsSrc)
            .Replace("__OPPS__",        opps)
            .Replace("__DATA_AS_OF__",  data.DataAsOf)
            .Replace("__GENERATED__",   generated);

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
<title>Sales Dashboard – Camden Homes</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;background:#f4f6f9;color:#222;font-size:13px}

/* ── Header ── */
.hdr{background:#1b3a6b;color:#fff;padding:14px 24px;display:flex;align-items:center;justify-content:space-between}
.hdr-title{font-size:1.25rem;font-weight:700;letter-spacing:.3px}
.hdr-sub{font-size:.75rem;color:#a8c4e8;margin-top:2px}
.hdr-brand{font-size:1rem;font-weight:600;color:#6dd5fa}

/* ── Date-range bar ── */
.dbar{background:#fff;padding:9px 24px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;border-bottom:2px solid #e0e6ef}
.dbar label{font-size:.68rem;font-weight:700;color:#6b7a99;text-transform:uppercase;letter-spacing:.8px;margin-right:4px}
.dbtn{padding:4px 12px;border:1.5px solid #cbd5e1;border-radius:14px;background:#fff;font-size:.74rem;font-weight:500;cursor:pointer;color:#4a5568;transition:all .15s}
.dbtn:hover{border-color:#1b3a6b;color:#1b3a6b}
.dbtn.active{background:#1b3a6b;border-color:#1b3a6b;color:#fff;font-weight:600}
.dbar-right{margin-left:auto;font-size:.72rem;color:#6b7a99}
.dbar-right strong{color:#1b3a6b}

/* ── KPI strip ── */
.kpi-row{display:grid;grid-template-columns:repeat(5,1fr);gap:12px;padding:14px 24px 6px}
.kpi{background:#fff;border-radius:10px;padding:14px 18px;box-shadow:0 1px 6px rgba(0,0,0,.07)}
.kpi .lbl{font-size:.65rem;font-weight:700;text-transform:uppercase;letter-spacing:.8px;color:#6b7a99;margin-bottom:6px}
.kpi .val{font-size:2rem;font-weight:800;line-height:1.1}
.kpi .sub{font-size:.68rem;color:#94a3b8;margin-top:4px}
.kpi.k-leads{border-top:3px solid #1b3a6b} .kpi.k-leads .val{color:#1b3a6b}
.kpi.k-appts{border-top:3px solid #2563eb} .kpi.k-appts .val{color:#2563eb}
.kpi.k-opps {border-top:3px solid #7c3aed} .kpi.k-opps  .val{color:#7c3aed}
.kpi.k-canc {border-top:3px solid #dc2626} .kpi.k-canc  .val{color:#dc2626}
.kpi.k-net  {border-top:3px solid #16a34a} .kpi.k-net   .val{color:#16a34a}

/* ── Section layout ── */
.section{padding:6px 24px 14px}
.sec-title{font-size:.78rem;font-weight:700;text-transform:uppercase;letter-spacing:.7px;color:#1b3a6b;margin-bottom:8px;display:flex;align-items:center;gap:8px}
.sec-title::after{content:'';flex:1;height:1px;background:#dde3ef}

/* ── Funnel tables ── */
.tbl-wrap{background:#fff;border-radius:10px;box-shadow:0 1px 6px rgba(0,0,0,.07);overflow:hidden}
table{width:100%;border-collapse:collapse}
thead tr{background:#1b3a6b;color:#fff}
thead th{padding:9px 12px;font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.6px;text-align:right;white-space:nowrap}
thead th:first-child{text-align:left}
tbody tr:nth-child(even){background:#f8fafc}
tbody tr:hover{background:#eef3ff}
tbody td{padding:7px 12px;font-size:.78rem;border-bottom:1px solid #f0f4fa;text-align:right}
tbody td:first-child{text-align:left;font-weight:500;color:#1e293b}
tfoot tr{background:#e8edf7;border-top:2px solid #c8d4e8}
tfoot td{padding:8px 12px;font-size:.78rem;font-weight:700;text-align:right;color:#1b3a6b}
tfoot td:first-child{text-align:left}
.pct{font-size:.68rem;color:#94a3b8;margin-left:3px}
.net-pos{color:#16a34a;font-weight:700}
.net-neg{color:#dc2626;font-weight:700}
.bar-cell{position:relative}
.bar-bg{position:absolute;left:0;top:0;bottom:0;background:#dbeafe;z-index:0;border-radius:0 3px 3px 0}
.bar-val{position:relative;z-index:1}

/* ── Two-column grid for tables ── */
.two-col{display:grid;grid-template-columns:1fr 1fr;gap:14px}
@media(max-width:1100px){.two-col{grid-template-columns:1fr}}

/* ── Charts row ── */
.charts-row{display:grid;grid-template-columns:1fr 1fr 1fr;gap:14px;padding:0 24px 14px}
.card{background:#fff;border-radius:10px;padding:16px;box-shadow:0 1px 6px rgba(0,0,0,.07)}
.card-title{font-size:.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.6px;color:#374151;margin-bottom:12px}
.chart-wrap{position:relative;height:260px}
#map{height:300px;border-radius:8px}

/* ── Full-width map row ── */
.map-row{padding:0 24px 14px}

/* ── Footer ── */
.footer{text-align:center;padding:10px;font-size:.68rem;color:#94a3b8;border-top:1px solid #e2e8f0}

/* ── No-data badge ── */
.no-appts{font-size:.68rem;background:#fef3c7;color:#92400e;padding:2px 7px;border-radius:8px;margin-left:6px}
</style>
</head>
<body>

<div class="hdr">
  <div>
    <div class="hdr-title">Sales Management Dashboard</div>
    <div class="hdr-sub">Leads &middot; Appointments &middot; Opportunities &middot; Cancellations</div>
  </div>
  <div class="hdr-brand">Camden Homes</div>
</div>

<div class="dbar">
  <label>Period</label>
  <button class="dbtn" data-r="last-week">Last Week</button>
  <button class="dbtn" data-r="cur-month">Current Month</button>
  <button class="dbtn" data-r="last-month">Last Month</button>
  <button class="dbtn" data-r="last-10">Last 10 Days</button>
  <button class="dbtn" data-r="last-20">Last 20 Days</button>
  <button class="dbtn" data-r="last-30">Last 30 Days</button>
  <button class="dbtn" data-r="last-60">Last 60 Days</button>
  <button class="dbtn" data-r="last-90">Last 90 Days</button>
  <button class="dbtn" data-r="ytd">YTD</button>
  <button class="dbtn" data-r="last-year">Last Year</button>
  <div class="dbar-right" id="showing"></div>
</div>

<div class="kpi-row">
  <div class="kpi k-leads"><div class="lbl">Leads Created</div><div class="val" id="kLeads">–</div><div class="sub" id="kLeadsSub"></div></div>
  <div class="kpi k-appts"><div class="lbl">Appointments <span class="no-appts" id="apptsNote" style="display:none">No data</span></div><div class="val" id="kAppts">–</div><div class="sub" id="kApptsSub"></div></div>
  <div class="kpi k-opps"><div class="lbl">Opportunities</div><div class="val" id="kOpps">–</div><div class="sub" id="kOppsSub"></div></div>
  <div class="kpi k-canc"><div class="lbl">Cancellations</div><div class="val" id="kCanc">–</div><div class="sub" id="kCancSub"></div></div>
  <div class="kpi k-net"><div class="lbl">Net</div><div class="val" id="kNet">–</div><div class="sub" id="kNetSub"></div></div>
</div>

<div class="section">
  <div class="sec-title">Summary by Community (Area of Interest)</div>
  <div class="tbl-wrap">
    <table id="tbl-community">
      <thead><tr>
        <th style="text-align:left">Community</th>
        <th>Leads</th><th>Appointments</th><th>Appt %</th>
        <th>Opportunities</th><th>Opp %</th>
        <th>Cancellations</th><th>Net</th>
      </tr></thead>
      <tbody id="tbody-community"></tbody>
      <tfoot><tr id="tfoot-community"></tr></tfoot>
    </table>
  </div>
</div>

<div class="section">
  <div class="sec-title">Summary by Lead Source</div>
  <div class="tbl-wrap">
    <table id="tbl-source">
      <thead><tr>
        <th style="text-align:left">Lead Source</th>
        <th>Leads</th><th>Appointments</th><th>Appt %</th>
        <th>Opportunities</th><th>Opp %</th>
        <th>Cancellations</th><th>Net</th>
      </tr></thead>
      <tbody id="tbody-source"></tbody>
      <tfoot><tr id="tfoot-source"></tr></tfoot>
    </table>
  </div>
</div>

<div class="map-row">
  <div class="sec-title" style="margin-bottom:8px">Leads by Community &ndash; Geo Map</div>
  <div class="card" style="padding:12px">
    <div id="map"></div>
  </div>
</div>

<div class="charts-row">
  <div class="card">
    <div class="card-title">Leads by Source</div>
    <div class="chart-wrap"><canvas id="ch-source"></canvas></div>
  </div>
  <div class="card">
    <div class="card-title">Opportunities by Owner</div>
    <div class="chart-wrap"><canvas id="ch-owner"></canvas></div>
  </div>
  <div class="card">
    <div class="card-title">Cancellations by Source</div>
    <div class="chart-wrap"><canvas id="ch-canc"></canvas></div>
  </div>
</div>

<div class="footer">
  Data source: Salesforce &nbsp;&middot;&nbsp; Data as of: <strong>__DATA_AS_OF__</strong> &nbsp;&middot;&nbsp; Generated: __GENERATED__
</div>

<script>
// ── Injected data ────────────────────────────────────────────────────────────
const LEADS_DAILY = __LEADS_DAILY__;
const LEADS_SRC   = __LEADS_SRC__;
const LEADS_AOI   = __LEADS_AOI__;
const APPTS_DAILY = __APPTS_DAILY__;
const APPTS_AOI   = __APPTS_AOI__;
const APPTS_SRC   = __APPTS_SRC__;
const OPPS        = __OPPS__;

// ── Geo coords ───────────────────────────────────────────────────────────────
const GEO = {
  "Fort Worth":[32.7555,-97.3308],"Dallas":[32.7767,-96.7970],"Frisco":[33.1507,-96.8236],
  "McKinney":[33.1972,-96.6398],"Prosper":[33.2362,-96.8003],"Celina":[33.3248,-96.7836],
  "Allen":[33.1032,-96.6705],"Plano":[33.0198,-96.6989],"Arlington":[32.7357,-97.1081],
  "Mansfield":[32.5632,-97.1417],"Burleson":[32.5421,-97.3208],"Midlothian":[32.4821,-96.9939],
  "Waxahachie":[32.3868,-96.8489],"Weatherford":[32.7590,-97.7975],"Denton":[33.2148,-97.1331],
  "Flower Mound":[33.0146,-97.0961],"Little Elm":[33.1629,-96.9375],"Aubrey":[33.3076,-96.9886],
  "Gunter":[33.4432,-96.7453],"Sherman":[33.6357,-96.6089],"Wylie":[33.0151,-96.5388],
  "Rockwall":[32.9290,-96.4597],"Garland":[32.9126,-96.6389],"Rowlett":[32.9029,-96.5639],
  "Sachse":[32.9765,-96.5786],"Murphy":[33.0126,-96.6113],"Forney":[32.7474,-96.4697],
  "Terrell":[32.7357,-96.2752],"Kaufman":[32.5874,-96.3063],"Azle":[32.8957,-97.5467],
  "Keller":[32.9343,-97.2294],"Southlake":[32.9440,-97.1342],"Grapevine":[32.9343,-97.0781],
  "Euless":[32.8371,-97.0819],"Hurst":[32.8232,-97.1886],"Bedford":[32.8443,-97.1436],
  "North Richland Hills":[32.8343,-97.2289],"Haltom City":[32.7993,-97.2697],
  "Saginaw":[32.8593,-97.3644],"Lake Worth":[32.8057,-97.4328],"Aledo":[32.6974,-97.6025],
  "Cleburne":[32.3501,-97.3886],"Granbury":[32.4418,-97.7947],"Stephenville":[32.2207,-98.2023],
  "Crowley":[32.5793,-97.3622],"Kennedale":[32.6457,-97.2264],"Duncanville":[32.6526,-96.9083],
  "DeSoto":[32.5896,-96.8572],"Cedar Hill":[32.5885,-96.9561],"Lancaster":[32.5924,-96.7561],
  "Ennis":[32.3290,-96.6252],"Italy":[32.1818,-96.8861],"Waco":[31.5493,-97.1467],
  "Temple":[31.0982,-97.3428],"Killeen":[31.1171,-97.7278],"Georgetown":[30.6333,-97.6789],
  "Round Rock":[30.5083,-97.6789],"Cedar Park":[30.5052,-97.8203],"Leander":[30.5788,-97.8531],
  "Pflugerville":[30.4388,-97.6200],"Hutto":[30.5427,-97.5461],"Kyle":[29.9891,-97.8772],
  "Buda":[30.0852,-97.8408],"San Marcos":[29.8833,-97.9414],"New Braunfels":[29.7030,-98.1245],
  "Seguin":[29.5688,-97.9642],"Conroe":[30.3119,-95.4561],"Magnolia":[30.2099,-95.7527],
  "Montgomery":[30.3877,-95.6966],"Tomball":[30.0974,-95.6155],"Spring":[30.0799,-95.4172],
  "Katy":[29.7858,-95.8244],"Sugar Land":[29.6197,-95.6349],"Missouri City":[29.6185,-95.5377],
  "Pearland":[29.5636,-95.2860],"League City":[29.5075,-95.0949],"Friendswood":[29.5294,-95.2010],
  "Manvel":[29.4613,-95.3577],"Alvin":[29.4238,-95.2438],"Angleton":[29.1694,-95.4316],
  "Lake Jackson":[29.0344,-95.4344],"Clute":[29.0194,-95.3977],
  "Anna":[33.3515,-96.5492],"Blue Ridge":[33.2868,-96.4046],"Boyd":[33.0682,-97.5536],
  "Brownsboro":[32.2968,-95.6125],"Cleveland":[30.3413,-95.0894],"Decatur":[33.2348,-97.5836],
  "Durant":[33.9941,-96.3708],"Farmersville":[33.1640,-96.3583],"Houston":[29.7604,-95.3698],
  "Hutchins":[32.6415,-96.7094],"Mabank":[32.3671,-96.1030],"Mesquite":[32.7668,-96.5992],
  "Princeton":[33.1801,-96.4990],"Red Oak":[32.5182,-96.7994],"Tyler":[32.3513,-95.3011]
};

// ── Palette ──────────────────────────────────────────────────────────────────
const PAL = ['#1b3a6b','#2563eb','#7c3aed','#0891b2','#059669',
             '#d97706','#dc2626','#db2777','#65a30d','#ea580c',
             '#6366f1','#14b8a6','#f59e0b','#8b5cf6','#10b981'];

// ── Helpers ──────────────────────────────────────────────────────────────────
const fmt  = n => n.toLocaleString();
const pct  = (a,b) => b > 0 ? (a/b*100).toFixed(1)+'%' : '–';
const fmtD = d => d.toLocaleDateString('en-US',{month:'short',day:'numeric',year:'numeric'});
const ym   = d => `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`;

function sumByKey(arr, keyField, valField='Count') {
  const m = {};
  for (const r of arr) m[r[keyField]] = (m[r[keyField]] || 0) + r[valField];
  return m;
}

// ── Chart helper ─────────────────────────────────────────────────────────────
let charts = {};
function buildBar(id, labels, values, colors, existing) {
  if (existing) existing.destroy();
  const ctx = document.getElementById(id);
  if (!ctx) return null;
  return new Chart(ctx, {
    type: 'bar',
    data: { labels, datasets:[{ data:values, backgroundColor:colors.slice(0,labels.length), borderRadius:3 }] },
    options: {
      indexAxis:'y', responsive:true, maintainAspectRatio:false,
      plugins:{ legend:{display:false}, tooltip:{ callbacks:{ label: c=>` ${fmt(c.parsed.x)}` } } },
      scales:{
        x:{ grid:{color:'#f0f4fa'}, ticks:{font:{size:10}} },
        y:{ grid:{display:false}, ticks:{font:{size:10}} }
      }
    }
  });
}

// ── Map ──────────────────────────────────────────────────────────────────────
let leafMap = null, mapMarkers = [];
function initMap() {
  leafMap = L.map('map',{zoomControl:true}).setView([32.4,-97.0],7);
  L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',{
    attribution:'&copy; CartoDB', maxZoom:18
  }).addTo(leafMap);
}
function geoFuzzy(name) {
  const lc = name.toLowerCase();
  for (const [k,v] of Object.entries(GEO))
    if (k.toLowerCase().includes(lc)||lc.includes(k.toLowerCase())) return v;
  return null;
}
function updateMap(aoiMap) {
  mapMarkers.forEach(m=>leafMap.removeLayer(m));
  mapMarkers = [];
  const entries = Object.entries(aoiMap).filter(([,v])=>v>0);
  if (!entries.length) return;
  const maxV = Math.max(...entries.map(([,v])=>v));
  for (const [name,cnt] of entries) {
    const coords = GEO[name] || geoFuzzy(name);
    if (!coords) continue;
    const r = 8 + Math.round((cnt/maxV)*28);
    L.circleMarker(coords,{radius:r,color:'#1b3a6b',fillColor:'#2563eb',fillOpacity:.55,weight:1.5})
     .bindPopup(`<b>${name}</b><br>${fmt(cnt)} leads`)
     .addTo(leafMap);
    mapMarkers.push(L.circleMarker(coords));
  }
}

// ── Date range ───────────────────────────────────────────────────────────────
function getRange(r) {
  let t = new Date(); t.setHours(23,59,59,999);
  let s = new Date();
  if (r==='last-month') {
    s = new Date(t.getFullYear(),t.getMonth()-1,1);
    t = new Date(t.getFullYear(),t.getMonth(),0,23,59,59,999);
  } else if (r==='cur-month') {
    s = new Date(t.getFullYear(),t.getMonth(),1);
  } else if (r==='last-week') {
    const dow = t.getDay();
    const dsm = dow===0?6:dow-1;
    const thisMon = new Date(t); thisMon.setDate(t.getDate()-dsm); thisMon.setHours(0,0,0,0);
    const lastSun = new Date(thisMon); lastSun.setDate(thisMon.getDate()-1); lastSun.setHours(23,59,59,999);
    const lastMon = new Date(lastSun); lastMon.setDate(lastSun.getDate()-6); lastMon.setHours(0,0,0,0);
    const ms = new Set([ym(lastMon)]); if (ym(lastMon)!==ym(lastSun)) ms.add(ym(lastSun));
    return {start:lastMon,end:lastSun,months:ms};
  } else if (r==='last-10') { s=new Date(t); s.setDate(t.getDate()-9);
  } else if (r==='last-20') { s=new Date(t); s.setDate(t.getDate()-19);
  } else if (r==='last-30') { s=new Date(t); s.setDate(t.getDate()-29);
  } else if (r==='last-60') { s=new Date(t); s.setDate(t.getDate()-59);
  } else if (r==='last-90') { s=new Date(t); s.setDate(t.getDate()-89);
  } else if (r==='ytd')     { s=new Date(t.getFullYear(),0,1);
  } else if (r==='last-year') {
    s=new Date(t.getFullYear()-1,0,1);
    t=new Date(t.getFullYear()-1,11,31,23,59,59,999);
  }
  s.setHours(0,0,0,0);
  const ms=new Set(), cur=new Date(s.getFullYear(),s.getMonth(),1), last=new Date(t.getFullYear(),t.getMonth(),1);
  while (cur<=last){ ms.add(ym(cur)); cur.setMonth(cur.getMonth()+1); }
  return {start:s,end:t,months:ms};
}

// ── Funnel table builder ──────────────────────────────────────────────────────
function buildTable(tbodyId, tfootId, rows, totLeads, totAppts, totOpps, totCanc) {
  // rows: [{name, leads, appts, opps, canc}] sorted by leads desc
  const tbody = document.getElementById(tbodyId);
  const tfoot = document.getElementById(tfootId);
  const hasAppts = totAppts > 0;
  tbody.innerHTML = '';

  const maxLeads = rows.length ? Math.max(...rows.map(r=>r.leads)) : 1;

  for (const r of rows) {
    const net = r.opps - r.canc;
    const netCls = net >= 0 ? 'net-pos' : 'net-neg';
    const barW = maxLeads > 0 ? Math.round(r.leads/maxLeads*100) : 0;
    tbody.insertAdjacentHTML('beforeend', `<tr>
      <td>${r.name}</td>
      <td class="bar-cell"><div class="bar-bg" style="width:${barW}%"></div><span class="bar-val">${fmt(r.leads)}</span></td>
      <td>${hasAppts ? fmt(r.appts) : '<span style="color:#ccc">–</span>'}</td>
      <td>${hasAppts ? '<span class="pct">'+pct(r.appts,r.leads)+'</span>' : '<span style="color:#ccc">–</span>'}</td>
      <td>${fmt(r.opps)}</td>
      <td><span class="pct">${pct(r.opps,r.leads)}</span></td>
      <td>${fmt(r.canc)}</td>
      <td class="${netCls}">${net>=0?'+':''}${fmt(net)}</td>
    </tr>`);
  }

  const totNet = totOpps - totCanc;
  tfoot.innerHTML = `
    <td>Total</td>
    <td>${fmt(totLeads)}</td>
    <td>${hasAppts ? fmt(totAppts) : '–'}</td>
    <td>${hasAppts ? '<span class="pct">'+pct(totAppts,totLeads)+'</span>' : '–'}</td>
    <td>${fmt(totOpps)}</td>
    <td><span class="pct">${pct(totOpps,totLeads)}</span></td>
    <td>${fmt(totCanc)}</td>
    <td class="${totNet>=0?'net-pos':'net-neg'}">${totNet>=0?'+':''}${fmt(totNet)}</td>`;
}

// ── Main update ───────────────────────────────────────────────────────────────
function update(r) {
  const {start,end,months} = getRange(r);

  // ── KPI: exact counts from daily data ──────────────────────────────────
  const inRange = d => { const dt=new Date(d+'T00:00:00'); return dt>=start&&dt<=end; };

  const totalLeads = LEADS_DAILY.filter(d=>inRange(d.Date)).reduce((s,d)=>s+d.Count,0);
  const totalAppts = APPTS_DAILY.filter(d=>inRange(d.Date)).reduce((s,d)=>s+d.Count,0);
  const hasAppts   = APPTS_DAILY.length > 0;

  const fOpps = OPPS.filter(d=>inRange(d.Date));
  const totalOpps = fOpps.length;
  const fCanc     = fOpps.filter(d=>d.Stage==='Closed Lost'||d.Stage==='Cancelled');
  const totalCanc = fCanc.length;
  const totalNet  = totalOpps - totalCanc;

  // ── Monthly breakdowns (community & source) ────────────────────────────
  const leadsAoiMap  = {};
  for (const d of LEADS_AOI.filter(d=>months.has(d.Month)))
    leadsAoiMap[d.Aoi] = (leadsAoiMap[d.Aoi]||0)+d.Count;

  const apptsAoiMap = {};
  for (const d of APPTS_AOI.filter(d=>months.has(d.Month)))
    apptsAoiMap[d.Aoi] = (apptsAoiMap[d.Aoi]||0)+d.Count;

  const leadsSrcMap = {};
  for (const d of LEADS_SRC.filter(d=>months.has(d.Month)))
    leadsSrcMap[d.Src] = (leadsSrcMap[d.Src]||0)+d.Count;

  const apptsSrcMap = {};
  for (const d of APPTS_SRC.filter(d=>months.has(d.Month)))
    apptsSrcMap[d.Src] = (apptsSrcMap[d.Src]||0)+d.Count;

  // Opps & Cancellations by community and source (exact date range)
  const oppsCommMap={}, cancCommMap={}, oppsSrcMap={}, cancSrcMap={};
  for (const d of fOpps) {
    oppsCommMap[d.Comm]=(oppsCommMap[d.Comm]||0)+1;
    oppsSrcMap[d.Src]=(oppsSrcMap[d.Src]||0)+1;
  }
  for (const d of fCanc) {
    cancCommMap[d.Comm]=(cancCommMap[d.Comm]||0)+1;
    cancSrcMap[d.Src]=(cancSrcMap[d.Src]||0)+1;
  }

  // ── Build community rows ──────────────────────────────────────────────
  const allComm = new Set([...Object.keys(leadsAoiMap),...Object.keys(oppsCommMap)]);
  allComm.delete('Unknown');
  const commRows = [...allComm].map(name=>({
    name,
    leads: leadsAoiMap[name]||0,
    appts: apptsAoiMap[name]||0,
    opps:  oppsCommMap[name]||0,
    canc:  cancCommMap[name]||0
  })).filter(r=>r.leads>0||r.opps>0).sort((a,b)=>b.leads-a.leads);

  // ── Build source rows ────────────────────────────────────────────────
  const allSrc = new Set([...Object.keys(leadsSrcMap),...Object.keys(oppsSrcMap)]);
  allSrc.delete('Unknown');
  const srcRows = [...allSrc].map(name=>({
    name,
    leads: leadsSrcMap[name]||0,
    appts: apptsSrcMap[name]||0,
    opps:  oppsSrcMap[name]||0,
    canc:  cancSrcMap[name]||0
  })).filter(r=>r.leads>0||r.opps>0).sort((a,b)=>b.leads-a.leads);

  // ── KPI cards ────────────────────────────────────────────────────────
  const rl = `${fmtD(start)} – ${fmtD(end)}`;
  document.getElementById('kLeads').textContent = fmt(totalLeads);
  document.getElementById('kLeadsSub').textContent = rl;
  document.getElementById('kAppts').textContent = hasAppts ? fmt(totalAppts) : '–';
  document.getElementById('kApptsSub').textContent = hasAppts ? rl : 'Not configured';
  document.getElementById('apptsNote').style.display = hasAppts ? 'none' : 'inline';
  document.getElementById('kOpps').textContent = fmt(totalOpps);
  document.getElementById('kOppsSub').textContent = rl;
  document.getElementById('kCanc').textContent = fmt(totalCanc);
  document.getElementById('kCancSub').textContent = rl;
  const netEl = document.getElementById('kNet');
  netEl.textContent = (totalNet>=0?'+':'')+fmt(totalNet);
  netEl.style.color = totalNet>=0?'#16a34a':'#dc2626';
  document.getElementById('kNetSub').textContent = rl;

  const btnLabel = document.querySelector(`.dbtn[data-r="${r}"]`)?.textContent||r;
  document.getElementById('showing').innerHTML =
    `Showing: <strong>${btnLabel}</strong> &nbsp;|&nbsp; ${fmtD(start)} – ${fmtD(end)}`;

  // ── Tables ───────────────────────────────────────────────────────────
  buildTable('tbody-community','tfoot-community',commRows,totalLeads,totalAppts,totalOpps,totalCanc);
  buildTable('tbody-source',   'tfoot-source',   srcRows, totalLeads,totalAppts,totalOpps,totalCanc);

  // ── Charts ──────────────────────────────────────────────────────────
  const srcTop = Object.entries(leadsSrcMap).sort((a,b)=>b[1]-a[1]).slice(0,12);
  charts.source = buildBar('ch-source', srcTop.map(x=>x[0]), srcTop.map(x=>x[1]), PAL, charts.source);

  const ownerMap={};
  for (const d of fOpps) ownerMap[d.Owner]=(ownerMap[d.Owner]||0)+1;
  const owTop = Object.entries(ownerMap).sort((a,b)=>b[1]-a[1]).slice(0,12);
  charts.owner = buildBar('ch-owner', owTop.map(x=>x[0]), owTop.map(x=>x[1]), PAL.slice(3), charts.owner);

  const cancTop = Object.entries(cancSrcMap).sort((a,b)=>b[1]-a[1]).slice(0,12);
  charts.canc = buildBar('ch-canc', cancTop.map(x=>x[0]), cancTop.map(x=>x[1]), ['#dc2626'], charts.canc);

  // ── Map ──────────────────────────────────────────────────────────────
  updateMap(leadsAoiMap);
}

// ── Init ─────────────────────────────────────────────────────────────────────
initMap();
document.querySelectorAll('.dbtn').forEach(btn=>{
  btn.addEventListener('click',()=>{
    document.querySelectorAll('.dbtn').forEach(b=>b.classList.remove('active'));
    btn.classList.add('active');
    update(btn.dataset.r);
  });
});
document.querySelector('[data-r="cur-month"]').click();
</script>
</body>
</html>
""";
}
