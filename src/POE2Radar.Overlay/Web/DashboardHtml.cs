namespace POE2Radar.Overlay.Web;

/// <summary>
/// Self-contained web dashboard served at <c>GET /</c> by <see cref="ApiServer"/>. One inlined
/// HTML/CSS/JS document — no external assets beyond Google Fonts. The Console tab reads/writes
/// radar/visual settings via <c>/api/settings</c> (the only writes it makes — flags + calibration,
/// never flask/automation); the Filters tab manages watched/hidden lists via <c>/api/watched</c> /
/// <c>/api/hidden</c>; the Dashboard tab polls the same-origin read endpoints (<c>/state</c>,
/// <c>/entities</c>, <c>/landmarks</c>, <c>/api/nav</c>).
/// </summary>
internal static class DashboardHtml
{
    public const string Page = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>POE2Radar — Console</title>
<!-- Self-contained: no external fonts/CDNs. Falls back to local system serif/mono fonts. -->
<style>
  :root{
    --bg:#0a0907; --bg2:#100d09; --panel:#15110b; --panel2:#1b1610;
    --line:#3a2f1d; --line-soft:#271f14;
    --ink:#e8dcc2; --ink-dim:#9c8e72; --ink-faint:#6b5f49;
    --gold:#c8a049; --gold-bright:#ecca7e; --gold-deep:#8a6d34;
    --blood:#9c342a; --blood-bright:#d6584a;
    --rare:#f1e36b; --magic:#7f93ff; --unique:#d2641e; --normal:#cdc6b4;
    --good:#79b06a; --poi:#4bb3c4;
    --shadow:0 18px 40px -20px rgba(0,0,0,.9);
  }
  *{box-sizing:border-box}
  html,body{height:100%}
  body{
    margin:0; background:
      radial-gradient(120% 90% at 50% -10%, #1a150d 0%, var(--bg) 55%) fixed,
      var(--bg);
    color:var(--ink);
    font-family:"IBM Plex Mono","Consolas",ui-monospace,monospace;
    font-size:13px; line-height:1.5;
    -webkit-font-smoothing:antialiased;
    overflow:hidden;
  }
  /* grain + vignette atmosphere */
  body::before{
    content:""; position:fixed; inset:0; pointer-events:none; z-index:999;
    background:radial-gradient(120% 120% at 50% 40%, transparent 58%, rgba(0,0,0,.55) 100%);
    mix-blend-mode:multiply;
  }
  body::after{
    content:""; position:fixed; inset:0; pointer-events:none; z-index:998; opacity:.045;
    background-image:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.9' numOctaves='2'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
  }

  .shell{display:grid; grid-template-rows:auto 1fr; height:100vh}

  /* ── masthead ── */
  header{
    display:flex; align-items:center; gap:20px; padding:14px 26px;
    border-bottom:1px solid var(--line);
    background:linear-gradient(180deg, rgba(30,24,14,.6), transparent);
  }
  .mark{display:flex; align-items:baseline; gap:12px}
  .mark h1{
    font-family:"Cinzel","Georgia",serif; font-weight:700; font-size:22px; margin:0;
    letter-spacing:.14em; color:var(--gold-bright);
    text-shadow:0 1px 0 #000, 0 0 22px rgba(200,160,73,.25);
  }
  .mark .sub{font-size:10px; letter-spacing:.42em; color:var(--ink-faint); text-transform:uppercase}
  .hgap{flex:1}
  .conn{display:flex; align-items:center; gap:9px; font-size:11px; letter-spacing:.1em; color:var(--ink-dim); text-transform:uppercase}
  .dot{width:9px; height:9px; border-radius:50%; background:var(--blood); box-shadow:0 0 0 0 rgba(214,88,74,.5); }
  .conn.live .dot{background:var(--good); animation:pulse 2.2s infinite}
  @keyframes pulse{0%{box-shadow:0 0 0 0 rgba(121,176,106,.5)}70%{box-shadow:0 0 0 7px rgba(121,176,106,0)}100%{box-shadow:0 0 0 0 rgba(121,176,106,0)}}
  .area-chip{
    font-family:"Cinzel","Georgia",serif; letter-spacing:.08em; color:var(--ink);
    border:1px solid var(--line); padding:5px 14px; border-radius:2px;
    background:var(--panel); font-size:13px;
  }
  .area-chip b{color:var(--gold-bright); font-weight:600}

  /* ── body grid ── */
  .body{display:grid; grid-template-columns:300px 1fr; gap:0; min-height:0}
  aside{
    border-right:1px solid var(--line); padding:22px 22px 0;
    overflow-y:auto; background:linear-gradient(180deg, rgba(20,16,10,.5), transparent 220px);
  }
  main{display:grid; grid-template-rows:auto 1fr; min-height:0; min-width:0}

  /* ── vitals ── */
  .vital{margin-bottom:18px}
  .vital .vlabel{display:flex; justify-content:space-between; font-size:10px; letter-spacing:.18em; text-transform:uppercase; color:var(--ink-dim); margin-bottom:6px}
  .vital .vlabel .num{color:var(--ink); font-weight:600}
  .bar{height:9px; border:1px solid var(--line); background:#0c0a07; border-radius:1px; overflow:hidden; position:relative}
  .bar > i{display:block; height:100%; transition:width .35s ease}
  .bar.hp > i{background:linear-gradient(90deg,#6e1f18,var(--blood-bright))}
  .bar.es > i{background:linear-gradient(90deg,#1f6e63,#33e0c4)}
  .bar.mana > i{background:linear-gradient(90deg,#23306e,var(--magic))}

  .sect{font-family:"Cinzel","Georgia",serif; font-size:12px; letter-spacing:.22em; text-transform:uppercase; color:var(--gold); margin:24px 0 12px; display:flex; align-items:center; gap:10px}
  .sect::after{content:""; flex:1; height:1px; background:linear-gradient(90deg,var(--line),transparent)}

  .kv{display:flex; justify-content:space-between; padding:5px 0; border-bottom:1px dotted var(--line-soft); font-size:12px}
  .kv span:first-child{color:var(--ink-faint); letter-spacing:.04em}
  .kv span:last-child{color:var(--ink); font-weight:500}

  .tally{display:grid; grid-template-columns:1fr 1fr; gap:7px; margin-top:4px}
  .tally .t{border:1px solid var(--line-soft); background:var(--panel); padding:9px 10px; border-radius:2px}
  .tally .t .n{font-size:20px; font-weight:600; color:var(--gold-bright); font-family:"Cinzel","Georgia",serif; line-height:1}
  .tally .t .l{font-size:9px; letter-spacing:.16em; text-transform:uppercase; color:var(--ink-faint); margin-top:4px}

  /* ── zone leveling notes ── */
  .znotes{margin-top:12px; padding:11px 13px; border:1px solid var(--line-soft); border-left:2px solid var(--gold-deep); border-radius:2px; background:var(--panel); white-space:pre-wrap; font-size:11px; line-height:1.5; color:var(--ink-dim); max-height:240px; overflow:auto}
  .znotes .zt{font-family:"Cinzel","Georgia",serif; font-size:11px; letter-spacing:.1em; color:var(--gold-bright); margin-bottom:6px; white-space:normal}

  /* ── tabs ── */
  .tabs{display:flex; gap:2px; padding:14px 26px 0; border-bottom:1px solid var(--line)}
  .tab{
    font-family:"Cinzel","Georgia",serif; font-size:12px; letter-spacing:.16em; text-transform:uppercase;
    color:var(--ink-faint); background:transparent; border:1px solid transparent; border-bottom:none;
    padding:9px 20px; cursor:pointer; border-radius:3px 3px 0 0; position:relative; top:1px;
  }
  .tab:hover{color:var(--ink-dim)}
  .tab.on{color:var(--gold-bright); background:var(--panel); border-color:var(--line); }
  .tab.on::after{content:""; position:absolute; left:0; right:0; bottom:-1px; height:2px; background:var(--panel)}

  .view{overflow:auto; padding:22px 26px; min-height:0}
  .view[hidden]{display:none}
  /* ── atlas tab ── */
  .arow{display:grid; grid-template-columns:minmax(200px,2fr) minmax(120px,1.4fr) 120px; gap:10px; align-items:center;
        padding:5px 10px; border-bottom:1px solid var(--line); font-size:13px}
  .arow.ahead{font-weight:600; color:var(--ink-dim); border-bottom:1px solid var(--line); position:sticky; top:0; background:var(--panel)}
  .arow.val{background:rgba(255,168,38,.07)}
  .arow .acode{font-family:ui-monospace,Consolas,monospace; color:var(--ink)}
  .arow.val .acode{color:var(--gold-bright)}
  .arow .aname{color:var(--ink-dim)}
  .arow .aid{display:inline-block; min-width:22px; color:var(--ink-dim); font-family:ui-monospace,Consolas,monospace}
  .rin{color:#6ee787; font-weight:600} .rno{color:var(--ink-dim); opacity:.5}
  .arow.nrow{grid-template-columns:60px minmax(90px,1fr) minmax(200px,2fr) 130px; cursor:pointer}
  .arow.nrow:hover{background:rgba(255,255,255,.04)}
  .arow.nrow.sel{background:rgba(60,220,255,.16); outline:1px solid var(--edge,#3cdcff)}
  .amono{font-family:ui-monospace,Consolas,monospace; color:var(--ink-dim); font-size:12px}
  .ntag{font-size:10px; font-weight:600; padding:0 6px; border-radius:8px; border:1px solid var(--line); margin-right:3px}
  .ntag.tc{color:#ff9f43;border-color:#a35a00} .ntag.tv{color:var(--ink-dim)} .ntag.tu{color:#6ee787;border-color:#2f6b3f}
  .ntag.tk{color:#73a6ff;border-color:#2a4a80} .ntag.ts{color:#c98bff;border-color:#5a3a80}
  .akind{font-size:11px; font-weight:600; padding:1px 8px; border-radius:10px; border:1px solid var(--line); color:var(--ink-dim)}
  .akind.k-boss{color:#ff7300; border-color:#ff7300} .akind.k-unique{color:#ff9f43; border-color:#a35a00}
  .akind.k-tower{color:#73a6ff; border-color:#2a4a80} .akind.k-merchant{color:#c98bff; border-color:#5a3a80}

  /* ── controls ── */
  .controls{display:flex; flex-wrap:wrap; gap:8px; align-items:center; margin-bottom:16px}
  .chip{
    font-size:11px; letter-spacing:.06em; color:var(--ink-dim);
    border:1px solid var(--line-soft); background:var(--panel); padding:6px 12px; border-radius:14px; cursor:pointer;
    transition:all .15s;
  }
  .chip:hover{border-color:var(--gold-deep); color:var(--ink)}
  .chip.on{background:var(--gold-deep); border-color:var(--gold); color:#1a140a; font-weight:600}
  .chips{display:flex; flex-wrap:wrap; gap:6px; margin:4px 0 12px}
  input[type=search]{
    font-family:inherit; font-size:12px; color:var(--ink); background:#0c0a07;
    border:1px solid var(--line); border-radius:2px; padding:7px 12px; min-width:200px; flex:1;
  }
  input[type=search]:focus{outline:none; border-color:var(--gold-deep)}
  input[type=search]::placeholder{color:var(--ink-faint)}

  /* ── tables ── */
  table{width:100%; border-collapse:collapse; font-size:12px}
  thead th{
    text-align:left; font-weight:500; font-size:10px; letter-spacing:.14em; text-transform:uppercase;
    color:var(--ink-faint); padding:8px 10px; border-bottom:1px solid var(--line); position:sticky; top:-22px;
    background:var(--bg);
  }
  tbody td{padding:7px 10px; border-bottom:1px solid var(--line-soft); white-space:nowrap}
  tbody tr:hover{background:rgba(200,160,73,.05)}
  .meta{color:var(--ink-faint); font-size:11px; max-width:380px; overflow:hidden; text-overflow:ellipsis}
  .rar-Normal{color:var(--normal)} .rar-Magic{color:var(--magic)} .rar-Rare{color:var(--rare)} .rar-Unique{color:var(--unique)}
  .pill{font-size:9px; letter-spacing:.1em; text-transform:uppercase; padding:2px 7px; border-radius:10px; border:1px solid currentColor}
  .friendly{color:var(--good)} .hostile{color:var(--blood-bright)}
  .num-r{text-align:right; color:var(--ink-dim)}
  .hpbar{width:60px; height:6px; border:1px solid var(--line); border-radius:1px; overflow:hidden; display:inline-block; vertical-align:middle}
  .hpbar > i{display:block; height:100%; background:linear-gradient(90deg,#6e1f18,var(--blood-bright))}

  .lm{display:flex; align-items:center; gap:14px; padding:11px 14px; border:1px solid var(--line-soft); border-radius:3px; margin-bottom:8px; background:var(--panel)}
  .lm:hover{border-color:var(--gold-deep)}
  .lm .name{font-family:"Spectral","Georgia",serif; font-size:15px; color:var(--gold-bright); font-style:italic}
  .lm .path{font-size:10px; color:var(--ink-faint); overflow:hidden; text-overflow:ellipsis; white-space:nowrap}
  .lm .dist{margin-left:auto; font-family:"Cinzel","Georgia",serif; color:var(--ink); font-size:14px; flex:none}
  .lm .dist small{color:var(--ink-faint); font-size:9px; letter-spacing:.1em; display:block; text-align:right}

  .empty{color:var(--ink-faint); text-align:center; padding:60px 0; font-style:italic; font-family:"Spectral","Georgia",serif; font-size:15px}
  ::-webkit-scrollbar{width:10px;height:10px}
  ::-webkit-scrollbar-thumb{background:var(--line); border-radius:5px; border:2px solid var(--bg)}
  ::-webkit-scrollbar-track{background:transparent}

  /* ── console / control panel ── */
  .panel-grid{display:grid; grid-template-columns:repeat(auto-fill,minmax(330px,1fr)); gap:22px; align-items:start}
  .card{border:1px solid var(--line); border-radius:4px; background:var(--panel); padding:18px 22px; box-shadow:var(--shadow)}
  .card h3{font-family:"Cinzel","Georgia",serif; font-size:12px; letter-spacing:.2em; text-transform:uppercase; color:var(--gold); margin:0 0 8px}
  .card h3 .tag{color:var(--ink-faint); font-size:10px; letter-spacing:.1em}
  .row{display:flex; align-items:center; justify-content:space-between; gap:16px; padding:11px 0; border-bottom:1px dotted var(--line-soft)}
  .row:last-child{border-bottom:none}
  .row .rl{font-size:12px; color:var(--ink); min-width:0}
  .row .rl small{display:block; color:var(--ink-faint); font-size:10px; letter-spacing:.03em; margin-top:3px; line-height:1.4}
  .sw{position:relative; width:44px; height:23px; flex:none; cursor:pointer; display:inline-block}
  .sw input{opacity:0; width:0; height:0; position:absolute}
  .sw .track{position:absolute; inset:0; background:#0c0a07; border:1px solid var(--line); border-radius:12px; transition:.2s}
  .sw .knob{position:absolute; top:3px; left:3px; width:15px; height:15px; border-radius:50%; background:var(--ink-faint); transition:.2s}
  .sw input:checked ~ .track{background:var(--gold-deep); border-color:var(--gold)}
  .sw input:checked ~ .knob{transform:translateX(21px); background:var(--gold-bright); box-shadow:0 0 9px -1px var(--gold-bright)}
  .numin{font-family:inherit; font-size:12px; color:var(--ink); background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:6px 9px; width:96px; text-align:right}
  .numin:focus{outline:none; border-color:var(--gold-deep)}
  .ro{color:var(--gold-bright); font-family:"Cinzel","Georgia",serif; font-size:14px}
  .hint-row{color:var(--ink-faint)!important; font-size:11px!important; font-style:italic}
  .saved{font-size:10px; letter-spacing:.18em; text-transform:uppercase; color:var(--good); opacity:0; transition:opacity .3s}
  .saved.show{opacity:1}

  /* ── icon / mechanic style editors ── */
  .stylerow{display:flex; align-items:center; gap:9px; padding:9px 0; border-bottom:1px dotted var(--line-soft); flex-wrap:wrap}
  .stylerow:last-child{border-bottom:none}
  .stylerow .nm{flex:1 1 110px; min-width:90px; font-size:12px; color:var(--ink)}
  .stylerow .sw{width:38px; height:20px}
  .stylerow .sw .knob{width:13px; height:13px}
  .stylerow .sw input:checked ~ .knob{transform:translateX(18px)}
  input[type=color]{width:30px; height:24px; padding:0; border:1px solid var(--line); background:#0c0a07; border-radius:2px; cursor:pointer; flex:none}
  input[type=range].op{width:78px; accent-color:var(--gold); flex:none}
  .opv{font-size:10px; color:var(--ink-faint); width:30px; text-align:right}
  .numin.sz{width:56px}
  .mechrow{border:1px solid var(--line-soft); border-radius:3px; background:var(--panel2); padding:10px 12px; margin-bottom:8px}
  .mechrow .top{display:flex; align-items:center; gap:9px; margin-bottom:8px}
  .mechrow .top input.mname{flex:1; font-family:inherit; font-size:12px; color:var(--ink); background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:5px 9px}
  .mechrow .matchin{width:100%; font-family:inherit; font-size:11px; color:var(--ink-dim); background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:5px 9px; margin-bottom:8px}
  .mechrow .ctl{display:flex; align-items:center; gap:9px; flex-wrap:wrap}
  .mcats{display:flex; align-items:center; gap:6px; flex-wrap:wrap; margin-bottom:8px}
  .mcats-lbl{font-size:10px; letter-spacing:.06em; text-transform:uppercase; color:var(--ink-faint); margin-right:2px}
  .mcats-hint{font-size:10px; font-style:italic; color:var(--ink-faint)}
  .catchip{display:inline-flex; align-items:center; font-size:11px; color:var(--ink-dim); background:#0c0a07; border:1px solid var(--line); border-radius:10px; padding:2px 9px; cursor:pointer; user-select:none}
  .catchip:hover{border-color:var(--gold-deep)}
  .catchip.on{color:var(--bg); background:var(--gold); border-color:var(--gold-bright); font-weight:600}
  .catchip input{display:none}
  /* Display-rule rows: collapsed one-line header, expand to the full editor. */
  .drrow{padding:8px 12px}
  .drhead{display:flex; align-items:center; gap:9px; cursor:pointer}
  .drhead .sw{flex:none}
  .drcaret{color:var(--ink-faint); width:10px; font-size:10px; flex:none}
  .drswatch{width:15px; height:15px; flex:none; display:inline-flex}
  .drswatch svg{width:15px; height:15px; display:block}
  .drnm{font-weight:600; color:var(--ink); white-space:nowrap; flex:none; max-width:200px; overflow:hidden; text-overflow:ellipsis}
  .drsum{flex:1 1 auto; min-width:0; color:var(--ink-faint); font-size:11px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis}
  .drbadges{display:inline-flex; gap:4px; flex:none}
  .drbadge{font-size:9px; text-transform:uppercase; letter-spacing:.05em; color:var(--ink-dim); border:1px solid var(--line); border-radius:8px; padding:1px 6px; white-space:nowrap}
  .drbadge.hide{color:var(--blood-bright); border-color:var(--blood)}
  .drrow.off .drnm,.drrow.off .drsum,.drrow.off .drswatch{opacity:.45}
  .drbody{margin-top:10px; padding-top:10px; border-top:1px dotted var(--line-soft)}
  .drbody .top{align-items:center; margin-bottom:8px}
  .drord{display:inline-flex; gap:2px; flex:none}
  .drhead .delbtn{flex:none}
  .ordbtn{font-size:10px; line-height:1; color:var(--ink-dim); background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:3px 6px; cursor:pointer}
  .ordbtn:hover{color:var(--gold-bright); border-color:var(--gold-deep)}
  .drconds{display:flex; align-items:center; gap:10px; flex-wrap:wrap; margin-bottom:8px}
  .drsel{display:inline-flex; align-items:center; gap:5px; font-size:10px; letter-spacing:.05em; text-transform:uppercase; color:var(--ink-faint)}
  .drsel select{font-family:inherit; font-size:11px; text-transform:none; letter-spacing:0; color:var(--ink); background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:3px 6px}
  .drsel select:hover{border-color:var(--gold-deep)}
  .drflag{display:inline-flex; align-items:center; gap:5px; font-size:11px; color:var(--ink-dim); cursor:pointer; user-select:none; white-space:nowrap}
  .dr-hideflag{color:var(--blood-bright)}
  .drrow.hideon{opacity:.72}
  .drrow.hideon .iconpick,.drrow.hideon .dr-color,.drrow.hideon .dr-op,.drrow.hideon .dr-size,.drrow.hideon .dr-label,.drrow.hideon .opv{opacity:.4; pointer-events:none}
  /* consolidated HP-bar card: per-rarity grid + shared geometry footer */
  .hpgrid{display:grid; grid-template-columns:30px 64px 1fr 30px 1fr; gap:9px 11px; align-items:center; padding:4px 0 2px}
  .hpgrid input[type=checkbox]{margin:0; justify-self:center}
  .hpgrid .hph{font-size:10px; letter-spacing:.06em; text-transform:uppercase; color:var(--ink-faint); text-align:right}
  .hpgrid .hph:first-child{text-align:left}
  .hpgrid .hpr{font-size:12px; color:var(--ink)}
  .hpgrid .numin{width:100%; min-width:0; padding:5px 8px}
  .hpgrid input[type=color]{width:100%}
  .hpshared{display:flex; gap:16px; flex-wrap:wrap; margin-top:10px; padding-top:11px; border-top:1px dotted var(--line-soft)}
  .hpshared label{display:flex; align-items:center; gap:7px; font-size:11px; color:var(--ink-dim)}
  .hpshared .numin{width:62px}
  .delbtn{font-family:inherit; font-size:11px; color:var(--blood-bright); background:transparent; border:1px solid var(--line); border-radius:2px; padding:4px 9px; cursor:pointer; flex:none}
  .trow-ctl{display:flex; align-items:center; gap:9px; flex:none}

  /* ── SVG icon picker (replaces the plain shape <select>): a button showing the chosen icon's
       silhouette + name, opening a shared popup grid of icon previews. ── */
  .iconpick{display:inline-flex; align-items:center; gap:6px; min-width:104px; background:#0c0a07; border:1px solid var(--line); border-radius:2px; padding:3px 7px; cursor:pointer; flex:none}
  .iconpick:hover{border-color:var(--gold-deep)}
  .iconpick .ipreview{width:15px; height:15px; flex:none; display:inline-flex; color:var(--ink)}
  .iconpick .ipreview svg{width:15px; height:15px; display:block}
  .iconpick .ipname{font-size:11px; color:var(--ink); white-space:nowrap; overflow:hidden; text-overflow:ellipsis}
  .iconpick .ipcar{margin-left:auto; color:var(--ink-faint); font-size:8px}
  #iconPop{position:fixed; z-index:1000; display:none; background:var(--panel2); border:1px solid var(--gold-deep); border-radius:4px; box-shadow:var(--shadow); padding:8px; max-height:300px; overflow:auto}
  #iconPop.open{display:block}
  /* Add-rule picker modal: browse live entities + terrain tiles. */
  #pickPop{position:fixed; inset:0; z-index:1100; display:none; background:rgba(0,0,0,.62); padding:6vh 4vw}
  #pickPop.open{display:flex; justify-content:center; align-items:flex-start}
  .pickbox{display:flex; flex-direction:column; width:min(760px,100%); max-height:88vh; background:var(--panel); border:1px solid var(--gold-deep); border-radius:6px; box-shadow:var(--shadow); overflow:hidden}
  .pickhead{display:flex; align-items:center; gap:10px; padding:12px 14px; border-bottom:1px solid var(--line)}
  .pickhead #pickSearch{flex:1; font-family:inherit; font-size:13px; color:var(--ink); background:#0c0a07; border:1px solid var(--line); border-radius:3px; padding:8px 11px}
  .pickkinds{display:inline-flex; gap:3px}
  .pickclose{font-size:13px; color:var(--ink-dim); background:transparent; border:1px solid var(--line); border-radius:3px; padding:6px 10px; cursor:pointer}
  .pickclose:hover{color:var(--blood-bright); border-color:var(--blood)}
  .picklist{overflow:auto; padding:4px 0}
  .pickrow{display:flex; align-items:center; gap:10px; padding:7px 14px; cursor:pointer; border-bottom:1px dotted var(--line-soft)}
  .pickrow:hover{background:var(--panel2)}
  .pickbadge{flex:none; font-size:9px; text-transform:uppercase; letter-spacing:.05em; color:var(--ink-dim); background:#0c0a07; border:1px solid var(--line); border-radius:8px; padding:2px 7px; min-width:58px; text-align:center}
  .pickbadge.tile{color:var(--poi); border-color:var(--poi)}
  .pickbadge.entity{color:var(--gold)}
  .pickbadge.mod{color:#26d9c0; border-color:#1c9e8c}
  .pickcount{flex:none; font-size:10px; color:var(--ink-dim); font-family:"Cinzel","Georgia",serif}
  .picknm{flex:none; font-weight:600; color:var(--ink); max-width:230px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap}
  .picksub{flex:1; min-width:0; color:var(--ink-faint); font-size:11px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap}
  .pickrar{flex:none; font-size:10px; color:var(--rare)}
  .pickempty{padding:24px 14px; color:var(--ink-faint); font-style:italic; text-align:center}
  .pickfoot{padding:9px 14px; border-top:1px solid var(--line); color:var(--ink-faint); font-size:11px}
  /* Landmarks tab rows */
  .lmrow{display:flex; align-items:center; gap:10px; padding:6px 0; border-bottom:1px dotted var(--line-soft)}
  .lmbadge{flex:none; min-width:48px; text-align:center; font-size:9px; text-transform:uppercase; letter-spacing:.05em; color:var(--ink-dim); border:1px solid var(--line); border-radius:8px; padding:2px 6px}
  .lmbadge.user{color:var(--gold); border-color:var(--gold-deep)}
  .lmbadge.hidden{color:var(--blood-bright); border-color:var(--blood)}
  .lmarea{flex:none; min-width:64px; font-size:11px; color:var(--ink-dim); font-family:"Consolas",monospace}
  .lmlabel{flex:none; width:200px}
  .lmpath{flex:1; min-width:0; color:var(--ink-faint); font-size:11px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; font-family:"Consolas",monospace}
  .lmrow.sup .lmlabel,.lmrow.sup .lmpath{opacity:.5}
  .ipop-grid{display:grid; grid-template-columns:repeat(6,38px); gap:4px}
  .ipop-cell{display:flex; flex-direction:column; align-items:center; justify-content:center; gap:3px; width:38px; height:40px; border:1px solid transparent; border-radius:3px; cursor:pointer; color:var(--ink)}
  .ipop-cell:hover{border-color:var(--gold); background:#0c0a07}
  .ipop-cell.sel{border-color:var(--gold-bright); background:#0c0a07}
  .ipop-cell svg{width:20px; height:20px; display:block}
  .ipop-cell .cn{font-size:7px; line-height:1; color:var(--ink-faint); max-width:36px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap}
  .delbtn:hover{border-color:var(--blood-bright)}
  .addbtn{font-family:"Cinzel","Georgia",serif; font-size:11px; letter-spacing:.1em; color:var(--gold-bright); background:transparent; border:1px dashed var(--gold-deep); border-radius:3px; padding:8px 14px; cursor:pointer; width:100%; margin-top:4px}
  .addbtn:hover{background:rgba(200,160,73,.07)}

  /* ── dashboard nav list ── */
  .navrow{display:flex; align-items:center; gap:12px; padding:9px 12px; border:1px solid var(--line-soft); border-radius:3px; margin-bottom:6px; background:var(--panel); cursor:pointer}
  .navrow:hover{border-color:var(--gold-deep)}
  .navrow.sel{border-color:var(--gold); background:rgba(200,160,73,.07)}
  .navbtn{width:18px; height:18px; flex:none; border:1px solid var(--ink-faint); border-radius:50%; display:flex; align-items:center; justify-content:center; font-size:11px; color:#120d06; line-height:1}
  .navrow:not(.sel) .navbtn{color:var(--ink-faint)}
  .navname{flex:1; min-width:0; color:var(--ink); overflow:hidden; text-overflow:ellipsis; white-space:nowrap; font-family:"Spectral","Georgia",serif; font-size:14px}
  .navrow.sel .navname{color:var(--gold-bright)}
  .navtag{font-size:9px; letter-spacing:.12em; text-transform:uppercase; color:var(--ink-faint); border:1px solid var(--line-soft); border-radius:10px; padding:2px 8px; flex:none}
  .navdist{font-family:"Cinzel","Georgia",serif; color:var(--ink-dim); font-size:13px; min-width:48px; text-align:right; flex:none}
</style>
</head>
<body>
<a id="updateBanner" href="#" target="_blank" rel="noopener" hidden
   style="display:none;align-items:center;gap:10px;padding:9px 16px;margin:0;background:#e0b341;color:#1a1400;font-weight:600;text-decoration:none">
  <span>&#x2B06; Update available</span><span id="updateMsg" style="font-weight:400"></span><span style="margin-left:auto;text-decoration:underline">Download &rarr;</span>
</a>
<div class="shell">
  <header>
    <div class="mark">
      <h1>POE2RADAR</h1>
    </div>
    <div class="hgap"></div>
    <div class="area-chip" id="areaChip">— <b>·</b></div>
    <div class="conn" id="conn"><span class="dot"></span><span id="connTxt">offline</span></div>
  </header>

  <div class="body">
    <aside>
      <div class="vital">
        <div class="vlabel"><span data-i18n="side.life">Life</span><span class="num" id="hpNum">—</span></div>
        <div class="bar hp"><i id="hpBar" style="width:0"></i></div>
      </div>
      <div class="vital">
        <div class="vlabel"><span data-i18n="side.es">Energy Shield</span><span class="num" id="esNum">—</span></div>
        <div class="bar es"><i id="esBar" style="width:0"></i></div>
      </div>
      <div class="vital">
        <div class="vlabel"><span data-i18n="side.mana">Mana</span><span class="num" id="mpNum">—</span></div>
        <div class="bar mana"><i id="mpBar" style="width:0"></i></div>
      </div>

      <div class="sect" data-i18n="side.zone">Zone</div>
      <div class="kv"><span data-i18n="side.area">Area</span><span id="kAreaName">—</span></div>
      <div class="kv"><span data-i18n="side.areaCode">Area code</span><span id="kArea">—</span></div>
      <div class="kv"><span data-i18n="side.actLevel">Act / Level</span><span id="kAlvl">—</span></div>
      <div class="kv"><span data-i18n="side.mapOpen">Map open</span><span id="kMap">—</span></div>
      <div class="kv"><span data-i18n="side.autoFlask">Auto-flask</span><span id="kFlask">—</span></div>
      <div id="zoneNotes" class="znotes" hidden></div>

      <div class="sect" data-i18n="side.census">Census</div>
      <div class="tally">
        <div class="t"><div class="n" id="cEnt">0</div><div class="l" data-i18n="side.entities">Entities</div></div>
        <div class="t"><div class="n" id="cPoi">0</div><div class="l" data-i18n="side.poi">Points of Int.</div></div>
        <div class="t"><div class="n" id="cMon">0</div><div class="l" data-i18n="side.monsters">Monsters</div></div>
        <div class="t"><div class="n" id="cLm">0</div><div class="l" data-i18n="side.landmarks">Landmarks</div></div>
      </div>

      <div id="monoCard" hidden>
        <div class="sect" data-i18n="side.monolithRewards">Monolith Rewards</div>
        <div id="monoList" class="znotes" style="display:block"></div>
      </div>

      <div style="height:24px"></div>
    </aside>

    <main>
      <div class="tabs">
        <button class="tab on" data-tab="filters" data-i18n="tab.rules">Rules</button>
        <button class="tab" data-tab="landmarks" data-i18n="tab.landmarks">Landmarks</button>
        <button class="tab" data-tab="atlas" data-i18n="tab.atlas">Atlas</button>
        <button class="tab" data-tab="value" data-i18n="tab.value">Item Value</button>
        <button class="tab" data-tab="settings" data-i18n="tab.settings">Settings</button>
      </div>

      <section class="view" data-view="filters">
        <div class="panel-grid">
          <div class="card" style="grid-column:1/-1">
            <h3><span data-i18n="sec.displayRules">Display Rules</span> <span class="tag">&middot; <span data-i18n="sec.displayRulesTag">one ordered ruleset &mdash; first match wins</span></span></h3>
            <div class="row"><div class="rl hint-row" data-i18n="rules.drHint">The single source of truth for how every entity draws. Each entity is matched <b>top&ndash;to&ndash;bottom</b>; the <b>first enabled rule that matches</b> decides everything &mdash; its icon &amp; color, whether it&rsquo;s hidden, whether it shows an HP bar, and whether it&rsquo;s auto-pathed. Reorder with &#9650;/&#9660; to change precedence. A rule matches on any mix of <i>type, metadata terms, monster mods (auras/buffs), rarity, reaction, life, chest/POI/encounter state</i>; a blank condition means &ldquo;any&rdquo;. No more conflicting filters &mdash; if two rules could match, the higher one wins.</div></div>
            <div id="drList"></div>
            <div class="controls" style="margin:8px 0 0">
              <button class="addbtn" id="drPick" style="width:auto;margin:0;padding:9px 16px" data-i18n="btn.addFromGame">+ Add from game data…</button>
              <button class="addbtn" id="drAdd" style="width:auto;margin:0;padding:9px 16px" data-i18n="btn.addBlank">+ Add blank rule</button>
            </div>
          </div>
          <div class="card" style="grid-column:1/-1">
            <h3><span data-i18n="sec.hidden">Hidden</span> <span class="tag">&middot; <span data-i18n="sec.hiddenTag">cull entirely from radar, list &amp; nav</span></span></h3>
            <div class="row"><div class="rl hint-row" data-i18n="rules.hiddenHint">A stronger cut than a Hide rule: entities whose metadata contains a pattern (or matches a <code>*</code>/<code>?</code> glob) are removed <i>everywhere</i> &mdash; overlay, entity list, and navigation &mdash; before the display rules even run.</div></div>
            <div id="hideList" class="controls" style="margin:8px 0 14px"></div>
            <div class="controls" style="margin:0">
              <input type="search" id="hidePattern" data-i18n-ph="rules.hidePh" placeholder="pattern or glob to hide (e.g. AbyssCrack, *Daemon*)">
              <button class="addbtn" id="hideAdd" style="width:auto;margin:0;padding:8px 16px" data-i18n="btn.hide">+ Hide</button>
            </div>
          </div>
        </div>
        <div style="margin-top:18px; height:14px"><span class="saved" id="savedMsgF">&#10003; <span data-i18n="saved">saved to config</span></span></div>
      </section>

      <section class="view" data-view="landmarks" hidden>
        <div class="panel-grid">
          <div class="card" style="grid-column:1/-1">
            <h3><span data-i18n="sec.landmarks">Landmarks</span> <span class="tag">&middot; <span data-i18n="sec.landmarksTag">curated map labels &mdash; view, fix, share</span></span></h3>
            <div class="row"><div class="rl hint-row" data-i18n="lm.hint">The built-in &ldquo;known&rdquo; map features (boss arenas, exits, loot, waypoints&hellip;), labelled per area. Rename a wrong label, add your own, or hide a bad entry. <b>Export</b> a corrected list to share or submit for baking into a release; <b>Import</b> to load one. (For how a tile <i>draws</i> — icon/color/hide — use a Tile rule on the Rules tab; this is just the labels.)</div></div>
            <div class="controls" style="margin:6px 0 12px">
              <input type="search" id="lmSearch" data-i18n-ph="lm.searchPh" placeholder="filter by area / tile / label…">
              <button class="chip on" id="lmAreaOnly" data-i18n="btn.thisArea">This area only</button>
              <span style="flex:1"></span>
              <button class="addbtn" id="lmImport" style="width:auto;margin:0;padding:8px 14px" data-i18n="btn.import">Import…</button>
              <button class="addbtn" id="lmExport" style="width:auto;margin:0;padding:8px 14px" data-i18n="btn.export">Export</button>
            </div>
            <div id="lmList"></div>
            <div class="mechrow">
              <div class="top">
                <input class="mname" id="lmArea" data-i18n-ph="lm.areaPh" placeholder="area (e.g. P2_3, or *)" style="max-width:150px">
                <input class="mname" id="lmPat" data-i18n-ph="lm.patPh" placeholder="tile path / pattern">
                <input class="mname" id="lmLabel" data-i18n-ph="lm.labelPh" placeholder="label">
                <button class="addbtn" id="lmAdd" style="width:auto;margin:0;padding:8px 16px" data-i18n="btn.add">+ Add</button>
              </div>
            </div>
          </div>
        </div>
        <div style="margin-top:18px; height:14px"><span class="saved" id="savedMsgL">&#10003; <span data-i18n="saved">saved to config</span></span></div>
      </section>

      <section class="view" data-view="atlas" hidden>
        <div class="panel-grid">
          <div class="card" style="grid-column:1/-1">
            <h3 style="display:flex;align-items:center;gap:10px"><span data-i18n="tab.atlas">Atlas</span>
              <span class="tag" id="atlasStatus">&mdash;</span>
              <span style="flex:1"></span>
              <button class="chip" id="atlasRefresh" title="Re-read the open Atlas">&#8635; <span data-i18n="btn.refresh">Refresh</span></button>
              <button class="chip" id="atlasHelp" title="How it works" style="width:28px;padding:6px 0;text-align:center">?</button>
            </h3>

            <!-- help popover (collapsed by default) -->
            <div id="atlasHelpBox" hidden class="hint-row" data-i18n="atlas.help" style="margin:0 0 10px;padding:9px 11px;border:1px solid var(--line);border-radius:6px;line-height:1.6">
              Open the Atlas in-game, then <b>Refresh</b>. Each row is a map type or rolled content read from memory.
              Per row toggle <b>&#9745; Highlight</b> (ring it in-game), <b style="color:#3ddc97">&#8674; Nav</b> (draw a route to it),
              <b style="color:#e0b341">&#10148; Arrow</b> (edge pointer when off-screen) &mdash; independent. Click any column header to sort.
              Hover a tile in-game + press <b>F10</b> to inspect it.
            </div>

            <!-- quick presets -->
            <div class="controls" id="atlasPresets" style="gap:6px;margin:0 0 8px;flex-wrap:wrap">
              <span class="hint-row" style="opacity:.7;margin-right:2px"><span data-i18n="atlas.quickSet">Quick&nbsp;set:</span></span>
              <button class="chip" data-preset="citadels">&#9733; <span data-i18n="atlas.citadels">Citadels</span></button>
              <button class="chip" data-preset="deadly">&#9760; <span data-i18n="atlas.deadly">Deadly Boss</span></button>
              <button class="chip" data-preset="bosses" data-i18n="atlas.bosses">Bosses</button>
              <button class="chip" data-preset="towers" data-i18n="atlas.towers">Towers</button>
              <button class="chip" data-preset="uniques" data-i18n="atlas.uniques">Uniques</button>
            </div>

            <!-- display options (#3 declutter / #5 content icons) — persisted via /api/settings -->
            <div class="controls" id="atlasOpts" style="gap:14px;margin:0 0 8px;flex-wrap:wrap;font-size:12px">
              <label title="Hide maps you've already completed (declutter)"><input type="checkbox" data-atset="atlasHideCompleted"> <span data-i18n="atlas.hideCompleted">Hide completed</span></label>
              <label title="Hide maps you can run right now"><input type="checkbox" data-atset="atlasHideAccessible"> <span data-i18n="atlas.hideAccessible">Hide accessible</span></label>
              <label title="Draw in-game content art above tracked + fogged maps"><input type="checkbox" data-atset="atlasShowContentIcons"> <span data-i18n="atlas.contentIcons">Content icons</span></label>
              <label title="Content icon size (px)"><span data-i18n="atlas.iconSize">Icon size</span> <input type="number" data-atset="atlasContentIconSize" min="12" max="64" step="1" style="width:56px"></label>
              <label title="Spacing of the directional arrows along routes"><span data-i18n="atlas.arrowSpacing">Arrow spacing</span> <input type="number" data-atset="atlasRouteArrowSpacing" min="1.5" max="18" step="0.5" style="width:56px"></label>
            </div>

            <!-- active rules (removable chips) -->
            <div id="atlasActive" style="margin:0 0 8px"></div>

            <!-- group filter + search -->
            <div class="controls" style="gap:6px;margin:0 0 8px;flex-wrap:wrap">
              <button class="chip on" data-group="all" data-i18n="pick.all">All</button>
              <button class="chip" data-group="Kind" data-i18n="atlas.kind">Kind</button>
              <button class="chip" data-group="Type" data-i18n="atlas.type">Type</button>
              <button class="chip" data-group="Content" data-i18n="atlas.content">Content</button>
              <button class="chip" data-group="Map" data-i18n="atlas.map">Map</button>
              <span style="flex:1"></span>
              <button class="chip" id="atlasHlSelOnly" data-i18n="atlas.activeOnly">Active only</button>
              <button class="chip" id="atlasHlClear" data-i18n="atlas.clearAll">Clear all</button>
              <input type="search" id="atlasHlFilter" placeholder="search&hellip;" style="width:160px">
            </div>

            <div id="atlasHlTable" style="max-height:460px;overflow:auto;border:1px solid var(--line);border-radius:6px">
              <span class="hint-row" data-i18n="atlas.emptyTable" style="padding:8px;display:block">Open the Atlas in-game + Refresh to list filters.</span>
            </div>
          </div>

          <!-- #7 colour groups: a named set of map names that all draw in one ring/label colour. -->
          <div class="card" style="grid-column:1/-1">
            <h3 style="display:flex;align-items:center;gap:10px"><span data-i18n="atlas.mapColourGroups">Map colour groups</span>
              <span class="hint-row" data-i18n="atlas.colourHint" style="opacity:.7;font-weight:400">recolour a whole category at once (Citadels, Halls, Uniques&hellip;)</span>
              <span style="flex:1"></span>
              <button class="chip" id="atlasGroupAdd" data-i18n="atlas.addGroup">+ Add group</button>
            </h3>
            <div id="atlasGroups"></div>
          </div>
        </div>
      </section>

      <section class="view" data-view="settings" hidden>
        <div class="panel-grid">
          <div class="card">
            <h3 data-i18n="sec.radarDisplay">Radar Display</h3>
            <div class="row"><div class="rl"><span data-i18n="set.showTerrain">Show terrain</span><small data-i18n="set.showTerrainHint">walkable-terrain bitmap</small></div>
              <label class="sw"><input type="checkbox" data-set="showTerrain"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.showPlayerBlip">Show player blip</span><small data-i18n="set.showPlayerBlipHint">blue dot marking your own position</small></div>
              <label class="sw"><input type="checkbox" data-set="showPlayerBlip"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.showMinimap">Minimap radar</span><small data-i18n="set.showMinimapHint">own circular corner map (game map made transparent)</small></div>
              <label class="sw"><input type="checkbox" data-set="showMinimap"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.minimapCorner">Minimap corner</span><small data-i18n="set.minimapCornerHint">which corner the circle is pinned to</small></div>
              <select class="numin selin" data-set="minimapCorner">
                <option value="TopRight">Top Right</option>
                <option value="TopLeft">Top Left</option>
                <option value="BottomLeft">Bottom Left</option>
                <option value="BottomRight">Bottom Right</option>
              </select></div>
            <div class="row"><div class="rl"><span data-i18n="set.minimapSize">Minimap size</span><small data-i18n="set.minimapSizeHint">circle diameter in pixels (80&ndash;600)</small></div>
              <input class="numin" type="number" step="10" min="80" max="600" data-set="minimapSize"></div>
            <div class="row"><div class="rl"><span data-i18n="set.minimapZoom">Minimap zoom</span><small data-i18n="set.minimapZoomHint">× the large-map scale (higher = zoomed in)</small></div>
              <input class="numin" type="number" step="0.05" min="0.25" max="8" data-set="minimapZoom"></div>
            <div class="row"><div class="rl"><span data-i18n="set.alwaysShow">Always show overlay</span><small data-i18n="set.alwaysShowHint">draw even when PoE2 isn&rsquo;t focused (e.g. while tweaking this dashboard); auto-flask stays focus-gated</small></div>
              <label class="sw"><input type="checkbox" data-set="alwaysShowOverlay"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.hideJunk">Hide junk entities</span><small data-i18n="set.hideJunkHint">suppress cosmetic / FX / daemon dots</small></div>
              <label class="sw"><input type="checkbox" data-set="hideJunk"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.navPaths">Navigation paths</span><small data-i18n="set.navPathsHint">draw A&#42; routes to selected landmarks</small></div>
              <label class="sw"><input type="checkbox" data-set="showPath"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.showWorldPaths">World paths (no map)</span><small data-i18n="set.showWorldPathsHint">draw the routes on the world ground when the big map is closed (off = minimap only)</small></div>
              <label class="sw"><input type="checkbox" data-set="showWorldPaths"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.curated">Curated landmark names</span><small data-i18n="set.curatedHint">community labels (boss / reward / exits)</small></div>
              <label class="sw"><input type="checkbox" data-set="useCuratedLandmarks"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.gh2Landmarks">GameHelper2 landmarks</span><small data-i18n="set.gh2LandmarksHint">endgame boss arenas + dungeon stairs (GH2 reference set)</small></div>
              <label class="sw"><input type="checkbox" data-set="useGh2Landmarks"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.autoBoss">Auto-detect boss rooms</span><small data-i18n="set.autoBossHint">flag any tile named &ldquo;boss&rdquo;/&ldquo;arena&rdquo; as a Boss landmark (maps the curated lists don&rsquo;t cover)</small></div>
              <label class="sw"><input type="checkbox" data-set="autoDetectBossRooms"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row" style="border:1px solid var(--line);border-radius:8px;padding:6px 10px"><div class="rl"><b data-i18n="set.gh2Radar">GH2 Radar (full)</b><small data-i18n="set.gh2RadarHint">replicate the GameHelper2 Radar plugin&rsquo;s extra icon recognition (tormented spirits, abyss, delirium, incursion, sekhemas, expedition chests, strongbox subtypes, campaign runestones) as a layer over the local radar &mdash; flip in-game to compare</small></div>
              <label class="sw"><input type="checkbox" data-set="useGh2Radar"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="set.fpsCap">Overlay FPS cap</span><small data-i18n="set.fpsCapHint">lower = less load on the game; 60 is smooth for a radar (15&ndash;360)</small></div>
              <input class="numin" type="number" step="1" min="15" max="360" data-set="fpsCap"></div>
            <div class="row"><div class="rl"><span data-i18n="set.language">Language</span><small data-i18n="set.languageHint">radar UI + on-map terms (mechanic / POI labels)</small></div>
              <select class="numin selin" data-set="language">
                <option value="en">English</option>
                <option value="zh-CN">简体中文</option>
                <option value="zh-Hant">繁體中文</option>
              </select></div>
          </div>
          <div class="card">
            <h3><span data-i18n="sec.hpBars">Monster HP Bars</span> <span class="tag">&middot; <span data-i18n="sec.hpBarsTag">by rarity</span></span></h3>
            <div class="row"><div class="rl hint-row" data-i18n="hp.hint1">Toggle the bar on/off per rarity with the <b>On</b> checkbox &mdash; uncheck all to disable HP bars entirely, or leave only the rarities you want. The rest sets the bar <i>geometry</i> per rarity.</div></div>
            <div class="hpgrid">
              <span class="hph" data-i18n="hp.on">On</span><span class="hph" data-i18n="hp.rarity">Rarity</span><span class="hph" data-i18n="hp.width">Width</span><span class="hph" data-i18n="hp.border">Border</span><span class="hph" data-i18n="hp.thick">Thick</span>
              <input type="checkbox" data-set="hpBarNormal">
              <span class="hpr" data-i18n="hp.normal">Normal</span>
              <input class="numin" type="number" step="1" min="4" data-hp="widthNormal">
              <input type="color" class="i-color" data-hpcolor="borderColorNormal">
              <input class="numin" type="number" step="0.5" min="0" max="20" data-hp="borderNormal">
              <input type="checkbox" data-set="hpBarMagic">
              <span class="hpr" style="color:var(--magic)" data-i18n="hp.magic">Magic</span>
              <input class="numin" type="number" step="1" min="4" data-hp="widthMagic">
              <input type="color" class="i-color" data-hpcolor="borderColorMagic">
              <input class="numin" type="number" step="0.5" min="0" max="20" data-hp="borderMagic">
              <input type="checkbox" data-set="hpBarRare">
              <span class="hpr" style="color:var(--rare)" data-i18n="hp.rare">Rare</span>
              <input class="numin" type="number" step="1" min="4" data-hp="widthRare">
              <input type="color" class="i-color" data-hpcolor="borderColorRare">
              <input class="numin" type="number" step="0.5" min="0" max="20" data-hp="borderRare">
              <input type="checkbox" data-set="hpBarUnique">
              <span class="hpr" style="color:var(--unique)" data-i18n="hp.unique">Unique</span>
              <input class="numin" type="number" step="1" min="4" data-hp="widthUnique">
              <input type="color" class="i-color" data-hpcolor="borderColorUnique">
              <input class="numin" type="number" step="0.5" min="0" max="20" data-hp="borderUnique">
            </div>
            <div class="hpshared">
              <label><span data-i18n="hp.height">Height</span><input class="numin" type="number" step="1" min="1" max="30" data-hp="height"></label>
              <label><span data-i18n="hp.offsetX">Offset X</span><input class="numin" type="number" step="1" data-hp="offsetX"></label>
              <label><span data-i18n="hp.offsetY">Offset Y</span><input class="numin" type="number" step="1" data-hp="offsetY"></label>
            </div>
            <div class="row"><div class="rl hint-row" data-i18n="hp.hint2">Bar fill follows the monster icon color; set border color &amp; thickness per rarity (thickness 0 = no border). Offset Y negative = above the mob.</div></div>
          </div>
          <div class="card">
            <h3><span data-i18n="sec.terrain">Terrain</span> <span class="tag">&middot; <span data-i18n="sec.terrainTag">walkable overlay</span></span></h3>
            <div class="row"><div class="rl"><span data-i18n="ter.interior">Interior fill</span><small data-i18n="ter.interiorHint">wash over walkable cells</small></div>
              <span class="trow-ctl">
                <input type="color" class="i-color" data-tcolor="interiorColor">
                <input type="range" class="op" min="0" max="100" data-topacity="interiorOpacity">
                <span class="opv" data-topv="interiorOpacity">—</span></span></div>
            <div class="row"><div class="rl" style="color:var(--poi)"><span data-i18n="ter.edge">Wall edge</span><small data-i18n="ter.edgeHint">outlines around rooms</small></div>
              <span class="trow-ctl">
                <input type="color" class="i-color" data-tcolor="edgeColor">
                <input type="range" class="op" min="0" max="100" data-topacity="edgeOpacity">
                <span class="opv" data-topv="edgeOpacity">—</span></span></div>
            <div class="row"><div class="rl hint-row" data-i18n="ter.hint">Edits rebuild the terrain bitmap; use &ldquo;Show terrain&rdquo; above to hide it entirely.</div></div>
          </div>
          <div class="card">
            <h3 data-i18n="sec.calibration">Map Calibration</h3>
            <div class="row"><div class="rl"><span data-i18n="cal.scale">Scale multiplier</span><small data-i18n="cal.scaleHint">projection scale of the map overlay</small></div>
              <input class="numin" type="number" step="0.01" data-set="scaleMul"></div>
            <div class="row"><div class="rl"><span data-i18n="cal.offsetX">Offset X</span></div><input class="numin" type="number" step="1" data-set="offX"></div>
            <div class="row"><div class="rl"><span data-i18n="cal.offsetY">Offset Y</span></div><input class="numin" type="number" step="1" data-set="offY"></div>
            <div class="row"><div class="rl hint-row" data-i18n="cal.hint">Adjust here &mdash; changes apply live (no in-game hotkeys).</div></div>
          </div>
          <div class="card">
            <h3 data-i18n="sec.autoFlask">Auto-Flask</h3>
            <div class="row"><div class="rl"><span data-i18n="flask.mode">Life flask triggers on</span><small data-i18n="flask.modeHint">which pool the life flask key watches &mdash; ES is ignored if your build has none</small></div>
              <select class="numin selin" data-set="lifeFlaskMode">
                <option value="Health" data-i18n="flask.modeHealth">Health %</option>
                <option value="EnergyShield" data-i18n="flask.modeEs">Energy Shield %</option>
                <option value="Either" data-i18n="flask.modeEither">Either (HP or ES)</option>
              </select></div>
            <div class="row"><div class="rl"><span data-i18n="flask.lifeThr">Life threshold %</span><small data-i18n="flask.lifeThrHint">tap life flask below this Life %</small></div>
              <input class="numin" type="number" step="1" min="0" max="100" data-set="lifeThresholdPct"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.esThr">ES threshold %</span><small data-i18n="flask.esThrHint">tap life flask below this Energy Shield % (ES / Either modes)</small></div>
              <input class="numin" type="number" step="1" min="0" max="100" data-set="esThresholdPct"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.manaThr">Mana threshold %</span><small data-i18n="flask.manaThrHint">tap mana flask below this Mana %</small></div>
              <input class="numin" type="number" step="1" min="0" max="100" data-set="manaThresholdPct"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.lifeKey">Life flask key</span></div>
              <input class="numin keyin" type="text" maxlength="1" data-set="lifeKey"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.manaKey">Mana flask key</span></div>
              <input class="numin keyin" type="text" maxlength="1" data-set="manaKey"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.lifeCd">Life cooldown</span><small data-i18n="flask.lifeCdHint">min ms between life taps</small></div>
              <input class="numin" type="number" step="100" min="0" data-set="lifeCooldownMs"></div>
            <div class="row"><div class="rl"><span data-i18n="flask.manaCd">Mana cooldown</span><small data-i18n="flask.manaCdHint">min ms between mana taps</small></div>
              <input class="numin" type="number" step="100" min="0" data-set="manaCooldownMs"></div>
            <div class="row"><div class="rl hint-row"><span data-i18n="flask.hint">F8 toggles auto-flask in-game. Status:</span> <span id="flaskState">&mdash;</span></div></div>
          </div>
          <div class="card">
            <h3><span data-i18n="sec.cameraZoom">Camera Zoom</span> <span class="tag">&middot; <span data-i18n="sec.cameraZoomTag">opt-in memory patch</span></span></h3>
            <div class="row"><div class="rl hint-row" style="color:var(--blood-bright)" data-i18n="zoom.warn">Writes to the game process &mdash; bypasses POE2Radar&rsquo;s external read-only boundary and may violate the PoE2 Terms of Service. Off by default; use at your own risk.</div></div>
            <div class="row"><div class="rl"><span data-i18n="zoom.enable">Enable zoom-out</span><small data-i18n="zoom.enableHint">let the camera zoom out further than the game normally allows</small></div>
              <label class="sw"><input type="checkbox" data-zoom="enabled"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="zoom.value">Zoom clamp value</span><small data-i18n="zoom.valueHint">higher = further out (the minss clamp target)</small></div>
              <input class="numin" type="number" step="1" min="1" max="200" data-zoom="zoomValue"></div>
            <div class="row"><div class="rl hint-row"><span data-i18n="state.statusPh">Status:</span> <span id="zoomState" style="color:var(--ink-dim)">&mdash;</span></div></div>
          </div>
        </div>
        <div style="margin-top:18px; height:14px"><span class="saved" id="savedMsg">&#10003; <span data-i18n="saved">saved to config</span></span></div>
      </section>

      <section class="view" data-view="value" hidden>
        <div class="panel-grid">
          <!-- Shared pricing config: applies to BOTH the ground overlay and the hover chip. -->
          <div class="card" style="grid-column:1/-1">
            <h3><span data-i18n="sec.generalPricing">General Pricing</span> <span class="tag">&middot; <span data-i18n="sec.generalPricingTag">poe.ninja</span></span></h3>
            <div class="row"><div class="rl hint-row" data-i18n="price.hint">These apply to <b>everything priced</b> below &mdash; ground loot, hover, monolith &amp; ritual rewards. Prices come from poe.ninja for the detected league.</div></div>
            <div class="row"><div class="rl"><span data-i18n="price.league">Price league</span><small data-i18n="price.leagueHint">leave blank to auto-detect your league (HC/SC/Standard) from the game</small></div>
              <input class="numin" type="text" id="giLeague" data-gi="league" placeholder="auto-detect" style="width:200px"></div>
            <div class="row"><div class="rl"><span data-i18n="price.minQty">Low-listing warning</span><small data-i18n="price.minQtyHint">flag a price backed by fewer than N live listings with a &ldquo;?&rdquo; (possible mislisting). 0 = never flag</small></div>
              <input class="numin" type="number" step="1" min="0" data-gi="minQuantity"></div>
            <div class="row"><div class="rl hint-row"><span data-i18n="price.statusPh">Pricing status:</span> <span id="priceStatus" style="color:var(--ink-dim)">&mdash;</span></div></div>
          </div>

          <!-- Ground loot value labels. -->
          <div class="card">
            <h3 data-i18n="sec.groundLoot">Ground Loot</h3>
            <div class="row"><div class="rl"><span data-i18n="ground.show">Show ground loot value</span><small data-i18n="ground.showHint">draw a value label over dropped items on the map</small></div>
              <label class="sw"><input type="checkbox" data-gi="enabled"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl hint-row" data-i18n="ground.catHint">Show a label for these categories:</div></div>
            <div class="chips" id="giCats">
              <span class="chip" data-gicat="Uniques" data-i18n="gi.uniques">Uniques</span>
              <span class="chip" data-gicat="Currency" data-i18n="gi.currency">Currency</span>
              <span class="chip" data-gicat="Runes" data-i18n="gi.runes">Runes</span>
              <span class="chip" data-gicat="SoulCores" data-i18n="gi.soulcores">Soul Cores</span>
              <span class="chip" data-gicat="UncutGems" data-i18n="gi.uncutgems">Uncut Gems</span>
              <span class="chip" data-gicat="Essences" data-i18n="gi.essences">Essences</span>
              <span class="chip" data-gicat="Fragments" data-i18n="gi.fragments">Fragments</span>
              <span class="chip" data-gicat="Tablets" data-i18n="gi.tablets">Tablets</span>
              <span class="chip" data-gicat="Delirium" data-i18n="gi.delirium">Delirium</span>
              <span class="chip" data-gicat="Idols" data-i18n="gi.idols">Idols</span>
              <span class="chip" data-gicat="Abyss" data-i18n="gi.abyss">Abyss</span>
              <span class="chip" data-gicat="Ritual" data-i18n="gi.ritual">Ritual</span>
              <span class="chip" data-gicat="Breach" data-i18n="gi.breach">Breach</span>
              <span class="chip" data-gicat="Expedition" data-i18n="gi.expedition">Expedition</span>
            </div>
            <div class="row"><div class="rl hint-row" data-i18n="ground.bucketHint">Minimum value to show, per bucket (Ex) &mdash; drops below the floor are hidden:</div></div>
            <div class="row"><div class="rl"><span data-i18n="ground.uniqueMin">Uniques min</span><small data-i18n="ground.uniqueMinHint">hide uniques under this (Ex)</small></div>
              <input class="numin" type="number" step="0.1" min="0" data-gi="uniqueMinEx"></div>
            <div class="row"><div class="rl"><span data-i18n="ground.currencyMin">Currency min</span><small data-i18n="ground.currencyMinHint">hide currency under this (Ex)</small></div>
              <input class="numin" type="number" step="0.1" min="0" data-gi="currencyMinEx"></div>
            <div class="row"><div class="rl"><span data-i18n="ground.otherMin">Other min</span><small data-i18n="ground.otherMinHint">runes / essences / fragments / … (Ex)</small></div>
              <input class="numin" type="number" step="0.1" min="0" data-gi="otherMinEx"></div>
            <div class="row"><div class="rl"><span data-i18n="ground.highlight">Highlight threshold</span><small data-i18n="ground.highlightHint">border/emphasis at or above this value (Ex)</small></div>
              <input class="numin" type="number" step="1" min="0" data-gi="highlightMinEx"></div>
            <div class="row"><div class="rl hint-row" data-i18n="ground.unidHint">Unidentified uniques reveal their NAME + value; everything else (identified uniques, currency, runes, essences, …) shows the value only.</div></div>
          </div>

          <!-- Hover price chip (any item UI). -->
          <div class="card">
            <h3 data-i18n="sec.onHover">On Hover</h3>
            <div class="row"><div class="rl"><span data-i18n="hover.show">Show item value on hover</span><small data-i18n="hover.showHint">a price chip beside the game tooltip in inventory / stash / vendor / reward UIs</small></div>
              <label class="sw"><input type="checkbox" data-hv="enabled"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="hover.highlight">Highlight threshold</span><small data-i18n="hover.highlightHint">emphasize the chip at or above this (stack) value (Ex)</small></div>
              <input class="numin" type="number" step="1" min="0" data-hv="highlightMinEx"></div>
            <div class="row"><div class="rl hint-row" data-i18n="hover.hint">Hovering is explicit intent, so this ignores the ground category toggles &amp; value floors &mdash; any priced item shows. Stacks show the per-unit price and the stack total.</div></div>
          </div>

          <!-- Monolith (expedition) reward overlay — a value/pricing feature, grouped here. -->
          <div class="card">
            <h3><span data-i18n="sec.monolithRewards">Monolith Rewards</span> <span class="tag">&middot; <span data-i18n="sec.monolithRewardsTag">expedition</span></span></h3>
            <div class="row"><div class="rl"><span data-i18n="mono.enabled">Enabled</span><small data-i18n="mono.enabledHint">read + price runeshape-monolith rewards</small></div>
              <label class="sw"><input type="checkbox" data-mono="enabled"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="mono.minValue">Min value to show / auto-path</span><small data-i18n="mono.minValueHint">hide the monolith entirely (icon, panel, auto-nav) below this (Ex). 0 = show every monolith</small></div>
              <input class="numin" type="number" step="1" min="0" data-mono="minValueEx"></div>
            <div class="row"><div class="rl"><span data-i18n="mono.highlight">Highlight threshold</span><small data-i18n="mono.highlightHint">green value tier at or above this (Ex)</small></div>
              <input class="numin" type="number" step="1" min="0" data-mono="highlightMinEx"></div>
            <div class="row"><div class="rl"><span data-i18n="mono.hideCollected">Hide collected</span><small data-i18n="mono.hideCollectedHint">drop monoliths whose reward was already claimed</small></div>
              <label class="sw"><input type="checkbox" data-mono="hideCollected"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="mono.showPanel">Show reward panel</span><small data-i18n="mono.showPanelHint">the in-overlay nearby-monolith reward list</small></div>
              <label class="sw"><input type="checkbox" data-mono="showPanel"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="mono.showLabel">Show map label</span><small data-i18n="mono.showLabelHint">draw value + top reward at the icon</small></div>
              <label class="sw"><input type="checkbox" data-mono="showMapLabel"><span class="track"></span><span class="knob"></span></label></div>
          </div>

          <!-- Currency Exchange depth panel — value/pricing feature, grouped here. -->
          <div class="card">
            <h3><span data-i18n="sec.currencyExchange">Currency Exchange</span> <span class="tag">&middot; <span data-i18n="sec.currencyExchangeTag">Kalguur market</span></span></h3>
            <div class="row"><div class="rl"><span data-i18n="ce.enabled">Enabled</span><small data-i18n="ce.enabledHint">show the order-book depth panel when the exchange is open</small></div>
              <label class="sw"><input type="checkbox" data-ce="enabled"><span class="track"></span><span class="knob"></span></label></div>
            <div class="row"><div class="rl"><span data-i18n="ce.maxRows">Max rows</span><small data-i18n="ce.maxRowsHint">ladder rows to show per side</small></div>
              <input class="numin" type="number" step="1" min="1" max="64" data-ce="maxRows"></div>
            <div class="row"><div class="rl hint-row" data-i18n="ce.hint">When the in-game Currency Exchange is open, a top-right panel lists the best offered/wanted ratios + depth (the best row of each side is highlighted).</div></div>
          </div>
        </div>
        <div style="margin-top:18px; height:14px"><span class="saved" id="savedMsg2">&#10003; <span data-i18n="saved">saved to config</span></span></div>
      </section>

    </main>
  </div>
</div>

<script>
const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
let state=null, zone=null;
let activeTab='filters';
let atlasData=null, atlasView='region', atlasSel=new Set(), atlasHl=null, atlasNav=null, atlasArrow=null, atlasHlSelOnly=false, atlasGroup='all';

/* ── trilingual UI (en / zh-CN / zh-Hant). Static text uses data-i18n; dynamic JS uses t(). ── */
const I18N = {
'en': {
  'tab.rules':'Rules','tab.landmarks':'Landmarks','tab.atlas':'Atlas','tab.value':'Item Value','tab.settings':'Settings',
  'side.life':'Life','side.es':'Energy Shield','side.mana':'Mana','side.zone':'Zone','side.area':'Area',
  'side.areaCode':'Area code','side.actLevel':'Act / Level','side.mapOpen':'Map open','side.autoFlask':'Auto-flask',
  'side.census':'Census','side.entities':'Entities','side.poi':'Points of Int.','side.monsters':'Monsters',
  'side.landmarks':'Landmarks','side.monolithRewards':'Monolith Rewards',
  'conn.live':'live','conn.offline':'offline','state.ingame':'in game','state.town':'town/menu',
  'state.yes':'yes','state.no':'no','state.on':'on','state.off':'off',
  'sec.displayRules':'Display Rules','sec.displayRulesTag':'one ordered ruleset — first match wins',
  'sec.hidden':'Hidden','sec.hiddenTag':'cull entirely from radar, list & nav',
  'sec.landmarks':'Landmarks','sec.landmarksTag':'curated map labels — view, fix, share',
  'sec.radarDisplay':'Radar Display','sec.hpBars':'Monster HP Bars','sec.hpBarsTag':'by rarity',
  'sec.terrain':'Terrain','sec.terrainTag':'walkable overlay','sec.calibration':'Map Calibration',
  'sec.autoFlask':'Auto-Flask','sec.cameraZoom':'Camera Zoom','sec.cameraZoomTag':'opt-in memory patch',
  'sec.generalPricing':'General Pricing','sec.generalPricingTag':'poe.ninja','sec.groundLoot':'Ground Loot',
  'sec.onHover':'On Hover','sec.monolithRewards':'Monolith Rewards','sec.monolithRewardsTag':'expedition',
  'sec.currencyExchange':'Currency Exchange','sec.currencyExchangeTag':'Kalguur market',
  'set.showTerrain':'Show terrain','set.showTerrainHint':'walkable-terrain bitmap',
  'set.showPlayerBlip':'Show player blip','set.showPlayerBlipHint':'blue dot marking your own position',
  'set.showMinimap':'Minimap radar','set.showMinimapHint':'own circular corner map (game map made transparent)',
  'set.minimapCorner':'Minimap corner','set.minimapCornerHint':'which corner the circle is pinned to',
  'set.minimapSize':'Minimap size','set.minimapSizeHint':'circle diameter in pixels (80–600)',
  'set.minimapZoom':'Minimap zoom','set.minimapZoomHint':'× the large-map scale (higher = zoomed in)',
  'set.alwaysShow':'Always show overlay','set.alwaysShowHint':'draw even when PoE2 isn’t focused; auto-flask stays focus-gated',
  'set.hideJunk':'Hide junk entities','set.hideJunkHint':'suppress cosmetic / FX / daemon dots',
  'set.navPaths':'Navigation paths','set.navPathsHint':'draw A* routes to selected landmarks',
  'set.showWorldPaths':'World paths (no map)','set.showWorldPathsHint':'draw routes on the world ground when the big map is closed (off = minimap only)',
  'set.curated':'Curated landmark names','set.curatedHint':'community labels (boss / reward / exits)',
  'set.gh2Landmarks':'GameHelper2 landmarks','set.gh2LandmarksHint':'endgame boss arenas + dungeon stairs (GH2 reference set)',
  'set.autoBoss':'Auto-detect boss rooms','set.autoBossHint':'flag any tile named “boss”/“arena” as a Boss landmark',
  'set.gh2Radar':'GH2 Radar (full)','set.gh2RadarHint':'replicate the GameHelper2 Radar plugin’s extra icon recognition',
  'set.fpsCap':'Overlay FPS cap','set.fpsCapHint':'lower = less load on the game',
  'set.language':'Language','set.languageHint':'radar UI + on-map terms (mechanic / POI labels)',
  'hp.on':'On','hp.rarity':'Rarity','hp.width':'Width','hp.border':'Border','hp.thick':'Thick',
  'hp.normal':'Normal','hp.magic':'Magic','hp.rare':'Rare','hp.unique':'Unique',
  'hp.height':'Height','hp.offsetX':'Offset X','hp.offsetY':'Offset Y',
  'ter.interior':'Interior fill','ter.interiorHint':'wash over walkable cells',
  'ter.edge':'Wall edge','ter.edgeHint':'outlines around rooms',
  'cal.scale':'Scale multiplier','cal.scaleHint':'projection scale of the map overlay',
  'cal.offsetX':'Offset X','cal.offsetY':'Offset Y',
  'flask.mode':'Life flask triggers on','flask.modeHint':'which pool the life flask key watches — ES is ignored if your build has none',
  'flask.modeHealth':'Health %','flask.modeEs':'Energy Shield %','flask.modeEither':'Either (HP or ES)',
  'flask.lifeThr':'Life threshold %','flask.lifeThrHint':'tap life flask below this Life %',
  'flask.esThr':'ES threshold %','flask.esThrHint':'tap life flask below this Energy Shield % (ES / Either modes)',
  'flask.manaThr':'Mana threshold %','flask.manaThrHint':'tap mana flask below this Mana %',
  'flask.lifeKey':'Life flask key','flask.manaKey':'Mana flask key',
  'flask.lifeCd':'Life cooldown','flask.lifeCdHint':'min ms between life taps',
  'flask.manaCd':'Mana cooldown','flask.manaCdHint':'min ms between mana taps',
  'zoom.enable':'Enable zoom-out','zoom.enableHint':'let the camera zoom out further than the game normally allows',
  'zoom.value':'Zoom clamp value','zoom.valueHint':'higher = further out (the minss clamp target)',
  'price.league':'Price league','price.leagueHint':'leave blank to auto-detect your league (HC/SC/Standard)',
  'price.minQty':'Low-listing warning','price.minQtyHint':'flag a price backed by fewer than N listings with a “?”',
  'ground.show':'Show ground loot value','ground.showHint':'draw a value label over dropped items on the map',
  'ground.uniqueMin':'Uniques min','ground.uniqueMinHint':'hide uniques under this (Ex)',
  'ground.currencyMin':'Currency min','ground.currencyMinHint':'hide currency under this (Ex)',
  'ground.otherMin':'Other min','ground.otherMinHint':'runes / essences / fragments / … (Ex)',
  'ground.highlight':'Highlight threshold','ground.highlightHint':'border/emphasis at or above this value (Ex)',
  'hover.show':'Show item value on hover','hover.showHint':'a price chip beside the game tooltip',
  'hover.highlight':'Highlight threshold','hover.highlightHint':'emphasize the chip at or above this (stack) value (Ex)',
  'mono.enabled':'Enabled','mono.enabledHint':'read + price runeshape-monolith rewards',
  'mono.minValue':'Min value to show / auto-path','mono.minValueHint':'hide the monolith entirely below this (Ex). 0 = show every monolith',
  'mono.highlight':'Highlight threshold','mono.highlightHint':'green value tier at or above this (Ex)',
  'mono.hideCollected':'Hide collected','mono.hideCollectedHint':'drop monoliths whose reward was already claimed',
  'mono.showPanel':'Show reward panel','mono.showPanelHint':'the in-overlay nearby-monolith reward list',
  'mono.showLabel':'Show map label','mono.showLabelHint':'draw value + top reward at the icon',
  'ce.enabled':'Enabled','ce.enabledHint':'show the order-book depth panel when the exchange is open',
  'ce.maxRows':'Max rows','ce.maxRowsHint':'ladder rows to show per side',
  'cat.Monster':'Monster','cat.Chest':'Chest','cat.Player':'Player','cat.Npc':'NPC','cat.Object':'Terrain',
  'cat.Other':'Misc / POI','cat.Transition':'Transition','cat.Tile':'Tile',
  'cat.Monsters':'Monsters','cat.Chests':'Chests','cat.Misc':'Misc / POI','cat.Terrain':'Terrain',
  'cat.NPCs':'NPCs','cat.Transitions':'Transitions',
  'rar.Normal':'Normal','rar.Magic':'Magic','rar.Rare':'Rare','rar.Unique':'Unique',
  'sel.rarity':'Rarity','sel.reaction':'Reaction','sel.life':'Life','sel.chest':'Chest','sel.poi':'POI','sel.encounter':'Encounter',
  'sel.any':'any','sel.hostile':'Hostile','sel.friendly':'Friendly','sel.alive':'Alive','sel.dead':'Dead',
  'sel.opened':'Opened','sel.unopened':'Unopened','sel.yes':'Yes','sel.no':'No','sel.active':'Active','sel.complete':'Complete',
  'btn.add':'+ Add','btn.remove':'Remove','btn.delete':'Delete','btn.hide':'+ Hide','btn.restore':'Restore',
  'btn.import':'Import…','btn.export':'Export','btn.addFromGame':'+ Add from game data…','btn.addBlank':'+ Add blank rule',
  'btn.thisArea':'This area only','btn.refresh':'Refresh','btn.hide':'Hide','btn.autoPath':'Auto-path',
  'pick.all':'All','pick.entities':'Entities','pick.tiles':'Tiles','pick.mods':'Mods',
  'pick.filterPh':'filter by name / metadata / tile path / mod id…',
  'pick.loading':'Loading…','pick.noMatches':'No matches','pick.noMatchesGame':' — are you in game?',
  'atlas.statusScan':'scanning…','atlas.statusClosed':'atlas closed — open it in-game + Refresh',
  'atlas.title':'Title','atlas.count':'Count','atlas.category':'Category','atlas.map':'Map','atlas.content':'Content',
  'atlas.biome':'Biome','atlas.pos':'Pos','atlas.noNodes':'No live nodes (open the Atlas in-game, then Refresh).',
  'gi.uniques':'Uniques','gi.currency':'Currency','gi.runes':'Runes','gi.soulcores':'Soul Cores','gi.uncutgems':'Uncut Gems',
  'gi.essences':'Essences','gi.fragments':'Fragments','gi.tablets':'Tablets','gi.delirium':'Delirium','gi.idols':'Idols',
  'gi.abyss':'Abyss','gi.ritual':'Ritual','gi.breach':'Breach','gi.expedition':'Expedition',
  'state.act':'Act','state.lvl':'lvl',
  'atlas.mapColourGroups':'Map colour groups','atlas.addGroup':'+ Add group','atlas.quickSet':'Quick set',
  'atlas.citadels':'Citadels','atlas.deadly':'Deadly Boss','atlas.bosses':'Bosses','atlas.towers':'Towers','atlas.uniques':'Uniques',
  'atlas.kind':'Kind','atlas.type':'Type','atlas.activeOnly':'Active only','atlas.clearAll':'Clear all',
  'atlas.hideCompleted':'Hide completed','atlas.hideAccessible':'Hide accessible','atlas.contentIcons':'Content icons',
  'atlas.iconSize':'Icon size','atlas.arrowSpacing':'Arrow spacing',
  'icon.monsterNormal':'Monster · Normal','icon.monsterMagic':'Monster · Magic','icon.monsterRare':'Monster · Rare','icon.monsterUnique':'Monster · Unique',
  'icon.chestRare':'Chest · Rare','icon.chestUnique':'Chest · Unique','icon.poiLabel':'Point of Interest','icon.landmarkLabel':'Landmark',
  'rules.noRules':'No display rules yet. Add one below.','rules.newRule':'New rule','rules.unnamed':'(unnamed)',
  'rules.ruleNamePh':'rule name','rules.matchPh':'match: metadata terms, comma-separated (blank = any)',
  'rules.modsPh':'monster mods: aura/buff terms, comma-separated (e.g. Aura, ManaSiphon) — blank = any',
  'rules.labelPh':'label (optional)','rules.anyType':'any type','rules.modsLabel':'mods: ',
  'rules.hidePh':'pattern or glob to hide (e.g. AbyssCrack, *Daemon*)','rules.nothingHidden':'Nothing hidden.',
  'lm.searchPh':'filter by area / tile / label…','lm.areaPh':'area (e.g. P2_3, or *)','lm.patPh':'tile path / pattern','lm.labelPh':'label',
  'lm.noLandmarks':'No curated landmarks','lm.forArea':' for this area (','lm.addOne':'Add one below','lm.orOff':' or turn off “This area only”',
  'atlas.emptyTable':'Open the Atlas in-game + Refresh to list filters.',
  'atlas.noGroups':'No groups. Maps in a group draw in its colour when tracked.',
  'atlas.groupNamePh':'group name','atlas.mapsPh':'one map name per line','atlas.newGroup':'New group',
  'atlas.colourHint':'recolour a whole category at once (Citadels, Halls, Uniques…)',
  'atlas.noActive':'No active rules — click a row or a Quick set.',
  'atlas.noFilters':'No filters yet (open the Atlas + Refresh).','atlas.reading':'reading…',
  'atlas.nodes':'nodes','atlas.withContent':'with content','atlas.kinds':'kind','atlas.contents':'content','atlas.mapFilters':'map filters',
  'hp.hint1':'Toggle the bar on/off per rarity with the On checkbox — uncheck all to disable HP bars entirely, or leave only the rarities you want.',
  'hp.hint2':'Bar fill follows the monster icon color; set border color & thickness per rarity (thickness 0 = no border). Offset Y negative = above the mob.',
  'ter.hint':'Edits rebuild the terrain bitmap; use “Show terrain” above to hide it entirely.',
  'cal.hint':'Adjust here — changes apply live (no in-game hotkeys).',
  'flask.hint':'F8 toggles auto-flask in-game. Status:',
  'zoom.warn':'Writes to the game process — bypasses POE2Radar’s external read-only boundary and may violate the PoE2 Terms of Service. Off by default; use at your own risk.',
  'price.statusPh':'Pricing status:','ground.catHint':'Show a label for these categories:',
  'ground.bucketHint':'Minimum value to show, per bucket (Ex) — drops below the floor are hidden:',
  'state.itemsLoaded':'items loaded','state.loading':'loading…','state.auto':'(auto)','state.statusPh':'Status:',
  'mono.collected':'collected','mono.noRewards':'no priced rewards',
  'rules.drHint':'The single source of truth for how every entity draws. Each entity is matched top-to-bottom; the first enabled rule that matches decides everything — icon & color, hidden, HP bar, auto-path. Reorder with ▲/▼ to change precedence. A rule matches on any mix of type, metadata terms, monster mods (auras/buffs), rarity, reaction, life, chest/POI/encounter state; a blank condition means “any”.',
  'rules.hiddenHint':'A stronger cut than a Hide rule: entities whose metadata contains a pattern (or matches a */? glob) are removed everywhere — overlay, entity list, and navigation — before the display rules even run.',
  'lm.hint':'The built-in “known” map features (boss arenas, exits, loot, waypoints…), labelled per area. Rename a wrong label, add your own, or hide a bad entry. Export a corrected list to share or submit for baking into a release; Import to load one. (For how a tile draws — icon/color/hide — use a Tile rule on the Rules tab; this is just the labels.)',
  'atlas.help':'Open the Atlas in-game, then Refresh. Each row is a map type or rolled content read from memory. Per row toggle ☑ Highlight (ring it in-game), ⇴ Nav (draw a route to it), ➤ Arrow (edge pointer when off-screen) — independent. Click any column header to sort. Hover a tile in-game + press F10 to inspect it.',
  'price.hint':'These apply to everything priced below — ground loot, hover, monolith & ritual rewards. Prices come from poe.ninja for the detected league.',
  'ground.unidHint':'Unidentified uniques reveal their NAME + value; everything else (identified uniques, currency, runes, essences, …) shows the value only.',
  'hover.hint':'Hovering is explicit intent, so this ignores the ground category toggles & value floors — any priced item shows. Stacks show the per-unit price and the stack total.',
  'ce.hint':'When the in-game Currency Exchange is open, a top-right panel lists the best offered/wanted ratios + depth (the best row of each side is highlighted).',
  'saved':'saved to config','update':'Update available','download':'Download'
},
'zh-CN': {
  'tab.rules':'规则','tab.landmarks':'地标','tab.atlas':'异界图鉴','tab.value':'物品价值','tab.settings':'设置',
  'side.life':'生命','side.es':'能量护盾','side.mana':'魔力','side.zone':'区域','side.area':'地图',
  'side.areaCode':'地图代码','side.actLevel':'章节 / 等级','side.mapOpen':'地图已开','side.autoFlask':'自动药剂',
  'side.census':'统计','side.entities':'实体','side.poi':'兴趣点','side.monsters':'怪物',
  'side.landmarks':'地标','side.monolithRewards':'石碑奖励',
  'conn.live':'已连接','conn.offline':'离线','state.ingame':'游戏中','state.town':'城镇/菜单',
  'state.yes':'是','state.no':'否','state.on':'开','state.off':'关',
  'sec.displayRules':'显示规则','sec.displayRulesTag':'一套有序规则 — 首个匹配生效',
  'sec.hidden':'隐藏','sec.hiddenTag':'从雷达、列表与导航中彻底剔除',
  'sec.landmarks':'地标','sec.landmarksTag':'精选地图标注 — 查看、修正、分享',
  'sec.radarDisplay':'雷达显示','sec.hpBars':'怪物血条','sec.hpBarsTag':'按稀有度',
  'sec.terrain':'地形','sec.terrainTag':'可行走叠加层','sec.calibration':'地图校准',
  'sec.autoFlask':'自动药剂','sec.cameraZoom':'镜头缩放','sec.cameraZoomTag':'可选内存补丁',
  'sec.generalPricing':'通用定价','sec.generalPricingTag':'poe.ninja','sec.groundLoot':'地面掉落',
  'sec.onHover':'悬浮显示','sec.monolithRewards':'石碑奖励','sec.monolithRewardsTag':'先祖密藏',
  'sec.currencyExchange':'通货兑换','sec.currencyExchangeTag':'卡尔古尔市场',
  'set.showTerrain':'显示地形','set.showTerrainHint':'可行走地形位图',
  'set.showPlayerBlip':'显示玩家光点','set.showPlayerBlipHint':'标记自身位置的蓝点',
  'set.showMinimap':'小地图雷达','set.showMinimapHint':'自绘的圆形角落地图（游戏地图已透明）',
  'set.minimapCorner':'小地图位置','set.minimapCornerHint':'圆形地图固定的屏幕角落',
  'set.minimapSize':'小地图大小','set.minimapSizeHint':'圆形直径，单位像素（80–600）',
  'set.minimapZoom':'小地图缩放','set.minimapZoomHint':'× 大地图比例（越大越放大）',
  'set.alwaysShow':'始终显示悬浮层','set.alwaysShowHint':'即使 PoE2 不在前台也绘制；自动药剂始终受前台限制',
  'set.hideJunk':'隐藏垃圾实体','set.hideJunkHint':'过滤外观 / 特效 / 守护进程光点',
  'set.navPaths':'导航路径','set.navPathsHint':'绘制到所选地标的 A* 路线',
  'set.showWorldPaths':'世界路径（无地图时）','set.showWorldPathsHint':'大地图关闭时在真实地面上绘制路线（关闭 = 仅显示在小地图）',
  'set.curated':'精选地标名','set.curatedHint':'社区标注（boss / 奖励 / 出口）',
  'set.gh2Landmarks':'GameHelper2 地标','set.gh2LandmarksHint':'终局 boss 竞技场 + 地牢楼梯（GH2 参考集）',
  'set.autoBoss':'自动识别 Boss 房','set.autoBossHint':'将名称含 “boss”/“arena” 的地块标为 Boss 地标',
  'set.gh2Radar':'GH2 雷达（完整）','set.gh2RadarHint':'复刻 GameHelper2 Radar 插件的额外图标识别',
  'set.fpsCap':'悬浮层 FPS 上限','set.fpsCapHint':'越低对游戏负载越小',
  'set.language':'语言','set.languageHint':'雷达界面 + 地图术语（机制 / 兴趣点标签）',
  'hp.on':'开','hp.rarity':'稀有度','hp.width':'宽度','hp.border':'边框','hp.thick':'粗细',
  'hp.normal':'普通','hp.magic':'魔法','hp.rare':'稀有','hp.unique':'传奇',
  'hp.height':'高度','hp.offsetX':'偏移 X','hp.offsetY':'偏移 Y',
  'ter.interior':'内部填充','ter.interiorHint':'可行走格子的淡色覆盖',
  'ter.edge':'墙体边缘','ter.edgeHint':'房间外围描边',
  'cal.scale':'缩放倍率','cal.scaleHint':'地图叠加层的投影缩放',
  'cal.offsetX':'偏移 X','cal.offsetY':'偏移 Y',
  'flask.mode':'生命药剂触发条件','flask.modeHint':'生命药剂按键监控哪个资源池 — 无 ES 时忽略 ES',
  'flask.modeHealth':'生命 %','flask.modeEs':'能量护盾 %','flask.modeEither':'任一（生命或 ES）',
  'flask.lifeThr':'生命阈值 %','flask.lifeThrHint':'生命低于此 % 时使用生命药剂',
  'flask.esThr':'ES 阈值 %','flask.esThrHint':'ES 低于此 % 时使用生命药剂（ES / 任一模式）',
  'flask.manaThr':'魔力阈值 %','flask.manaThrHint':'魔力低于此 % 时使用魔力药剂',
  'flask.lifeKey':'生命药剂按键','flask.manaKey':'魔力药剂按键',
  'flask.lifeCd':'生命药剂冷却','flask.lifeCdHint':'两次生命药剂间最短毫秒',
  'flask.manaCd':'魔力药剂冷却','flask.manaCdHint':'两次魔力药剂间最短毫秒',
  'zoom.enable':'启用拉远镜头','zoom.enableHint':'让镜头比游戏默认更远地拉出',
  'zoom.value':'缩放钳制值','zoom.valueHint':'越高拉得越远（minss 钳制目标）',
  'price.league':'定价赛季','price.leagueHint':'留空以从游戏自动检测赛季（HC/SC/标准）',
  'price.minQty':'低挂单警告','price.minQtyHint':'挂单数少于 N 时用 “?” 标记（可能为错误挂单）',
  'ground.show':'显示地面掉落价值','ground.showHint':'在地图掉落物上绘制价值标签',
  'ground.uniqueMin':'传奇最低','ground.uniqueMinHint':'低于此值的传奇隐藏（Ex）',
  'ground.currencyMin':'通货最低','ground.currencyMinHint':'低于此值的通货隐藏（Ex）',
  'ground.otherMin':'其他最低','ground.otherMinHint':'符文 / 精华 / 碎片 / …（Ex）',
  'ground.highlight':'高亮阈值','ground.highlightHint':'达到或超过此值时描边/强调（Ex）',
  'hover.show':'悬浮显示物品价值','hover.showHint':'游戏提示旁的价格标签',
  'hover.highlight':'高亮阈值','hover.highlightHint':'达到或超过此（堆叠）价值时强调（Ex）',
  'mono.enabled':'启用','mono.enabledHint':'读取 + 定价符文塑形石碑奖励',
  'mono.minValue':'显示 / 自动导航最低值','mono.minValueHint':'低于此值完全隐藏石碑（Ex）。0 = 显示所有石碑',
  'mono.highlight':'高亮阈值','mono.highlightHint':'达到或超过此值的绿色价值层级（Ex）',
  'mono.hideCollected':'隐藏已领取','mono.hideCollectedHint':'隐藏奖励已领取的石碑',
  'mono.showPanel':'显示奖励面板','mono.showPanelHint':'悬浮层中的附近石碑奖励列表',
  'mono.showLabel':'显示地图标签','mono.showLabelHint':'在图标处绘制价值 + 最高奖励',
  'ce.enabled':'启用','ce.enabledHint':'打开兑换时显示订单簿深度面板',
  'ce.maxRows':'最大行数','ce.maxRowsHint':'每侧显示的阶梯行数',
  'cat.Monster':'怪物','cat.Chest':'宝箱','cat.Player':'玩家','cat.Npc':'NPC','cat.Object':'地形',
  'cat.Other':'杂项 / 兴趣点','cat.Transition':'出入口','cat.Tile':'地块',
  'cat.Monsters':'怪物','cat.Chests':'宝箱','cat.Misc':'杂项 / 兴趣点','cat.Terrain':'地形',
  'cat.NPCs':'NPC','cat.Transitions':'出入口',
  'rar.Normal':'普通','rar.Magic':'魔法','rar.Rare':'稀有','rar.Unique':'传奇',
  'sel.rarity':'稀有度','sel.reaction':'敌我关系','sel.life':'生命','sel.chest':'宝箱','sel.poi':'兴趣点','sel.encounter':'遭遇',
  'sel.any':'任意','sel.hostile':'敌对','sel.friendly':'友好','sel.alive':'存活','sel.dead':'死亡',
  'sel.opened':'已开启','sel.unopened':'未开启','sel.yes':'是','sel.no':'否','sel.active':'进行中','sel.complete':'已完成',
  'btn.add':'+ 添加','btn.remove':'移除','btn.delete':'删除','btn.hide':'+ 隐藏','btn.restore':'恢复',
  'btn.import':'导入…','btn.export':'导出','btn.addFromGame':'+ 从游戏数据添加…','btn.addBlank':'+ 添加空白规则',
  'btn.thisArea':'仅此区域','btn.refresh':'刷新','btn.hide':'隐藏','btn.autoPath':'自动寻路',
  'pick.all':'全部','pick.entities':'实体','pick.tiles':'地块','pick.mods':'词缀',
  'pick.filterPh':'按名称 / 元数据 / 地块路径 / 词缀 id 过滤…',
  'pick.loading':'加载中…','pick.noMatches':'无匹配','pick.noMatchesGame':' — 你在游戏中吗？',
  'atlas.statusScan':'扫描中…','atlas.statusClosed':'图鉴已关闭 — 在游戏中打开 + 刷新',
  'atlas.title':'标题','atlas.count':'数量','atlas.category':'分类','atlas.map':'地图','atlas.content':'内容',
  'atlas.biome':'生物群系','atlas.pos':'坐标','atlas.noNodes':'无实时节点（在游戏中打开图鉴，然后刷新）。',
  'gi.uniques':'传奇物品','gi.currency':'通货','gi.runes':'符文','gi.soulcores':'魂核','gi.uncutgems':'未切割宝石',
  'gi.essences':'精华','gi.fragments':'碎片','gi.tablets':'石板','gi.delirium':'迷雾','gi.idols':'神像',
  'gi.abyss':'深渊','gi.ritual':'祭祀','gi.breach':'裂隙','gi.expedition':'先祖密藏',
  'state.act':'章节','state.lvl':'级',
  'atlas.mapColourGroups':'地图颜色分组','atlas.addGroup':'+ 添加分组','atlas.quickSet':'快速设置',
  'atlas.citadels':'城塞','atlas.deadly':'致命 Boss','atlas.bosses':'Boss','atlas.towers':'塔','atlas.uniques':'传奇',
  'atlas.kind':'类型','atlas.type':'种类','atlas.activeOnly':'仅已启用','atlas.clearAll':'全部清除',
  'atlas.hideCompleted':'隐藏已完成','atlas.hideAccessible':'隐藏可进入','atlas.contentIcons':'内容图标',
  'atlas.iconSize':'图标大小','atlas.arrowSpacing':'箭头间距',
  'icon.monsterNormal':'怪物 · 普通','icon.monsterMagic':'怪物 · 魔法','icon.monsterRare':'怪物 · 稀有','icon.monsterUnique':'怪物 · 传奇',
  'icon.chestRare':'宝箱 · 稀有','icon.chestUnique':'宝箱 · 传奇','icon.poiLabel':'兴趣点','icon.landmarkLabel':'地标',
  'rules.noRules':'还没有显示规则，在下方添加。','rules.newRule':'新规则','rules.unnamed':'(未命名)',
  'rules.ruleNamePh':'规则名称','rules.matchPh':'匹配：元数据词条，逗号分隔（留空 = 任意）',
  'rules.modsPh':'怪物词缀：光环/buff 词条，逗号分隔（如 Aura、ManaSiphon）— 留空 = 任意',
  'rules.labelPh':'标签（可选）','rules.anyType':'任意类型','rules.modsLabel':'词缀：',
  'rules.hidePh':'要隐藏的模式或通配符（如 AbyssCrack、*Daemon*）','rules.nothingHidden':'未隐藏任何内容。',
  'lm.searchPh':'按区域 / 地块 / 标签过滤…','lm.areaPh':'区域（如 P2_3 或 *）','lm.patPh':'地块路径 / 模式','lm.labelPh':'标签',
  'lm.noLandmarks':'没有精选地标','lm.forArea':' 于该区域（','lm.addOne':'在下方添加','lm.orOff':' 或关闭“仅此区域”',
  'atlas.emptyTable':'在游戏中打开图鉴 + 刷新以列出过滤器。',
  'atlas.noGroups':'暂无分组。分组内的地图在被追踪时以该组颜色绘制。',
  'atlas.groupNamePh':'分组名称','atlas.mapsPh':'每行一个地图名','atlas.newGroup':'新分组',
  'atlas.colourHint':'一次性为整个分类着色（城塞、殿堂、传奇…）',
  'atlas.noActive':'无已启用规则 — 点击某行或快速设置。',
  'atlas.noFilters':'暂无过滤器（打开图鉴 + 刷新）。','atlas.reading':'读取中…',
  'atlas.nodes':'节点','atlas.withContent':'含内容','atlas.kinds':'类型','atlas.contents':'内容','atlas.mapFilters':'地图过滤器',
  'hp.hint1':'用“开”复选框按稀有度开关血条 — 全部取消可完全禁用血条，或只保留想要的稀有度。',
  'hp.hint2':'血条填充跟随怪物图标颜色；按稀有度设置边框颜色与粗细（粗细 0 = 无边框）。偏移 Y 为负 = 在怪物上方。',
  'ter.hint':'编辑会重建地形位图；用上方的“显示地形”可完全隐藏。',
  'cal.hint':'在此调整 — 修改立即生效（无需游戏内热键）。',
  'flask.hint':'F8 在游戏中切换自动药剂。状态：',
  'zoom.warn':'会写入游戏进程 — 绕过 POE2Radar 的外部只读边界，可能违反 PoE2 服务条款。默认关闭；风险自负。',
  'price.statusPh':'定价状态：','ground.catHint':'为这些分类显示标签：',
  'ground.bucketHint':'每个桶（Ex）显示的最低价值 — 低于下限的掉落被隐藏：',
  'state.itemsLoaded':'已加载物品','state.loading':'加载中…','state.auto':'(自动)','state.statusPh':'状态：',
  'mono.collected':'已领取','mono.noRewards':'无已定价奖励',
  'rules.drHint':'每个实体如何绘制的唯一依据。实体从上到下匹配；首个匹配的启用规则决定一切 — 图标与颜色、是否隐藏、是否显示血条、是否自动寻路。用 ▲/▼ 调整优先级。规则可匹配类型、元数据词条、怪物词缀（光环/buff）、稀有度、敌我关系、生命、宝箱/兴趣点/遭遇状态；留空条件表示“任意”。',
  'rules.hiddenHint':'比“隐藏”规则更强的剔除：元数据包含某模式（或匹配 */? 通配符）的实体会在所有地方被移除 — 悬浮层、实体列表与导航 — 甚至在显示规则运行之前。',
  'lm.hint':'内置“已知”地图要素（boss 竞技场、出口、战利品、传送点…），按区域标注。重命名错误的标签、添加自己的、或隐藏坏条目。导出修正后的列表以分享或提交纳入发布；导入以加载。（地块如何绘制 — 图标/颜色/隐藏 — 请在规则页用“地块”规则；这里只是标签。）',
  'atlas.help':'在游戏中打开图鉴，然后刷新。每行是一个地图类型或从内存读取的骰入内容。每行可独立切换 ☑ 高亮（游戏中圈出）、⇴ 导航（绘制路线）、➤ 箭头（屏幕外时边缘指针）。点击任意列头排序。在游戏中悬停地块并按 F10 检视。',
  'price.hint':'这些适用于下方所有定价内容 — 地面掉落、悬浮、石碑与祭祀奖励。价格来自 poe.ninja（按检测到的赛季）。',
  'ground.unidHint':'未鉴定传奇显示其名称 + 价值；其余（已鉴定传奇、通货、符文、精华、…）只显示价值。',
  'hover.hint':'悬浮是明确意图，因此忽略地面分类开关与价值下限 — 任何已定价物品都会显示。堆叠显示单价与堆叠总价。',
  'ce.hint':'当游戏内通货兑换打开时，右上角面板会列出最佳卖/买比价 + 深度（每侧最佳行高亮）。',
  'saved':'已保存到配置','update':'有可用更新','download':'下载'
},
'zh-Hant': {
  'tab.rules':'規則','tab.landmarks':'地標','tab.atlas':'異界圖鑑','tab.value':'物品價值','tab.settings':'設定',
  'side.life':'生命','side.es':'能量護盾','side.mana':'魔力','side.zone':'區域','side.area':'地圖',
  'side.areaCode':'地圖代碼','side.actLevel':'章節 / 等級','side.mapOpen':'地圖已開','side.autoFlask':'自動藥劑',
  'side.census':'統計','side.entities':'實體','side.poi':'興趣點','side.monsters':'怪物',
  'side.landmarks':'地標','side.monolithRewards':'石碑獎勵',
  'conn.live':'已連線','conn.offline':'離線','state.ingame':'遊戲中','state.town':'城鎮/選單',
  'state.yes':'是','state.no':'否','state.on':'開','state.off':'關',
  'sec.displayRules':'顯示規則','sec.displayRulesTag':'一套有序規則 — 首個匹配生效',
  'sec.hidden':'隱藏','sec.hiddenTag':'從雷達、列表與導航中徹底剔除',
  'sec.landmarks':'地標','sec.landmarksTag':'精選地圖標註 — 查看、修正、分享',
  'sec.radarDisplay':'雷達顯示','sec.hpBars':'怪物血條','sec.hpBarsTag':'按稀有度',
  'sec.terrain':'地形','sec.terrainTag':'可行走疊加層','sec.calibration':'地圖校準',
  'sec.autoFlask':'自動藥劑','sec.cameraZoom':'鏡頭縮放','sec.cameraZoomTag':'可選記憶體補丁',
  'sec.generalPricing':'通用定價','sec.generalPricingTag':'poe.ninja','sec.groundLoot':'地面掉落',
  'sec.onHover':'懸浮顯示','sec.monolithRewards':'石碑獎勵','sec.monolithRewardsTag':'先祖密藏',
  'sec.currencyExchange':'通貨兌換','sec.currencyExchangeTag':'卡爾古爾市場',
  'set.showTerrain':'顯示地形','set.showTerrainHint':'可行走地形點陣圖',
  'set.showPlayerBlip':'顯示玩家光點','set.showPlayerBlipHint':'標記自身位置的藍點',
  'set.showMinimap':'小地圖雷達','set.showMinimapHint':'自繪的圓形角落地圖（遊戲地圖已透明）',
  'set.minimapCorner':'小地圖位置','set.minimapCornerHint':'圓形地圖固定的螢幕角落',
  'set.minimapSize':'小地圖大小','set.minimapSizeHint':'圓形直徑，單位像素（80–600）',
  'set.minimapZoom':'小地圖縮放','set.minimapZoomHint':'× 大地圖比例（越大越放大）',
  'set.alwaysShow':'始終顯示疊加層','set.alwaysShowHint':'即使 PoE2 不在前景也繪製；自動藥劑始終受前景限制',
  'set.hideJunk':'隱藏垃圾實體','set.hideJunkHint':'過濾外觀 / 特效 / 守護進程光點',
  'set.navPaths':'導航路徑','set.navPathsHint':'繪製到所選地標的 A* 路線',
  'set.showWorldPaths':'世界路徑（無地圖時）','set.showWorldPathsHint':'大地圖關閉時在真實地面上繪製路線（關閉 = 僅顯示在小地圖）',
  'set.curated':'精選地標名','set.curatedHint':'社群標註（boss / 獎勵 / 出口）',
  'set.gh2Landmarks':'GameHelper2 地標','set.gh2LandmarksHint':'終局 boss 競技場 + 地牢樓梯（GH2 參考集）',
  'set.autoBoss':'自動識別 Boss 房','set.autoBossHint':'將名稱含 “boss”/“arena” 的地塊標為 Boss 地標',
  'set.gh2Radar':'GH2 雷達（完整）','set.gh2RadarHint':'複刻 GameHelper2 Radar 外掛的額外圖示識別',
  'set.fpsCap':'疊加層 FPS 上限','set.fpsCapHint':'越低對遊戲負載越小',
  'set.language':'語言','set.languageHint':'雷達介面 + 地圖術語（機制 / 興趣點標籤）',
  'hp.on':'開','hp.rarity':'稀有度','hp.width':'寬度','hp.border':'邊框','hp.thick':'粗細',
  'hp.normal':'普通','hp.magic':'魔法','hp.rare':'稀有','hp.unique':'傳奇',
  'hp.height':'高度','hp.offsetX':'偏移 X','hp.offsetY':'偏移 Y',
  'ter.interior':'內部填充','ter.interiorHint':'可行走格子的淡色覆蓋',
  'ter.edge':'牆體邊緣','ter.edgeHint':'房間外圍描邊',
  'cal.scale':'縮放倍率','cal.scaleHint':'地圖疊加層的投影縮放',
  'cal.offsetX':'偏移 X','cal.offsetY':'偏移 Y',
  'flask.mode':'生命藥劑觸發條件','flask.modeHint':'生命藥劑按鍵監控哪個資源池 — 無 ES 時忽略 ES',
  'flask.modeHealth':'生命 %','flask.modeEs':'能量護盾 %','flask.modeEither':'任一（生命或 ES）',
  'flask.lifeThr':'生命閾值 %','flask.lifeThrHint':'生命低於此 % 時使用生命藥劑',
  'flask.esThr':'ES 閾值 %','flask.esThrHint':'ES 低於此 % 時使用生命藥劑（ES / 任一模式）',
  'flask.manaThr':'魔力閾值 %','flask.manaThrHint':'魔力低於此 % 時使用魔力藥劑',
  'flask.lifeKey':'生命藥劑按鍵','flask.manaKey':'魔力藥劑按鍵',
  'flask.lifeCd':'生命藥劑冷卻','flask.lifeCdHint':'兩次生命藥劑間最短毫秒',
  'flask.manaCd':'魔力藥劑冷卻','flask.manaCdHint':'兩次魔力藥劑間最短毫秒',
  'zoom.enable':'啟用拉遠鏡頭','zoom.enableHint':'讓鏡頭比遊戲預設更遠地拉出',
  'zoom.value':'縮放鉗制值','zoom.valueHint':'越高拉得越遠（minss 鉗制目標）',
  'price.league':'定價聯盟','price.leagueHint':'留空以從遊戲自動偵測聯盟（HC/SC/標準）',
  'price.minQty':'低掛單警告','price.minQtyHint':'掛單數少於 N 時用 “?” 標記（可能為錯誤掛單）',
  'ground.show':'顯示地面掉落價值','ground.showHint':'在地圖掉落物上繪製價值標籤',
  'ground.uniqueMin':'傳奇最低','ground.uniqueMinHint':'低於此值的傳奇隱藏（Ex）',
  'ground.currencyMin':'通貨最低','ground.currencyMinHint':'低於此值的通貨隱藏（Ex）',
  'ground.otherMin':'其他最低','ground.otherMinHint':'符文 / 精華 / 碎片 / …（Ex）',
  'ground.highlight':'高亮閾值','ground.highlightHint':'達到或超過此值時描邊/強調（Ex）',
  'hover.show':'懸浮顯示物品價值','hover.showHint':'遊戲提示旁的價格標籤',
  'hover.highlight':'高亮閾值','hover.highlightHint':'達到或超過此（堆疊）價值時強調（Ex）',
  'mono.enabled':'啟用','mono.enabledHint':'讀取 + 定價符文塑形石碑獎勵',
  'mono.minValue':'顯示 / 自動導航最低值','mono.minValueHint':'低於此值完全隱藏石碑（Ex）。0 = 顯示所有石碑',
  'mono.highlight':'高亮閾值','mono.highlightHint':'達到或超過此值的綠色價值層級（Ex）',
  'mono.hideCollected':'隱藏已領取','mono.hideCollectedHint':'隱藏獎勵已領取的石碑',
  'mono.showPanel':'顯示獎勵面板','mono.showPanelHint':'疊加層中的附近石碑獎勵列表',
  'mono.showLabel':'顯示地圖標籤','mono.showLabelHint':'在圖示處繪製價值 + 最高獎勵',
  'ce.enabled':'啟用','ce.enabledHint':'開啟兌換時顯示訂單簿深度面板',
  'ce.maxRows':'最大行數','ce.maxRowsHint':'每側顯示的階梯行數',
  'cat.Monster':'怪物','cat.Chest':'寶箱','cat.Player':'玩家','cat.Npc':'NPC','cat.Object':'地形',
  'cat.Other':'雜項 / 興趣點','cat.Transition':'出入口','cat.Tile':'地塊',
  'cat.Monsters':'怪物','cat.Chests':'寶箱','cat.Misc':'雜項 / 興趣點','cat.Terrain':'地形',
  'cat.NPCs':'NPC','cat.Transitions':'出入口',
  'rar.Normal':'普通','rar.Magic':'魔法','rar.Rare':'稀有','rar.Unique':'傳奇',
  'sel.rarity':'稀有度','sel.reaction':'敵我關係','sel.life':'生命','sel.chest':'寶箱','sel.poi':'興趣點','sel.encounter':'遭遇',
  'sel.any':'任意','sel.hostile':'敵對','sel.friendly':'友好','sel.alive':'存活','sel.dead':'死亡',
  'sel.opened':'已開啟','sel.unopened':'未開啟','sel.yes':'是','sel.no':'否','sel.active':'進行中','sel.complete':'已完成',
  'btn.add':'+ 新增','btn.remove':'移除','btn.delete':'刪除','btn.hide':'+ 隱藏','btn.restore':'還原',
  'btn.import':'匯入…','btn.export':'匯出','btn.addFromGame':'+ 從遊戲資料新增…','btn.addBlank':'+ 新增空白規則',
  'btn.thisArea':'僅此區域','btn.refresh':'重新整理','btn.hide':'隱藏','btn.autoPath':'自動尋路',
  'pick.all':'全部','pick.entities':'實體','pick.tiles':'地塊','pick.mods':'詞綴',
  'pick.filterPh':'按名稱 / 中繼資料 / 地塊路徑 / 詞綴 id 過濾…',
  'pick.loading':'載入中…','pick.noMatches':'無相符','pick.noMatchesGame':' — 你在遊戲中嗎？',
  'atlas.statusScan':'掃描中…','atlas.statusClosed':'圖鑑已關閉 — 在遊戲中開啟 + 重新整理',
  'atlas.title':'標題','atlas.count':'數量','atlas.category':'分類','atlas.map':'地圖','atlas.content':'內容',
  'atlas.biome':'生物群系','atlas.pos':'座標','atlas.noNodes':'無即時節點（在遊戲中開啟圖鑑，然後重新整理）。',
  'gi.uniques':'傳奇物品','gi.currency':'通貨','gi.runes':'符文','gi.soulcores':'魂核','gi.uncutgems':'未切割寶石',
  'gi.essences':'精華','gi.fragments':'碎片','gi.tablets':'石板','gi.delirium':'迷霧','gi.idols':'神像',
  'gi.abyss':'深淵','gi.ritual':'祭祀','gi.breach':'裂隙','gi.expedition':'先祖密藏',
  'state.act':'章節','state.lvl':'級',
  'atlas.mapColourGroups':'地圖顏色分組','atlas.addGroup':'+ 新增分組','atlas.quickSet':'快速設定',
  'atlas.citadels':'城塞','atlas.deadly':'致命 Boss','atlas.bosses':'Boss','atlas.towers':'塔','atlas.uniques':'傳奇',
  'atlas.kind':'類型','atlas.type':'種類','atlas.activeOnly':'僅已啟用','atlas.clearAll':'全部清除',
  'atlas.hideCompleted':'隱藏已完成','atlas.hideAccessible':'隱藏可進入','atlas.contentIcons':'內容圖示',
  'atlas.iconSize':'圖示大小','atlas.arrowSpacing':'箭頭間距',
  'icon.monsterNormal':'怪物 · 普通','icon.monsterMagic':'怪物 · 魔法','icon.monsterRare':'怪物 · 稀有','icon.monsterUnique':'怪物 · 傳奇',
  'icon.chestRare':'寶箱 · 稀有','icon.chestUnique':'寶箱 · 傳奇','icon.poiLabel':'興趣點','icon.landmarkLabel':'地標',
  'rules.noRules':'還沒有顯示規則，在下方新增。','rules.newRule':'新規則','rules.unnamed':'(未命名)',
  'rules.ruleNamePh':'規則名稱','rules.matchPh':'符合：中繼資料詞條，逗號分隔（留空 = 任意）',
  'rules.modsPh':'怪物詞綴：光環/buff 詞條，逗號分隔（如 Aura、ManaSiphon）— 留空 = 任意',
  'rules.labelPh':'標籤（可選）','rules.anyType':'任意類型','rules.modsLabel':'詞綴：',
  'rules.hidePh':'要隱藏的模式或萬用字元（如 AbyssCrack、*Daemon*）','rules.nothingHidden':'未隱藏任何內容。',
  'lm.searchPh':'按區域 / 地塊 / 標籤過濾…','lm.areaPh':'區域（如 P2_3 或 *）','lm.patPh':'地塊路徑 / 模式','lm.labelPh':'標籤',
  'lm.noLandmarks':'沒有精選地標','lm.forArea':' 於該區域（','lm.addOne':'在下方新增','lm.orOff':' 或關閉「僅此區域」',
  'atlas.emptyTable':'在遊戲中開啟圖鑑 + 重新整理以列出過濾器。',
  'atlas.noGroups':'尚無分組。分組內的地圖在被追蹤時以該組顏色繪製。',
  'atlas.groupNamePh':'分組名稱','atlas.mapsPh':'每行一個地圖名','atlas.newGroup':'新分組',
  'atlas.colourHint':'一次為整個分類著色（城塞、殿堂、傳奇…）',
  'atlas.noActive':'無已啟用規則 — 點擊某行或快速設定。',
  'atlas.noFilters':'尚無過濾器（開啟圖鑑 + 重新整理）。','atlas.reading':'讀取中…',
  'atlas.nodes':'節點','atlas.withContent':'含內容','atlas.kinds':'類型','atlas.contents':'內容','atlas.mapFilters':'地圖過濾器',
  'hp.hint1':'用「開」核取方塊按稀有度開關血條 — 全部取消可完全停用血條，或只保留想要的稀有度。',
  'hp.hint2':'血條填滿跟隨怪物圖示顏色；按稀有度設定邊框顏色與粗細（粗細 0 = 無邊框）。偏移 Y 為負 = 在怪物上方。',
  'ter.hint':'編輯會重建地形點陣圖；用上方的「顯示地形」可完全隱藏。',
  'cal.hint':'在此調整 — 修改立即生效（無需遊戲內熱鍵）。',
  'flask.hint':'F8 在遊戲中切換自動藥劑。狀態：',
  'zoom.warn':'會寫入遊戲程序 — 繞過 POE2Radar 的外部唯讀邊界，可能違反 PoE2 服務條款。預設關閉；風險自負。',
  'price.statusPh':'定價狀態：','ground.catHint':'為這些分類顯示標籤：',
  'ground.bucketHint':'每個桶（Ex）顯示的最低價值 — 低於下限的掉落被隱藏：',
  'state.itemsLoaded':'已載入物品','state.loading':'載入中…','state.auto':'(自動)','state.statusPh':'狀態：',
  'mono.collected':'已領取','mono.noRewards':'無已定價獎勵',
  'rules.drHint':'每個實體如何繪製的唯一依據。實體從上到下符合；首個符合的啟用規則決定一切 — 圖示與顏色、是否隱藏、是否顯示血條、是否自動尋路。用 ▲/▼ 調整優先順序。規則可符合類型、中繼資料詞條、怪物詞綴（光環/buff）、稀有度、敵我關係、生命、寶箱/興趣點/遭遇狀態；留空條件表示「任意」。',
  'rules.hiddenHint':'比「隱藏」規則更強的剔除：中繼資料包含某模式（或符合 */? 萬用字元）的實體會在所有地方被移除 — 疊加層、實體列表與導航 — 甚至在顯示規則執行之前。',
  'lm.hint':'內建「已知」地圖要素（boss 競技場、出口、戰利品、傳送點…），按區域標註。重新命名錯誤的標籤、新增自己的、或隱藏壞條目。匯出修正後的列表以分享或提交納入發布；匯入以載入。（地塊如何繪製 — 圖示/顏色/隱藏 — 請在規則頁用「地塊」規則；這裡只是標籤。）',
  'atlas.help':'在遊戲中開啟圖鑑，然後重新整理。每行是一個地圖類型或從記憶體讀取的骰入內容。每行可獨立切換 ☑ 高亮（遊戲中圈出）、⇴ 導航（繪製路線）、➤ 箭頭（螢幕外時邊緣指標）。點擊任意列頭排序。在遊戲中懸停地塊並按 F10 檢視。',
  'price.hint':'這些適用於下方所有定價內容 — 地面掉落、懸浮、石碑與祭祀獎勵。價格來自 poe.ninja（按偵測到的聯盟）。',
  'ground.unidHint':'未鑑定傳奇顯示其名稱 + 價值；其餘（已鑑定傳奇、通貨、符文、精華、…）只顯示價值。',
  'hover.hint':'懸浮是明確意圖，因此忽略地面分類開關與價值下限 — 任何已定價物品都會顯示。堆疊顯示單價與堆疊總價。',
  'ce.hint':'當遊戲內通貨兌換開啟時，右上角面板會列出最佳賣/買比價 + 深度（每側最佳行高亮）。',
  'saved':'已儲存到設定','update':'有可用更新','download':'下載'
}
};
let curLang='en';
const t=k=>(I18N[curLang]&&I18N[curLang][k])||I18N['en'][k]||k;
function applyLang(lang){
  curLang=(lang==='zh-CN'||lang==='zh-Hant')?lang:'en';
  document.documentElement.lang=curLang;
  $$('[data-i18n]').forEach(el=>{ const v=t(el.dataset.i18n); if(v!==undefined) el.textContent=v; });
  $$('[data-i18n-ph]').forEach(el=>{ const v=t(el.dataset.i18nPh); if(v!==undefined) el.placeholder=v; });
  renderState();
  renderDrules(); renderMechanics(); renderIcons(); renderLandmarks();
  if(atlasData) renderAtlas();
}

/* ── tabs ── */
$$('.tab').forEach(t=>t.onclick=()=>{
  activeTab=t.dataset.tab;
  $$('.tab').forEach(x=>x.classList.toggle('on',x===t));
  $$('.view').forEach(v=>v.hidden = v.dataset.view!==activeTab);
  if(activeTab==='settings') loadSettings();
  if(activeTab==='value'){ loadSettings(); pollPrices(); }
  if(activeTab==='filters') loadFilters();
  if(activeTab==='landmarks') loadLandmarks();
  if(activeTab==='atlas'){ if(!atlasData) loadAtlas(); else renderAtlas(); }
});

/* ── polling (left rail vitals/zone/census) ── */
async function getJSON(u){ const r=await fetch(u,{cache:'no-store'}); if(!r.ok) throw 0; return r.json(); }
function setConn(live){ $('#conn').classList.toggle('live',live); $('#connTxt').textContent = t(live?'conn.live':'conn.offline'); }

async function tick(){
  try{
    state = await getJSON('/state');
    setConn(true);
    try{ zone = await getJSON('/api/zone'); }catch(e){ zone=null; }
    renderState();
    if(activeTab==='value') pollPrices();   // keep the league/status live (prices load a few s after launch)
    if(activeTab==='settings') refreshZoomStatus();   // keep the camera-zoom patch status live
  }catch(e){ setConn(false); }
}

/* ── settings tab (writes radar/visual + flask via the loopback-gated /api/settings) ── */
async function loadSettings(){
  try{
    const s = await getJSON('/api/settings');
    $$('[data-set]').forEach(el=>{
      const k=el.dataset.set;
      if(el.type==='checkbox') el.checked=!!s[k];
      else if(el.classList.contains('keyin')) el.value=vkToChar(s[k]);
      else if(s[k]!==undefined) el.value=s[k];
    });
    hpBars = s.hpBars || null;
    terrain = s.terrain || null;
    gi = s.groundItems || {};
    hover = s.hoverPrice || {};
    mono = s.monoliths || {};
    ce = s.currencyExchange || {};
    zoomCfg = s.zoom || null;
    renderHpBars(); renderTerrain(); renderGround(); renderHover(); renderMono(); renderExchange();
    renderZoom(); renderZoomStatus(s.zoomStatus, !!(s.zoom && s.zoom.enabled));
    applyLang(s.language);
  }catch(e){}
}

/* ── ground-item pricing (nested object: POST the whole {groundItems}) ── */
let gi = null;
function renderGround(){
  if(!gi) return;
  $$('[data-gi]').forEach(el=>{
    const k=el.dataset.gi;
    if(el.type==='checkbox') el.checked=!!gi[k];
    else if(gi[k]!==undefined && gi[k]!==null) el.value=gi[k];
  });
  const cats=new Set((gi.categories||[]).map(c=>(c||'').toLowerCase()));
  $$('#giCats .chip').forEach(c=>c.classList.toggle('on', cats.has(c.dataset.gicat.toLowerCase())));
}
function saveGround(){ if(gi) saveSetting('groundItems', gi); }
function wireGround(){
  $$('[data-gi]').forEach(el=>{
    const k=el.dataset.gi;
    if(el.type==='checkbox') el.onchange=()=>{ gi=gi||{}; gi[k]=el.checked; saveGround(); };
    else if(el.type==='text') el.onchange=()=>{ gi=gi||{}; gi[k]=el.value.trim(); saveGround(); };
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)){ gi=gi||{}; gi[k]=v; saveGround(); } };
  });
  $$('#giCats .chip').forEach(c=>c.onclick=()=>{
    c.classList.toggle('on');
    gi=gi||{};
    gi.categories=$$('#giCats .chip.on').map(x=>x.dataset.gicat);
    saveGround();
  });
}
/* ── hover price chip (nested object: POST the whole {hoverPrice}) ── */
let hover = null;
function renderHover(){
  if(!hover) return;
  $$('[data-hv]').forEach(el=>{
    const k=el.dataset.hv;
    if(el.type==='checkbox') el.checked=!!hover[k];
    else if(hover[k]!==undefined && hover[k]!==null) el.value=hover[k];
  });
}
function saveHover(){ if(hover) saveSetting('hoverPrice', hover); }
function wireHover(){
  $$('[data-hv]').forEach(el=>{
    const k=el.dataset.hv;
    if(el.type==='checkbox') el.onchange=()=>{ hover=hover||{}; hover[k]=el.checked; saveHover(); };
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)){ hover=hover||{}; hover[k]=v; saveHover(); } };
  });
}
/* ── live pricing status: shows the resolved league + load state, and uses the detected league as the
      placeholder in the (blank = auto-detect) league field so the user can see what auto-detect picked. ── */
async function pollPrices(){
  try{
    const p = await getJSON('/api/prices');
    const st = $('#priceStatus'); const lg = $('#giLeague');
    if(lg && p.league) lg.placeholder = p.league + ' '+t('state.auto');
    if(st){
      st.textContent = p.loaded
        ? `${p.league||'?'} — ${p.count||0} ${t('state.itemsLoaded')}`
        : (p.status||t('state.loading'));
      st.style.color = p.loaded ? 'var(--good, #3ddc97)' : 'var(--ink-dim)';
    }
  }catch(e){}
}
/* ── monolith (expedition) rewards (nested object: POST the whole {monoliths}) ── */
let mono = null;
function renderMono(){
  if(!mono) return;
  $$('[data-mono]').forEach(el=>{
    const k=el.dataset.mono;
    if(el.type==='checkbox') el.checked=!!mono[k];
    else if(mono[k]!==undefined && mono[k]!==null) el.value=mono[k];
  });
}
function saveMono(){ if(mono) saveSetting('monoliths', mono); }
function wireMono(){
  $$('[data-mono]').forEach(el=>{
    const k=el.dataset.mono;
    if(el.type==='checkbox') el.onchange=()=>{ mono=mono||{}; mono[k]=el.checked; saveMono(); };
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)){ mono=mono||{}; mono[k]=v; saveMono(); } };
  });
}
/* ── currency exchange depth panel (nested object: POST the whole {currencyExchange}) ── */
let ce = null;
function renderExchange(){
  if(!ce) return;
  $$('[data-ce]').forEach(el=>{
    const k=el.dataset.ce;
    if(el.type==='checkbox') el.checked=!!ce[k];
    else if(ce[k]!==undefined && ce[k]!==null) el.value=ce[k];
  });
}
function saveExchange(){ if(ce) saveSetting('currencyExchange', ce); }
function wireExchange(){
  $$('[data-ce]').forEach(el=>{
    const k=el.dataset.ce;
    if(el.type==='checkbox') el.onchange=()=>{ ce=ce||{}; ce[k]=el.checked; saveExchange(); };
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)){ ce=ce||{}; ce[k]=v; saveExchange(); } };
  });
}
/* ── camera zoom (nested object: POST {zoom}; applies the opt-in memory patch) ── */
let zoomCfg = null;
function renderZoom(){
  if(!zoomCfg) return;
  $$('[data-zoom]').forEach(el=>{
    const k=el.dataset.zoom;
    if(el.type==='checkbox') el.checked=!!zoomCfg[k];
    else if(zoomCfg[k]!==undefined && zoomCfg[k]!==null) el.value=zoomCfg[k];
  });
}
function renderZoomStatus(zs, enabled){
  const st=$('#zoomState'); if(!st) return;
  if(zs && zs.applied){ st.textContent=zs.note||'applied'; st.style.color='var(--good,#3ddc97)'; }
  else if(enabled){ st.textContent=(zs&&zs.note)||'applying…'; st.style.color='var(--blood-bright)'; }
  else { st.textContent='off'; st.style.color='var(--ink-dim)'; }
}
function saveZoom(){ if(zoomCfg) saveSetting('zoom', zoomCfg); }
function wireZoom(){
  $$('[data-zoom]').forEach(el=>{
    const k=el.dataset.zoom;
    if(el.type==='checkbox') el.onchange=()=>{ zoomCfg=zoomCfg||{}; zoomCfg[k]=el.checked; saveZoom(); };
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)){ zoomCfg=zoomCfg||{}; zoomCfg[k]=v; saveZoom(); } };
  });
}
async function refreshZoomStatus(){
  try{
    const s = await getJSON('/api/settings');
    renderZoomStatus(s.zoomStatus, !!(s.zoom && s.zoom.enabled));
  }catch(e){}
}
async function saveSetting(key,val){
  try{
    await fetch('/api/settings',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({[key]:val})});
    // Flash every on-page "saved" indicator (one per tab) — only the visible tab's is seen.
    $$('.saved').forEach(m=>{ m.classList.add('show'); clearTimeout(m._t); m._t=setTimeout(()=>m.classList.remove('show'),1100); });
  }catch(e){}
}
function wireSettings(){
  $$('[data-set]').forEach(el=>{
    const k=el.dataset.set;
    if(el.type==='checkbox') el.onchange=()=>saveSetting(k,el.checked);
    else if(el.classList.contains('keyin')) el.onchange=()=>{ const vk=charToVk(el.value); if(vk) saveSetting(k,vk); el.value=vkToChar(vk); };
    else if(el.tagName==='SELECT') el.onchange=()=>{ saveSetting(k,el.value); if(k==='language') applyLang(el.value); }; // string value (e.g. flask mode)
    else el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)) saveSetting(k,v); };
  });
}
// Flask key inputs accept a single character ('1'-'9', letters) → Win32 VK (== ASCII of uppercase).
const charToVk = s => { const c=(s||'').trim().toUpperCase().charCodeAt(0); return isNaN(c)?0:c; };
const vkToChar = v => v ? String.fromCharCode(v) : '';

/* ── icon / HP-bar / mechanics editors (nested objects: POST the whole {styles}/{hpBars}) ── */
let styles=null, hpBars=null, terrain=null;
const ICON_KEYS=[
  ['monsterNormal','Monster · Normal'],['monsterMagic','Monster · Magic'],
  ['monsterRare','Monster · Rare'],['monsterUnique','Monster · Unique'],
  ['player','Player'],['npc','NPC'],['chestRare','Chest · Rare'],
  ['chestUnique','Chest · Unique'],['transition','Transition'],
  ['poi','Point of Interest'],['landmark','Landmark']];
const esc=s=>(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
const pct=o=>Math.round((o==null?1:o)*100);

/* ── SVG icon library (served by /api/icons): drives both the in-page previews and the picker grid. ── */
let ICONS=[]; const ICONMAP={};
async function loadIcons(){
  try{ ICONS=await getJSON('/api/icons')||[]; }catch(e){ ICONS=[]; }
  for(const k in ICONMAP) delete ICONMAP[k];
  ICONS.forEach(d=>ICONMAP[(d.name||'').toLowerCase()]=d);
}
const iconDef=name=>ICONMAP[(name||'').toLowerCase()]||null;
function iconSvg(name,color){
  const d=iconDef(name); if(!d) return '';
  const c=color||'currentColor';
  return `<svg viewBox="${d.viewBox}" preserveAspectRatio="xMidYMid meet">`
    + (d.paths||[]).map(p=>`<path d="${esc(p)}" fill="${c}"/>`).join('') + `</svg>`;
}
function pickerHtml(name,color){
  const d=iconDef(name), nm=d?d.name:(name||'Circle');
  return `<span class="iconpick" data-val="${esc(nm)}"><span class="ipreview" style="color:${color||'var(--ink)'}">`
    + iconSvg(nm,color) + `</span><span class="ipname">${esc(nm)}</span><span class="ipcar">▼</span></span>`;
}
function refreshPicker(pk,name,color){
  const d=iconDef(name), nm=d?d.name:(name||'Circle');
  pk.dataset.val=nm;
  const pv=pk.querySelector('.ipreview'); pv.style.color=color||'var(--ink)'; pv.innerHTML=iconSvg(nm,color);
  pk.querySelector('.ipname').textContent=nm;
}
let _iconPop=null;
function ensureIconPop(){
  if(_iconPop) return _iconPop;
  _iconPop=document.createElement('div'); _iconPop.id='iconPop'; document.body.appendChild(_iconPop);
  document.addEventListener('mousedown',e=>{
    if(_iconPop.classList.contains('open') && !_iconPop.contains(e.target) && !e.target.closest('.iconpick')) _iconPop.classList.remove('open');
  });
  return _iconPop;
}
function openIconPicker(anchor,current,cb){
  const pop=ensureIconPop();
  pop.innerHTML='<div class="ipop-grid">'+ICONS.map(d=>
    `<div class="ipop-cell${d.name.toLowerCase()===(current||'').toLowerCase()?' sel':''}" data-n="${esc(d.name)}" title="${esc(d.name)}">`
    + iconSvg(d.name) + `<span class="cn">${esc(d.name)}</span></div>`).join('')+'</div>';
  pop.querySelectorAll('.ipop-cell').forEach(c=>c.onclick=()=>{ pop.classList.remove('open'); cb(c.dataset.n); });
  pop.classList.add('open');
  const r=anchor.getBoundingClientRect(), pw=pop.offsetWidth, ph=pop.offsetHeight;
  let left=Math.min(r.left, innerWidth-8-pw), top=r.bottom+4;
  if(top+ph>innerHeight-8) top=Math.max(8, r.top-4-ph);
  pop.style.left=Math.max(8,left)+'px'; pop.style.top=top+'px';
}
const saveStyles=()=>{ if(styles) saveSetting('styles',styles); };
const saveHpBars=()=>{ if(hpBars) saveSetting('hpBars',hpBars); };

function renderHpBars(){
  if(!hpBars) return;
  $$('[data-hp]').forEach(el=>{ if(hpBars[el.dataset.hp]!==undefined) el.value=hpBars[el.dataset.hp]; });
  $$('[data-hpcolor]').forEach(el=>{ el.value=hpBars[el.dataset.hpcolor]||'#ffffff'; });
}
function wireHpBars(){
  $$('[data-hp]').forEach(el=>{ el.onchange=()=>{ const v=parseFloat(el.value); if(!isNaN(v)&&hpBars){ hpBars[el.dataset.hp]=v; saveHpBars(); } }; });
  $$('[data-hpcolor]').forEach(el=>{ el.onchange=()=>{ if(hpBars){ hpBars[el.dataset.hpcolor]=el.value; saveHpBars(); } }; });
}

/* ── terrain color/transparency (POSTs the whole {terrain} object; rebuilds the terrain bitmap) ── */
const saveTerrain=()=>{ if(terrain) saveSetting('terrain',terrain); };
function renderTerrain(){
  if(!terrain) return;
  $$('[data-tcolor]').forEach(el=>{ el.value=terrain[el.dataset.tcolor]||'#ffffff'; });
  $$('[data-topacity]').forEach(el=>{ el.value=Math.round((terrain[el.dataset.topacity]??1)*100); });
  $$('[data-topv]').forEach(el=>{ el.textContent=Math.round((terrain[el.dataset.topv]??1)*100)+'%'; });
}
function wireTerrain(){
  $$('[data-tcolor]').forEach(el=>{ el.onchange=()=>{ if(terrain){ terrain[el.dataset.tcolor]=el.value; saveTerrain(); } }; });
  $$('[data-topacity]').forEach(el=>{
    const k=el.dataset.topacity, v=$(`[data-topv="${k}"]`);
    el.oninput=()=>{ if(v) v.textContent=el.value+'%'; };
    el.onchange=()=>{ if(terrain){ terrain[k]=(+el.value)/100; saveTerrain(); } };
  });
}

function iconRow(key,label,o){
  return `<div class="stylerow" data-k="${key}">
    <label class="sw"><input type="checkbox" class="i-en"${o.enabled?' checked':''}><span class="track"></span><span class="knob"></span></label>
    <span class="nm">${label}</span>
    ${pickerHtml(o.shape,o.color)}
    <input type="color" class="i-color" value="${o.color||'#ffffff'}">
    <input type="range" class="op i-op" min="0" max="100" value="${pct(o.opacity)}">
    <span class="opv">${pct(o.opacity)}%</span>
    <input type="number" class="numin sz i-size" step="0.1" min="0.5" value="${o.size}">
  </div>`;
}
function renderIcons(){
  if(!styles){ $('#iconStyles').innerHTML=''; return; }
  $('#iconStyles').innerHTML=ICON_KEYS.map(([k,l])=>iconRow(k,l,styles[k]||{})).join('');
  $$('#iconStyles .stylerow').forEach(row=>{
    const o=styles[row.dataset.k]; if(!o) return;
    const pk=row.querySelector('.iconpick');
    row.querySelector('.i-en').onchange=e=>{ o.enabled=e.target.checked; saveStyles(); };
    pk.onclick=()=>openIconPicker(pk,o.shape,n=>{ o.shape=n; refreshPicker(pk,n,o.color); saveStyles(); });
    row.querySelector('.i-color').onchange=e=>{ o.color=e.target.value; refreshPicker(pk,o.shape,o.color); saveStyles(); };
    const op=row.querySelector('.i-op'), opv=row.querySelector('.opv');
    op.oninput=()=>{ opv.textContent=op.value+'%'; };
    op.onchange=()=>{ o.opacity=(+op.value)/100; saveStyles(); };
    row.querySelector('.i-size').onchange=e=>{ const v=parseFloat(e.target.value); if(!isNaN(v)){ o.size=v; saveStyles(); } };
  });
}

/* Entity categories a mechanic rule can be gated to (value = Poe2Live.EntityCategory name). Empty
   selection = applies to every category. Labels are friendlier than the raw enum names. */
const MECH_CATS=[['Monster','cat.Monsters'],['Chest','cat.Chests'],['Other','cat.Misc'],
  ['Object','cat.Terrain'],['Npc','cat.NPCs'],['Transition','cat.Transitions']];
function mechRow(m,i){
  const cats=m.categories||[];
  return `<div class="mechrow" data-i="${i}">
    <div class="top">
      <label class="sw"><input type="checkbox" class="m-en"${m.enabled?' checked':''}><span class="track"></span><span class="knob"></span></label>
      <input class="mname" placeholder="Name (e.g. Expedition)" value="${esc(m.name)}">
      <button class="delbtn m-del">${t('btn.remove')}</button>
    </div>
    <input class="matchin m-match" placeholder="match terms, comma-separated (e.g. Strongbox, StrongBoxes)" value="${esc((m.match||[]).join(', '))}">
    <div class="mcats"><span class="mcats-lbl">Applies to</span>${MECH_CATS.map(([v,l])=>
      `<label class="catchip${cats.includes(v)?' on':''}"><input type="checkbox" class="m-cat" data-cat="${v}"${cats.includes(v)?' checked':''}>${t(l)}</label>`).join('')}
      <span class="mcats-hint">${cats.length?'':'all types'}</span></div>
    <div class="ctl">
      ${pickerHtml(m.shape,m.color)}
      <input type="color" class="m-color" value="${m.color||'#ffffff'}">
      <input type="range" class="op m-op" min="0" max="100" value="${pct(m.opacity)}">
      <span class="opv">${pct(m.opacity)}%</span>
      <input type="number" class="numin sz m-size" step="0.1" min="0.5" value="${m.size}">
    </div>
  </div>`;
}
function renderMechanics(){
  if(!styles){ $('#mechList').innerHTML=''; return; }
  styles.mechanics=styles.mechanics||[];
  $('#mechList').innerHTML=styles.mechanics.map((m,i)=>mechRow(m,i)).join('');
  $$('#mechList .mechrow').forEach(row=>{
    const m=styles.mechanics[+row.dataset.i]; if(!m) return;
    const pk=row.querySelector('.iconpick');
    row.querySelector('.m-en').onchange=e=>{ m.enabled=e.target.checked; saveStyles(); };
    row.querySelector('.mname').onchange=e=>{ m.name=e.target.value; saveStyles(); };
    row.querySelector('.m-match').onchange=e=>{ m.match=e.target.value.split(',').map(s=>s.trim()).filter(Boolean); saveStyles(); };
    row.querySelectorAll('.m-cat').forEach(cb=>{ cb.onchange=()=>{
      m.categories=[...row.querySelectorAll('.m-cat:checked')].map(c=>c.dataset.cat);
      cb.closest('.catchip').classList.toggle('on',cb.checked);
      const h=row.querySelector('.mcats-hint'); if(h) h.textContent=m.categories.length?'':'all types';
      saveStyles(); }; });
    pk.onclick=()=>openIconPicker(pk,m.shape,n=>{ m.shape=n; refreshPicker(pk,n,m.color); saveStyles(); });
    row.querySelector('.m-color').onchange=e=>{ m.color=e.target.value; refreshPicker(pk,m.shape,m.color); saveStyles(); };
    const op=row.querySelector('.m-op'), opv=row.querySelector('.opv');
    op.oninput=()=>{ opv.textContent=op.value+'%'; };
    op.onchange=()=>{ m.opacity=(+op.value)/100; saveStyles(); };
    row.querySelector('.m-size').onchange=e=>{ const v=parseFloat(e.target.value); if(!isNaN(v)){ m.size=v; saveStyles(); } };
    row.querySelector('.m-del').onclick=()=>{ styles.mechanics.splice(+row.dataset.i,1); renderMechanics(); saveStyles(); };
  });
}
/* ── Rules tab: unified Display Rules + Hidden cull patterns ── */
let hidden=[], drules=[];
function flashF(){ const m=$('#savedMsgF'); if(!m) return; m.classList.add('show'); clearTimeout(m._t); m._t=setTimeout(()=>m.classList.remove('show'),1100); }
async function postHidden(body){ try{ await fetch('/api/hidden',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); flashF(); }catch(e){} }
async function loadFilters(){
  await loadModVocab();   // populate the mods autocomplete BEFORE rendering rule rows reference it
  await loadDrules();
  try{ const h=await getJSON('/api/hidden'); hidden=h.patterns||[]; }catch(e){ hidden=[]; }
  renderHidden();
}
/* The persistent monster-mod catalog feeds the <datalist> the Mods matcher autocompletes against, so
   you can pick a known aura/buff id instead of recalling it. Refreshed each time the Rules tab loads. */
async function loadModVocab(){
  let mods=[]; try{ const r=await getJSON('/api/mods'); mods=(r&&r.mods)||[]; }catch(_){ mods=[]; }
  let dl=document.getElementById('modVocab');
  if(!dl){ dl=document.createElement('datalist'); dl.id='modVocab'; document.body.appendChild(dl); }
  dl.innerHTML=mods.map(m=>`<option value="${esc(m)}">`).join('');
}

/* ── Display Rules: the unified ordered ruleset. The page holds the array, edits it, and re-POSTs
   the WHOLE list on any change (add / remove / reorder / toggle / field) — same pattern styles used. ── */
const DR_CATS=['Monster','Chest','Npc','Object','Other','Transition','Player','Tile'];
const TERM_KEYS={'Normal':'rar.Normal','Magic':'rar.Magic','Rare':'rar.Rare','Unique':'rar.Unique','Hostile':'sel.hostile','Friendly':'sel.friendly','Alive':'sel.alive','Dead':'sel.dead','Opened':'sel.opened','Unopened':'sel.unopened','Yes':'sel.yes','No':'sel.no','Active':'sel.active','Complete':'sel.complete'};
const tr=v=>t(TERM_KEYS[v]||v);
const DR_SELECTS=[['rarity','sel.rarity',[['Normal','rar.Normal'],['Magic','rar.Magic'],['Rare','rar.Rare'],['Unique','rar.Unique']]],
  ['reaction','sel.reaction',[['Hostile','sel.hostile'],['Friendly','sel.friendly']]],
  ['life','sel.life',[['Alive','sel.alive'],['Dead','sel.dead']]],
  ['chest','sel.chest',[['Opened','sel.opened'],['Unopened','sel.unopened']]],
  ['poi','sel.poi',[['Yes','sel.yes'],['No','sel.no']]],
  ['encounter','sel.encounter',[['Active','sel.active'],['Complete','sel.complete']]]];
async function saveDrules(){ try{ await fetch('/api/display-rules',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({rules:drules})}); flashF(); }catch(e){} }
async function loadDrules(){ try{ const r=await getJSON('/api/display-rules'); drules=r.rules||[]; }catch(e){ drules=[]; } renderDrules(); }
function drSel(f,l,o,cur){ return `<label class="drsel">${t(l)}<select class="dr-cond" data-f="${f}"><option value="">${t('sel.any')}</option>`
  +o.map(([v,k])=>`<option value="${v}"${cur===v?' selected':''}>${t(k)}</option>`).join('')+`</select></label>`; }
/* Concise matcher→action summary shown on the collapsed row so the list stays scannable. */
function drSummary(r){
  const p=[];
  p.push((r.categories&&r.categories.length)?r.categories.join('/'):t('rules.anyType'));
  if(r.match&&r.match.length) p.push('“'+r.match.join(', ')+'”');
  if(r.mods&&r.mods.length) p.push(t('rules.modsLabel')+r.mods.join(', '));
  ['rarity','reaction','life','chest','poi','encounter'].forEach(f=>{ if(r[f]) p.push(tr(r[f])); });
  return esc(p.join(' · '));
}
function drRow(r,i){
  const open=!!r._open, cats=r.categories||[];
  const badges=(r.hide?'<span class="drbadge hide">hide</span>':'')
    +(r.navigable?'<span class="drbadge">path</span>':'');
  const body=open?`<div class="drbody">
      <div class="top"><input class="mname dr-name" value="${esc(r.name)}" placeholder="${t('rules.ruleNamePh')}"></div>
      <input class="matchin dr-match" placeholder="${t('rules.matchPh')}" value="${esc((r.match||[]).join(', '))}">
      <input class="matchin dr-mods" list="modVocab" placeholder="${t('rules.modsPh')}" value="${esc((r.mods||[]).join(', '))}">
      <div class="mcats"><span class="mcats-lbl">Type</span>${DR_CATS.map(c=>
        `<label class="catchip${cats.includes(c)?' on':''}"><input type="checkbox" class="dr-cat" data-cat="${c}"${cats.includes(c)?' checked':''}>${t('cat.'+c)}</label>`).join('')}</div>
      <div class="drconds">${DR_SELECTS.map(([f,l,o])=>drSel(f,l,o,r[f])).join('')}</div>
      <div class="ctl">
        <label class="drflag dr-hideflag" title="hide matching entities entirely"><input type="checkbox" class="dr-hide"${r.hide?' checked':''}> ${t('btn.hide')}</label>
        ${pickerHtml(r.shape,r.color)}
        <input type="color" class="dr-color" value="${r.color||'#ffffff'}">
        <input type="range" class="op dr-op" min="0" max="100" value="${pct(r.opacity)}"><span class="opv">${pct(r.opacity)}%</span>
        <input type="number" class="numin sz dr-size" step="0.1" min="0.5" value="${r.size}">
        <input class="mname dr-label" style="flex:1;min-width:70px" value="${esc(r.label||'')}" placeholder="${t('rules.labelPh')}">
        <label class="drflag" title="qualify as an auto-path navigation target"><input type="checkbox" class="dr-nav"${r.navigable?' checked':''}> ${t('btn.autoPath')}</label>
      </div>
    </div>`:'';
  return `<div class="mechrow drrow${r.hide?' hideon':''}${open?' open':''}${r.enabled?'':' off'}" data-i="${i}">
    <div class="drhead">
      <label class="sw" title="enabled"><input type="checkbox" class="dr-en"${r.enabled?' checked':''}><span class="track"></span><span class="knob"></span></label>
      <span class="drcaret">${open?'▾':'▸'}</span>
      <span class="drswatch" style="color:${r.color||'#fff'}">${r.hide?'':iconSvg(r.shape,r.color)}</span>
      <span class="drnm">${esc(r.name||t('rules.unnamed'))}</span>
      <span class="drsum">${drSummary(r)}</span>
      <span class="drbadges">${badges}</span>
      <span class="drord"><button class="ordbtn dr-up" title="higher precedence">▲</button><button class="ordbtn dr-dn" title="lower precedence">▼</button></span>
      <button class="delbtn dr-del" title="remove">✕</button>
    </div>
    ${body}
  </div>`;
}
function renderDrules(){
  const host=$('#drList'); if(!host) return;
  host.innerHTML = drules.length ? drules.map(drRow).join('') : '<div class="row"><div class="rl hint-row">'+t('rules.noRules')+'</div></div>';
  $$('#drList .drrow').forEach(row=>{
    const i=+row.dataset.i, r=drules[i]; if(!r) return;
    const save=saveDrules;
    // Header (always present): click anywhere except a control toggles expand.
    row.querySelector('.drhead').onclick=e=>{ if(e.target.closest('input,button,select,label,.drord')) return; r._open=!r._open; renderDrules(); };
    row.querySelector('.dr-en').onchange=e=>{ r.enabled=e.target.checked; row.classList.toggle('off',!r.enabled); save(); };
    row.querySelector('.dr-up').onclick=()=>{ if(i>0){ const t=drules[i-1]; drules[i-1]=drules[i]; drules[i]=t; renderDrules(); save(); } };
    row.querySelector('.dr-dn').onclick=()=>{ if(i<drules.length-1){ const t=drules[i+1]; drules[i+1]=drules[i]; drules[i]=t; renderDrules(); save(); } };
    row.querySelector('.dr-del').onclick=()=>{ drules.splice(i,1); renderDrules(); save(); };
    if(!r._open) return; // body controls only exist when expanded
    const pk=row.querySelector('.iconpick');
    row.querySelector('.dr-name').onchange=e=>{ r.name=e.target.value; save(); };
    row.querySelector('.dr-match').onchange=e=>{ r.match=e.target.value.split(',').map(s=>s.trim()).filter(Boolean); save(); };
    row.querySelector('.dr-mods').onchange=e=>{ r.mods=e.target.value.split(',').map(s=>s.trim()).filter(Boolean); save(); };
    row.querySelectorAll('.dr-cat').forEach(cb=>cb.onchange=()=>{ r.categories=[...row.querySelectorAll('.dr-cat:checked')].map(c=>c.dataset.cat); cb.closest('.catchip').classList.toggle('on',cb.checked); save(); });
    row.querySelectorAll('.dr-cond').forEach(sel=>sel.onchange=()=>{ r[sel.dataset.f]=sel.value||null; save(); });
    row.querySelector('.dr-hide').onchange=e=>{ r.hide=e.target.checked; row.classList.toggle('hideon',r.hide); save(); };
    pk.onclick=()=>openIconPicker(pk,r.shape,n=>{ r.shape=n; refreshPicker(pk,n,r.color); save(); });
    row.querySelector('.dr-color').onchange=e=>{ r.color=e.target.value; refreshPicker(pk,r.shape,r.color); save(); };
    const op=row.querySelector('.dr-op'),opv=row.querySelector('.opv'); op.oninput=()=>opv.textContent=op.value+'%'; op.onchange=()=>{ r.opacity=(+op.value)/100; save(); };
    row.querySelector('.dr-size').onchange=e=>{ const v=parseFloat(e.target.value); if(!isNaN(v)){ r.size=v; save(); } };
    row.querySelector('.dr-label').onchange=e=>{ r.label=e.target.value; save(); };
    row.querySelector('.dr-nav').onchange=e=>{ r.navigable=e.target.checked; save(); };
  });
}
$('#drAdd')?.addEventListener('click',()=>{ drules.push({enabled:true,name:t('rules.newRule'),categories:[],match:[],shape:'Circle',color:'#ffd926',opacity:1,size:4,_open:true}); renderDrules(); saveDrules(); });

/* ── Add-rule picker: browse the area's live ENTITIES + terrain TILE names + monster MODS, filter,
   click to seed a rule (entity → entity rule by category; tile → Tile rule; mod → Monster rule whose
   Mods matcher targets that affix id). Removes the guesswork of typing metadata/mod ids. ── */
let _pickEl=null, _pickEnts=[], _pickTiles=[], _pickKind='all', _pickQ='';
const lastSeg=s=>((s||'').split('/').pop()||'').replace(/@\d+$/,'').replace(/\.tdt$/i,'');
function ensurePick(){
  if(_pickEl) return _pickEl;
  _pickEl=document.createElement('div'); _pickEl.id='pickPop';
  _pickEl.innerHTML=`<div class="pickbox">
    <div class="pickhead">
      <input id="pickSearch" type="search" data-i18n-ph="pick.filterPh" placeholder="filter by name / metadata / tile path / mod id…">
      <span class="pickkinds"><button class="chip on" data-k="all" data-i18n="pick.all">All</button><button class="chip" data-k="entity" data-i18n="pick.entities">Entities</button><button class="chip" data-k="tile" data-i18n="pick.tiles">Tiles</button><button class="chip" data-k="mod" data-i18n="pick.mods">Mods</button></span>
      <button class="pickclose" title="close">✕</button>
    </div>
    <div class="picklist" id="pickList"></div>
    <div class="pickfoot">Click a target to add a rule for it (opens expanded to refine). Entities seed an entity rule; tiles seed a Tile rule; mods seed a Monster rule matching that affix.</div>
  </div>`;
  document.body.appendChild(_pickEl);
  _pickEl.querySelector('.pickclose').onclick=()=>_pickEl.classList.remove('open');
  _pickEl.onclick=e=>{ if(e.target===_pickEl) _pickEl.classList.remove('open'); };
  _pickEl.querySelector('#pickSearch').oninput=e=>{ _pickQ=e.target.value.toLowerCase(); renderPick(); };
  _pickEl.querySelectorAll('.pickkinds .chip').forEach(c=>c.onclick=()=>{ _pickKind=c.dataset.k; _pickEl.querySelectorAll('.pickkinds .chip').forEach(x=>x.classList.toggle('on',x===c)); renderPick(); });
  return _pickEl;
}
async function openPicker(){
  const pop=ensurePick(); pop.classList.add('open');
  _pickQ=''; _pickKind='all';
  pop.querySelector('#pickSearch').value=''; pop.querySelectorAll('.pickkinds .chip').forEach((x,j)=>x.classList.toggle('on',j===0));
  $('#pickList').innerHTML='<div class="pickempty">'+t('pick.loading')+'</div>';
  try{ _pickEnts=await getJSON('/entities?limit=1000')||[]; }catch(_){ _pickEnts=[]; }
  try{ const t=await getJSON('/api/tiles'); _pickTiles=(t&&t.tiles)||[]; }catch(_){ _pickTiles=[]; }
  renderPick(); pop.querySelector('#pickSearch').focus();
}
/* Aggregate the live entities' affix-mod ids into distinct rows: one per mod id, with a carrier
   count and a few example monster names — so you can see which auras/buffs are actually in the zone
   right now and pick one to track. (Each entity lists a mod id at most once, so count = #monsters.) */
function pickMods(){
  const map=new Map(); // modId -> {count, names:Set}
  _pickEnts.forEach(e=>{ (e.mods||[]).forEach(m=>{ if(!m)return; let v=map.get(m); if(!v){v={count:0,names:new Set()}; map.set(m,v);} v.count++; const nm=e.name||lastSeg(e.metadata); if(nm) v.names.add(nm); }); });
  return [...map.entries()].sort((a,b)=>b[1].count-a[1].count).map(([m,v])=>({
    kind:'mod', cat:'Mod', name:m, modId:m, count:v.count,
    sub:[...v.names].slice(0,4).join(', ')||'monster affix',
  }));
}
function pickItems(){
  const q=_pickQ, out=[];
  if(_pickKind==='all'||_pickKind==='entity'){
    const seen=new Set();
    _pickEnts.forEach(e=>{ const k=e.category+'|'+e.metadata; if(seen.has(k))return; seen.add(k);
      if(q && !((e.metadata||'').toLowerCase().includes(q)||(e.name||'').toLowerCase().includes(q)||(e.category||'').toLowerCase().includes(q)))return;
      out.push({kind:'entity',cat:e.category,name:e.name||lastSeg(e.metadata),sub:e.metadata,rarity:e.rarity}); });
  }
  if(_pickKind==='all'||_pickKind==='tile'){
    _pickTiles.forEach(p=>{ if(q && !p.toLowerCase().includes(q))return; out.push({kind:'tile',cat:'Tile',name:lastSeg(p),sub:p}); });
  }
  if(_pickKind==='all'||_pickKind==='mod'){
    pickMods().forEach(it=>{ if(q && !(it.name.toLowerCase().includes(q)||it.sub.toLowerCase().includes(q)))return; out.push(it); });
  }
  return out;
}
function renderPick(){
  const items=pickItems(), list=$('#pickList');
  list.innerHTML = items.length ? items.slice(0,600).map((it,i)=>
    `<div class="pickrow" data-i="${i}"><span class="pickbadge ${it.kind}">${it.kind==='tile'?'TILE':it.kind==='mod'?'MOD':esc(it.cat)}</span>`
    +`<span class="picknm">${esc(it.name)}</span><span class="picksub">${esc(it.sub)}</span>`
    +(it.kind==='mod'?`<span class="pickcount">×${it.count}</span>`:'')
    +(it.rarity&&it.rarity!=='NonMonster'?`<span class="pickrar">${esc(it.rarity)}</span>`:'')+`</div>`).join('')
    : `<div class="pickempty">${t('pick.noMatches')}${(_pickEnts.length+_pickTiles.length===0)?t('pick.noMatchesGame'):''}.</div>`;
  $$('#pickList .pickrow').forEach(row=>row.onclick=()=>pickItem(items[+row.dataset.i]));
}
function pickItem(it){
  if(!it) return;
  let r;
  if(it.kind==='tile')
    r={enabled:true,name:it.name,categories:['Tile'],match:[lastSeg(it.sub)],shape:'Diamond',color:'#f259f2',opacity:1,size:5,navigable:true,_open:true};
  else if(it.kind==='mod')
    r={enabled:true,name:it.name,categories:['Monster'],match:[],mods:[it.modId],shape:'Star',color:'#26d9c0',opacity:1,size:6,_open:true};
  else
    r={enabled:true,name:it.name,categories:[it.cat],match:[lastSeg(it.sub)],shape:'Star',color:'#ffd926',opacity:1,size:6,_open:true};
  drules.unshift(r); renderDrules(); saveDrules();
  _pickEl.classList.remove('open');
  const first=$('#drList .drrow'); if(first) first.scrollIntoView({block:'center'});
}
$('#drPick')?.addEventListener('click',openPicker);
function renderHidden(){
  $('#hideList').innerHTML = hidden.length ? hidden.map(p=>
    `<span class="chip on" data-p="${esc(p)}">${esc(p)} <b style="margin-left:5px;cursor:pointer">&#10005;</b></span>`).join('')
    : '<span style="color:var(--ink-faint);font-size:11px;font-style:italic">'+t('rules.nothingHidden')+'</span>';
  $$('#hideList .chip').forEach(c=>c.querySelector('b').onclick=()=>{ postHidden({remove:c.dataset.p}).then(loadFilters); });
}
$('#hideAdd').onclick=()=>{
  const p=$('#hidePattern').value.trim(); if(!p) return;
  $('#hidePattern').value='';
  postHidden({add:p}).then(loadFilters);
};
$('#hidePattern').onkeydown=e=>{ if(e.key==='Enter') $('#hideAdd').click(); };

/* ── Landmarks tab: view/edit the curated map-label table (baked + user overlay) + import/export ── */
let lmEntries=[], lmAreaOnly=true, lmQ='';
function flashL(){ const m=$('#savedMsgL'); if(!m) return; m.classList.add('show'); clearTimeout(m._t); m._t=setTimeout(()=>m.classList.remove('show'),1100); }
async function loadLandmarks(){
  try{ const r=await getJSON('/api/landmarks'); lmEntries=r.entries||[]; }catch(e){ lmEntries=[]; }
  const a=$('#lmArea'); if(a && !a.value) a.value=(state&&state.areaCode)||'';
  renderLandmarks();
}
async function postLandmarks(body){
  try{ const r=await fetch('/api/landmarks',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); const j=await r.json(); if(j&&j.entries) lmEntries=j.entries; flashL(); }catch(e){}
  renderLandmarks();
}
function lmRow(e){
  const badge=e.suppressed?'hidden':e.source;
  const del=t(e.suppressed?'btn.restore':(e.source==='user'?'btn.remove':'btn.hide'));
  return `<div class="lmrow${e.suppressed?' sup':''}" data-area="${esc(e.area)}" data-pat="${esc(e.pattern)}">
    <span class="lmbadge ${badge}">${badge}</span>
    <span class="lmarea">${esc(e.area)}</span>
    <input class="mname lmlabel" value="${esc(e.label||'')}" placeholder="${e.suppressed?'(hidden)':'label'}">
    <span class="lmpath" title="${esc(e.pattern)}">${esc(e.pattern)}</span>
    <button class="delbtn lm-del">${del}</button>
  </div>`;
}
function renderLandmarks(){
  const host=$('#lmList'); if(!host) return;
  const area=(state&&state.areaCode)||'';
  const rows=lmEntries.filter(e=>{
    if(lmAreaOnly && e.area!=='*' && e.area!==area) return false;
    if(lmQ){ if(!((e.area+' '+e.pattern+' '+(e.label||'')).toLowerCase().includes(lmQ))) return false; }
    return true;
  });
  host.innerHTML = rows.length ? rows.map(lmRow).join('')
    : `<div class="row"><div class="rl hint-row">${t('lm.noLandmarks')}${lmAreaOnly?t('lm.forArea')+esc(area||'—')+')':''}. ${t('lm.addOne')}${lmAreaOnly?t('lm.orOff'):''}.</div></div>`;
  $$('#lmList .lmrow').forEach(row=>{
    const area=row.dataset.area, pat=row.dataset.pat, e=lmEntries.find(x=>x.area===area&&x.pattern===pat); if(!e) return;
    row.querySelector('.lmlabel').onchange=ev=>postLandmarks({set:{area,pattern:pat,label:ev.target.value}});
    row.querySelector('.lm-del').onclick=()=>{
      if(e.suppressed || e.source==='user') postLandmarks({remove:{area,pattern:pat}}); // restore baked / delete user
      else postLandmarks({set:{area,pattern:pat,label:null}});                          // suppress a baked entry
    };
  });
}
$('#lmSearch')?.addEventListener('input',e=>{ lmQ=e.target.value.toLowerCase(); renderLandmarks(); });
$('#lmAreaOnly')?.addEventListener('click',()=>{ lmAreaOnly=!lmAreaOnly; $('#lmAreaOnly').classList.toggle('on',lmAreaOnly); renderLandmarks(); });
$('#lmAdd')?.addEventListener('click',()=>{
  const area=($('#lmArea').value||'').trim(), pat=($('#lmPat').value||'').trim(), label=($('#lmLabel').value||'').trim();
  if(!area||!pat||!label) return;
  $('#lmPat').value=''; $('#lmLabel').value='';
  postLandmarks({set:{area,pattern:pat,label}});
});
$('#lmExport')?.addEventListener('click',async()=>{
  try{ const txt=await (await fetch('/api/landmarks?export=1',{cache:'no-store'})).text();
    const a=document.createElement('a'); a.href=URL.createObjectURL(new Blob([txt],{type:'application/json'}));
    a.download='CustomLandmarks.json'; a.click(); URL.revokeObjectURL(a.href);
  }catch(e){}
});
$('#lmImport')?.addEventListener('click',()=>{
  const inp=document.createElement('input'); inp.type='file'; inp.accept='.json,application/json';
  inp.onchange=()=>{ const f=inp.files&&inp.files[0]; if(!f) return; const rd=new FileReader();
    rd.onload=()=>{ try{ postLandmarks({import:JSON.parse(rd.result)}); }catch(_){ alert('Invalid JSON file'); } };
    rd.readAsText(f); };
  inp.click();
});

/* ── atlas tab (read-only inspection of the map-data we can read) ── */
let atlasOptsWired=false, atlasGroupsData=[];
async function wireAtlasOpts(){
  let s; try{ s=await getJSON('/api/settings'); }catch(e){ return; }
  document.querySelectorAll('#atlasOpts [data-atset]').forEach(el=>{
    const k=el.dataset.atset;
    if(el.type==='checkbox'){ el.checked=!!s[k]; el.onchange=()=>saveSetting(k,el.checked); }
    else { el.value=s[k]; el.onchange=()=>saveSetting(k, parseFloat(el.value)); }
  });
  atlasGroupsData = Array.isArray(s.atlasGroups) ? s.atlasGroups.map(g=>({name:g.name||'',color:g.color||'#E0B341',maps:(g.maps||[]).slice()})) : [];
  renderAtlasGroups();
}
function saveAtlasGroups(){ saveSetting('atlasGroups', atlasGroupsData); }
function renderAtlasGroups(){
  const box=document.querySelector('#atlasGroups'); if(!box) return;
  if(atlasGroupsData.length===0){ box.innerHTML='<span class="hint-row" style="opacity:.6">'+t('atlas.noGroups')+'</span>'; return; }
  box.innerHTML = atlasGroupsData.map((g,i)=>
    '<div style="display:grid;grid-template-columns:130px 44px 1fr 60px;gap:8px;align-items:start;padding:5px 0;border-bottom:1px solid var(--line)">'
    +'<input data-gi="'+i+'" data-gf="name" value="'+esc(g.name)+'" placeholder="'+t('atlas.groupNamePh')+'" style="width:100%">'
    +'<input data-gi="'+i+'" data-gf="color" type="color" value="'+esc(g.color)+'" style="width:40px;height:28px;padding:0;border:none;background:none">'
    +'<textarea data-gi="'+i+'" data-gf="maps" rows="2" placeholder="'+t('atlas.mapsPh')+'" style="width:100%;resize:vertical">'+esc((g.maps||[]).join('\n'))+'</textarea>'
    +'<button class="chip" data-gdel="'+i+'">'+t('btn.delete')+'</button></div>'
  ).join('');
  box.querySelectorAll('[data-gf]').forEach(el=>{
    const i=+el.dataset.gi, f=el.dataset.gf;
    el.onchange=()=>{ if(f==='maps') atlasGroupsData[i].maps=el.value.split('\n').map(x=>x.trim()).filter(Boolean); else atlasGroupsData[i][f]=el.value; saveAtlasGroups(); };
  });
  box.querySelectorAll('[data-gdel]').forEach(b=>b.onclick=()=>{ atlasGroupsData.splice(+b.dataset.gdel,1); renderAtlasGroups(); saveAtlasGroups(); });
}
document.querySelector('#atlasGroupAdd')?.addEventListener('click',()=>{ atlasGroupsData.push({name:t('atlas.newGroup'),color:'#E0B341',maps:[]}); renderAtlasGroups(); saveAtlasGroups(); });
async function loadAtlas(){
  $('#atlasStatus').textContent=t('atlas.reading');
  if(!atlasOptsWired){ atlasOptsWired=true; wireAtlasOpts(); }
  try{ atlasData=await getJSON('/api/atlas'); }catch(e){ atlasData={located:false,note:'request failed'}; }
  renderAtlas();
}
function renderAtlas(){
  const d=atlasData; if(!d){ return; }
  const st=$('#atlasStatus'); const nd=d.nodes;
  if(!(nd&&nd.total)) st.textContent = d.note ? t('atlas.statusScan') : t('atlas.statusClosed');
  else st.textContent = nd.total+' '+t('atlas.nodes')+' · '+nd.hasContent+' '+t('atlas.withContent')+' · '
        +(d.allKinds?.length||0)+' '+t('atlas.kinds')+' / '+(d.allTags?.length||0)+' '+t('atlas.contents')+' / '+(d.allMaps?.length||0)+' '+t('atlas.mapFilters');
  // Seed active rules from the overlay (once): tracked + arrow sets. Then render the filter table.
  if(atlasHl===null){ atlasHl=new Set((d.highlightTags||[]).map(t=>t.toLowerCase())); atlasNav=new Set((d.navTags||[]).map(t=>t.toLowerCase())); atlasArrow=new Set((d.arrowTags||[]).map(t=>t.toLowerCase())); }
  renderAtlasHighlight(d);
}
// Biome index → friendly-ish label (best-effort; index is the ground truth).
const BIOMES=['Grass','Sand','Swamp','Forest','Snow','Stone','Volcanic','Coast','Cave','Vaal','Water','Desert','Special'];
const biomeName=i=>(i>=0&&i<BIOMES.length)?BIOMES[i]:('biome '+i);

// Highlight-rule chips: one per distinct content tag on the atlas. Click to toggle → ONLY matching maps
// are drawn in-game. Active set is pushed to the overlay (persisted there).
// Classify a filter row into a category for the table (and grouping/colour).
function catContent(t){ const s=t.toLowerCase(); if(/not shown|\[dnt\]/.test(s))return'Hidden'; if(/boss/.test(s))return'Boss'; if(/influence/.test(s))return'Influence'; return'Mechanic'; }
function catMap(t){ const s=t.toLowerCase(); if(/citadel/.test(s))return'Citadel'; if(/tower/.test(s))return'Tower'; if(/temple/.test(s))return'Temple'; if(/vaal/.test(s))return'Vaal'; return'Map'; }
// Per-category colour (badge tint).
const CATCOL={Boss:'#e0533a',Mechanic:'#3ca0ff',Influence:'#a06cff',Hidden:'#ff5db1',Citadel:'#e0b341',Tower:'#2fb6a8',Temple:'#d98a2b',Vaal:'#c0395a',Unique:'#c678dd',Merchant:'#5aa9e6',Map:'#8a93a0',Type:'#d98a2b'};
function catBadge(cat){ const c=CATCOL[cat]||'#8a93a0'; return '<span style="display:inline-block;padding:1px 8px;border-radius:10px;font-size:11px;font-weight:600;background:'+c+'26;color:'+c+';border:1px solid '+c+'66">'+esc(cat)+'</span>'; }
// Build the unified filter list (content + map) with {title,count,cat,group}.
function atlasFilterRows(d){
  const rows=[];
  // Kind rows first: tracking one (e.g. "Tower") rings + routes to EVERY map of that archetype.
  (d.allKinds||[]).forEach(t=>rows.push({title:t.tag,count:t.count,group:'Kind',cat:t.tag}));
  // Type rows (#7): maps.json type/tags — unique / lineage / arbiter. One-click route-to-all-of-a-kind.
  (d.allDataTags||[]).forEach(t=>rows.push({title:t.tag,count:t.count,group:'Type',cat:'Type'}));
  (d.allTags||[]).forEach(t=>rows.push({title:t.tag,count:t.count,group:'Content',cat:catContent(t.tag),desc:t.desc}));
  (d.allMaps||[]).forEach(t=>rows.push({title:t.tag,count:t.count,group:'Map',cat:catMap(t.tag)}));
  return rows;
}
let atlasHlSort={key:'count',dir:-1};
function renderAtlasHighlight(d){
  const box=$('#atlasHlTable'); if(!box) return;
  let rows=atlasFilterRows(d);
  if(rows.length===0){ box.innerHTML='<span class="hint-row" style="padding:8px;display:block">'+t('atlas.noFilters')+'</span>'; updateHlCount(); return; }
  if(atlasGroup!=='all') rows=rows.filter(r=>r.group===atlasGroup);
  const flt=($('#atlasHlFilter')?.value||'').trim().toLowerCase();
  if(flt) rows=rows.filter(r=>r.title.toLowerCase().includes(flt)||r.cat.toLowerCase().includes(flt)||r.group.toLowerCase().includes(flt));
  if(atlasHlSelOnly) rows=rows.filter(r=>{const k=r.title.toLowerCase(); return atlasHl.has(k)||atlasNav.has(k)||atlasArrow.has(k);});
  const k=atlasHlSort.key, dir=atlasHlSort.dir;
  rows.sort((a,b)=>{
    const ak=a.title.toLowerCase(), bk=b.title.toLowerCase();
    let v;
    if(k==='count') v=a.count-b.count;
    else if(k==='trk') v=(atlasHl.has(ak)?1:0)-(atlasHl.has(bk)?1:0);
    else if(k==='nav') v=(atlasNav.has(ak)?1:0)-(atlasNav.has(bk)?1:0);
    else if(k==='arw') v=(atlasArrow.has(ak)?1:0)-(atlasArrow.has(bk)?1:0);
    else v=(''+a[k]).localeCompare(''+b[k]);
    return v*dir || a.title.localeCompare(b.title);
  });
  const sa=key=> atlasHlSort.key===key ? (atlasHlSort.dir<0?' ▼':' ▲') : '';
  const cell='display:grid;grid-template-columns:30px 30px 34px 1fr 50px 90px;gap:8px;align-items:center;padding:5px 9px';
  let html='<div style="'+cell+';position:sticky;top:0;background:var(--panel,#1a1a1a);border-bottom:1px solid var(--line);font-weight:600;font-size:11px;text-transform:uppercase;opacity:.75">'
    +'<span data-sort="trk" title="Highlight: ring the map in-game (click to sort)" style="cursor:pointer">&#9745;'+sa('trk')+'</span>'
    +'<span data-sort="nav" title="Nav-to: draw a route to it (click to sort)" style="cursor:pointer">&#8674;'+sa('nav')+'</span>'
    +'<span data-sort="arw" title="Arrow: edge arrow toward it when off-screen (click to sort)" style="cursor:pointer">&#10148;'+sa('arw')+'</span>'
    +'<span data-sort="title" style="cursor:pointer">'+t('atlas.title')+sa('title')+'</span>'
    +'<span data-sort="count" style="cursor:pointer;text-align:right">'+t('atlas.count')+sa('count')+'</span>'
    +'<span data-sort="cat" style="cursor:pointer">'+t('atlas.category')+sa('cat')+'</span></div>';
  html+=rows.map(r=>{
    const key=r.title.toLowerCase(); const trk=atlasHl.has(key), nav=atlasNav.has(key), arw=atlasArrow.has(key);
    return '<div class="hlrow" data-tag="'+esc(r.title)+'" title="click row = toggle Highlight" style="'+cell+';cursor:pointer;border-bottom:1px solid var(--line)'+((trk||nav||arw)?';background:rgba(60,160,255,.14)':'')+'">'
      +'<span style="font-size:15px">'+(trk?'☑':'☐')+'</span>'
      +'<span class="hlnav" data-tag="'+esc(r.title)+'" title="toggle nav-to (route)" style="font-size:15px;cursor:pointer;color:'+(nav?'#3ddc97':'#4a525c')+'">&#8674;</span>'
      +'<span class="hlarw" data-tag="'+esc(r.title)+'" title="toggle off-screen arrow" style="font-size:15px;cursor:pointer;color:'+(arw?'#e0b341':'#4a525c')+'">➤</span>'
      +'<span title="'+esc(r.desc||r.title)+'">'+esc(r.title)+'</span>'
      +'<span class="amono" style="text-align:right">'+r.count+'</span>'
      +'<span>'+catBadge(r.cat)+'</span></div>';
  }).join('');
  box.innerHTML=html;
  $$('#atlasHlTable [data-sort]').forEach(h=>h.onclick=()=>{ const key=h.dataset.sort; if(atlasHlSort.key===key) atlasHlSort.dir*=-1; else atlasHlSort={key,dir:(key==='count'||key==='trk'||key==='nav'||key==='arw')?-1:1}; renderAtlasHighlight(d); });
  $$('#atlasHlTable .hlnav[data-tag]').forEach(a=>a.onclick=e=>{
    e.stopPropagation(); const key=a.dataset.tag.toLowerCase();
    if(atlasNav.has(key)) atlasNav.delete(key); else atlasNav.add(key);
    renderAtlasHighlight(d); postAtlasHighlight();
  });
  $$('#atlasHlTable .hlarw[data-tag]').forEach(a=>a.onclick=e=>{
    e.stopPropagation(); const key=a.dataset.tag.toLowerCase();
    if(atlasArrow.has(key)) atlasArrow.delete(key); else atlasArrow.add(key);
    renderAtlasHighlight(d); postAtlasHighlight();
  });
  $$('#atlasHlTable .hlrow[data-tag]').forEach(row=>row.onclick=()=>{
    const key=row.dataset.tag.toLowerCase();
    if(atlasHl.has(key)) atlasHl.delete(key); else atlasHl.add(key);
    renderAtlasHighlight(d); postAtlasHighlight();
  });
  updateHlCount();
}
// Active-rule chips: one removable chip per tag that has any toggle on, showing which (✓⇢➤). Click ✕ to drop it.
function updateHlCount(){
  const box=$('#atlasActive'); if(!box) return;
  const keys=new Set([...(atlasHl||[]),...(atlasNav||[]),...(atlasArrow||[])]);
  if(keys.size===0){ box.innerHTML='<span class="hint-row" style="opacity:.6">'+t('atlas.noActive')+'</span>'; return; }
  // Recover original-case titles from the data.
  const titleOf={}; (atlasData?atlasFilterRows(atlasData):[]).forEach(r=>titleOf[r.title.toLowerCase()]=r.title);
  const chip=k=>{ const t=titleOf[k]||k; const marks=(atlasHl.has(k)?'<span title="Highlight">&#9745;</span>':'')+(atlasNav.has(k)?'<span style="color:#3ddc97" title="Nav">&#8674;</span>':'')+(atlasArrow.has(k)?'<span style="color:#e0b341" title="Arrow">&#10148;</span>':'');
    return '<span class="achip" data-k="'+esc(k)+'" style="display:inline-flex;align-items:center;gap:5px;padding:3px 7px;margin:0 5px 5px 0;border:1px solid var(--line);border-radius:12px;font-size:12px;background:rgba(60,160,255,.10)">'+marks+'<b>'+esc(t)+'</b><span class="achipx" data-k="'+esc(k)+'" style="cursor:pointer;opacity:.6;font-weight:700">&times;</span></span>'; };
  box.innerHTML=[...keys].sort().map(chip).join('');
  $$('#atlasActive .achipx').forEach(x=>x.onclick=()=>{ const k=x.dataset.k; atlasHl.delete(k); atlasNav.delete(k); atlasArrow.delete(k); renderAtlasHighlight(atlasData); postAtlasHighlight(); });
}
// Push the active rules (original-case) to the overlay.
async function postAtlasHighlight(){
  // Build {tag,color,track,nav,arrow} rules: colour = the row's category colour, so in-game rings match the table.
  const rows=atlasData?atlasFilterRows(atlasData):[];
  const rules=rows.filter(r=>{const k=r.title.toLowerCase(); return atlasHl.has(k)||atlasNav.has(k)||atlasArrow.has(k);})
    .map(r=>{const k=r.title.toLowerCase(); return {tag:r.title, color:(CATCOL[r.cat]||'#3ca0ff'), track:atlasHl.has(k), nav:atlasNav.has(k), arrow:atlasArrow.has(k)};});
  try{ await fetch('/api/atlas-highlight',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({rules})}); }catch(e){}
}
$('#atlasHlClear')?.addEventListener('click',()=>{ atlasHl.clear(); atlasNav.clear(); atlasArrow.clear(); if(atlasData) renderAtlasHighlight(atlasData); postAtlasHighlight(); });
$('#atlasHlFilter')?.addEventListener('input',()=>{ if(atlasData) renderAtlasHighlight(atlasData); });
$('#atlasHlSelOnly')?.addEventListener('click',e=>{ atlasHlSelOnly=!atlasHlSelOnly; e.target.classList.toggle('on',atlasHlSelOnly); if(atlasData) renderAtlasHighlight(atlasData); });
$('#atlasHelp')?.addEventListener('click',()=>{ const b=$('#atlasHelpBox'); if(b) b.hidden=!b.hidden; });
// Group filter chips (All / Kind / Content / Map).
$$('[data-group]').forEach(b=>b.addEventListener('click',()=>{ atlasGroup=b.dataset.group; $$('[data-group]').forEach(x=>x.classList.toggle('on',x===b)); if(atlasData) renderAtlasHighlight(atlasData); }));
// Quick presets: select matching rows and flip the relevant toggles in one click.
const ATLAS_PRESETS={
  citadels:{m:r=>r.cat==='Citadel'||/citadel/i.test(r.title), trk:1,nav:1,arw:1},
  deadly:  {m:r=>/deadly/i.test(r.title),                     trk:1,nav:1,arw:0},
  bosses:  {m:r=>/boss/i.test(r.title),                       trk:1,nav:0,arw:0},
  towers:  {m:r=>r.cat==='Tower'||/tower/i.test(r.title),     trk:1,nav:1,arw:0},
  uniques: {m:r=>r.cat==='Unique'||/unique/i.test(r.title),   trk:1,nav:1,arw:0},
};
$$('#atlasPresets [data-preset]').forEach(b=>b.addEventListener('click',()=>{
  const p=ATLAS_PRESETS[b.dataset.preset]; if(!p||!atlasData) return;
  atlasFilterRows(atlasData).filter(p.m).forEach(r=>{ const k=r.title.toLowerCase(); if(p.trk)atlasHl.add(k); if(p.nav)atlasNav.add(k); if(p.arw)atlasArrow.add(k); });
  renderAtlasHighlight(atlasData); postAtlasHighlight();
}));

// Live-nodes grid: each row is a real atlas node. Click a row to SELECT it → the overlay highlights
// it in-game (projection calibration loop). Selection is the set of element addresses.
function renderAtlasNodes(d, f){
  let list=d.nodeList||[];
  if(f) list=list.filter(n=> (''+n.id).includes(f) || biomeName(n.biome).toLowerCase().includes(f)
      || (n.map||'').toLowerCase().includes(f) || (n.hasContent&&'content'.includes(f))
      || (!n.visited&&'unvisited'.includes(f)) || ('biome '+n.biome).includes(f)
      || (n.tags||[]).some(t=>t.toLowerCase().includes(f)));   // match on map name + content names
  if(list.length===0){ $('#atlasList').innerHTML='<div class="hint-row">'+t('atlas.noNodes')+'</div>'; return; }
  // Content nodes first (the interesting ones), then by tag count.
  list=list.slice().sort((a,b)=>((b.tags||[]).length)-((a.tags||[]).length));
  const head='<div class="arow ahead nrow"><span>'+t('atlas.map')+'</span><span>'+t('atlas.content')+'</span><span>'+t('atlas.biome')+'</span><span>'+t('atlas.pos')+'</span></div>';
  const body=list.slice(0,1200).map(n=>{
    const sel=atlasSel.has(n.el)?' sel':'';
    const hot=((n.map&&atlasHl.has(n.map.toLowerCase()))||(n.tags||[]).some(t=>atlasHl.has(t.toLowerCase())));
    const val=(n.tags&&n.tags.length)?' val':'';
    const content=(n.tags||[]).map(t=>'<span class="ntag tc">'+esc(t)+'</span>').join(' ')||'<span class="hint-row">—</span>';
    return '<div class="arow nrow'+val+sel+(hot?' sel':'')+'" data-el="'+esc(n.el)+'">'
      +'<span title="'+esc(n.map||'')+'">'+esc(n.map||'—')+(n.visited?' <span class="ntag tv">✓</span>':'')+'</span>'
      +'<span>'+content+'</span><span>'+esc(biomeName(n.biome))+'</span>'
      +'<span class="amono">('+n.x+','+n.y+')</span></div>';
  }).join('');
  $('#atlasList').innerHTML=head+body
    +'<div class="hint-row" style="margin-top:10px"><b>Click a node row to highlight it in-game</b> (drives the overlay’s atlas highlight — use it to confirm positions / calibrate). Click again to deselect. Showing '+Math.min(list.length,1200)+' of '+list.length+' nodes.</div>';
  $$('#atlasList .nrow[data-el]').forEach(row=>row.onclick=()=>{
    const el=row.dataset.el;
    if(atlasSel.has(el)) atlasSel.delete(el); else atlasSel.add(el);
    row.classList.toggle('sel',atlasSel.has(el));
    postAtlasSel();
  });
}
async function postAtlasSel(){ try{ await fetch('/api/atlas-select',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({els:[...atlasSel]})}); }catch(e){} }

$('#atlasRefresh')?.addEventListener('click',loadAtlas);
$('#atlasSearch')?.addEventListener('input',()=>{ if(atlasData) renderAtlas(); });
$$('#atlasViewCatalog,#atlasViewRegion,#atlasViewNodes').forEach(b=>b?.addEventListener('click',()=>{
  atlasView=b.dataset.view;
  $$('#atlasViewCatalog,#atlasViewRegion,#atlasViewNodes').forEach(x=>x.classList.toggle('on',x===b));
  renderAtlas();
}));

/* ── left rail ── */
function renderState(){
  const s=state; if(!s) return;
  const hp=Math.max(0,Math.min(100,s.hpPct||0)), mp=Math.max(0,Math.min(100,s.manaPct||0)), es=Math.max(0,Math.min(100,s.esPct||0));
  $('#hpBar').style.width=hp+'%'; $('#mpBar').style.width=mp+'%'; $('#esBar').style.width=es+'%';
  $('#hpNum').textContent=hp.toFixed(0)+'%'; $('#mpNum').textContent=mp.toFixed(0)+'%'; $('#esNum').textContent=es.toFixed(0)+'%';
  const areaName=(s.areaName&&s.areaName!==s.areaCode)?s.areaName:'';
  $('#kAreaName').textContent=areaName||s.areaCode||'—';
  $('#kArea').textContent=s.areaCode||'—';
  const act=s.areaAct||0;
  $('#kAlvl').textContent=(act?t('state.act')+' '+act+' · ':'')+(s.areaLevel?(t('state.lvl')+' '+s.areaLevel):'—');
  $('#kMap').textContent=t(s.mapVisible?'state.yes':'state.no');
  $('#kFlask').textContent=t(s.autoFlask?'state.on':'state.off')+(s.flask?' · '+s.flask:'');
  const fs=$('#flaskState'); if(fs) fs.textContent=t(s.autoFlask?'state.on':'state.off')+(s.flask?' · '+s.flask:'');
  $('#cEnt').textContent=s.entityCount||0;
  $('#cPoi').textContent=s.poiCount||0;
  $('#cMon').textContent=(s.counts&&s.counts.Monster)||0;
  $('#cLm').textContent=s.landmarkCount||0;
  $('#areaChip').innerHTML = (areaName||s.areaCode||'—') + ' <b>·</b> ' + t(s.inGame?'state.ingame':'state.town');

  // Runeshape monoliths (from /state): each monolith's value-tier header (best ex · anchor · N holes)
  // with its priced reward rows. Sorted server-side by value; hidden when the area has none.
  const mc=$('#monoCard'), ml=$('#monoList');
  const monos=(s.monoliths||[]).slice().sort((a,b)=>(b.bestEx||0)-(a.bestEx||0));
  if(monos.length){
    mc.hidden=false;
    ml.innerHTML = monos.map(m=>{
      const tier = (m.bestEx||0)>=30 ? '#66e066' : (m.bestEx||0)>=18 ? '#e6c84d' : '#cfcfcf';
      const hdr = (m.bestEx>0?('<b style="color:'+tier+'">'+Math.round(m.bestEx)+' ex</b> · '):'')
                + esc(m.anchor||'?') + ' · ' + (m.holes||0) + 'h' + (m.collected?' · <span style="opacity:.6">'+t('mono.collected')+'</span>':'');
      const rows=(m.rewards||[]).filter(r=>r.ex>0).slice(0,6)
        .map(r=>'<div style="display:flex;justify-content:space-between;gap:8px"><span>'+esc(r.name)+(r.count>1?(' ×'+r.count):'')+'</span><span style="opacity:.85">'+Math.round(r.ex)+' ex</span></div>').join('');
      return '<div style="margin:0 0 9px"><div style="margin-bottom:2px">'+hdr+'</div>'
           + '<div style="font-size:12px;opacity:.9;padding-left:8px">'+(rows||'<span style="opacity:.6">'+t('mono.noRewards')+'</span>')+'</div></div>';
    }).join('');
  } else { mc.hidden=true; ml.innerHTML=''; }

  // Zone leveling notes (from /api/zone): title + note text, hidden when there's nothing to show.
  const zn=$('#zoneNotes');
  if(zone && (zone.notes||'').trim()){
    zn.hidden=false;
    zn.innerHTML='<div class="zt">'+esc(zone.title||zone.name||'')+'</div>'+esc(zone.notes);
  } else { zn.hidden=true; }
}

// Update banner: show a download link if a newer version exists on GitHub (best-effort).
async function checkVersion(){
  try{
    const v=await getJSON('/api/version');
    if(v && v.updateAvailable){
      const b=$('#updateBanner'); if(!b) return;
      const m=$('#updateMsg'); if(m) m.textContent=' — '+(v.latest||'')+' (you have v'+(v.current||'?')+')';
      b.href=v.url||'#'; b.hidden=false; b.style.display='flex';
    }
  }catch(e){}
}

wireSettings(); wireHpBars(); wireTerrain(); wireGround(); wireHover(); wireMono(); wireExchange(); wireZoom();
loadIcons().then(()=>{ loadSettings(); loadFilters(); }); // Rules is the default tab
tick(); setInterval(tick, 1000);
checkVersion();
</script>
</body>
</html>
""";
}
