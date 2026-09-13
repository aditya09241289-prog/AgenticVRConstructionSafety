from pathlib import Path
import json
from http.server import BaseHTTPRequestHandler, HTTPServer

BASE = Path(__file__).resolve().parent
METRICS = BASE / "metrics_output" / "training_metrics.json"

def load():
    if not METRICS.exists():
        return {"error": "Run python metrics.py first."}
    try:
        return json.loads(METRICS.read_text(encoding="utf-8"))
    except Exception as e:
        return {"error": str(e)}

def pct(x):
    return f"{x*100:.1f}%"

def render():
    m = load()
    if "error" in m:
        return f"<html><body style='font-family:Arial;background:#071018;color:white;padding:40px'><h2>Dashboard</h2><p>{m['error']}</p></body></html>"

    hazards = m.get("hazard_metrics", {})
    rows = ""
    for h in ("fall","struck_by","electrical"):
        d = hazards.get(h)
        if d:
            rows += f"""<tr><td>{h.replace("_"," ").title()}</td>
            <td><b>{pct(d.get("accuracy",0))}</b></td>
            <td>{d.get("attempts",0)}</td>
            <td>{d.get("average_response_time_ms",0):,.0f} ms</td></tr>"""

    actions = "".join(
        f"<div class='stat'><span>{a.replace('_',' ').title()}</span><b>{n}</b></div>"
        for a,n in m.get("agent_actions",{}).items()
    )

    progression = ""
    for i,e in enumerate(m.get("difficulty_progression",[])[-12:],1):
        result = "SUCCESS" if e.get("success") is True else "FAILURE"
        cls = "ok" if result=="SUCCESS" else "bad"
        progression += f"""<tr><td>{i}</td>
        <td>{str(e.get("hazard_type","")).replace("_"," ").title()}</td>
        <td>L{e.get("difficulty","-")}</td>
        <td class="{cls}">{result}</td></tr>"""

    return f"""<!doctype html><html><head><meta charset="utf-8">
    <meta http-equiv="refresh" content="15">
    <title>Adaptive Construction Safety AI</title>
    <style>
    *{{box-sizing:border-box}} body{{margin:0;background:#071018;color:#eaf2f8;font-family:Segoe UI,Arial,sans-serif}}
    .top{{padding:32px 42px;border-bottom:1px solid #1b2a36;display:flex;justify-content:space-between}}
    .eyebrow{{color:#58d6c4;font-size:11px;letter-spacing:2px;font-weight:700}}
    h1{{margin:7px 0;font-size:30px}} p,.muted{{color:#8ea4b5}}
    button{{background:#10202c;color:white;border:1px solid #294454;border-radius:8px;padding:10px 15px}}
    .cards{{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;padding:24px 42px 0}}
    .card{{background:#0c1720;border:1px solid #1a2b37;border-radius:14px;padding:21px}}
    .label{{font-size:10px;color:#7d95a6;letter-spacing:1.4px;font-weight:700}}
    .big{{font-size:30px;font-weight:800;margin:9px 0 3px}}
    .grid{{display:grid;grid-template-columns:2fr 1fr;gap:16px;padding:16px 42px 30px}}
    .title{{font-weight:800;margin-bottom:16px}} table{{width:100%;border-collapse:collapse;font-size:13px}}
    th{{color:#718898;font-size:10px;text-transform:uppercase;text-align:left;padding:0 8px 11px}}
    td{{border-top:1px solid #172731;padding:13px 8px}}
    .stat{{display:flex;justify-content:space-between;padding:11px 0;border-bottom:1px solid #172731;color:#8ea4b5}}
    .stat b{{color:white}} .ok{{color:#61ddb5;font-weight:800}} .bad{{color:#ff8b8b;font-weight:800}}
    footer{{padding:0 42px 30px;color:#526b7a;font-size:11px}}
    @media(max-width:900px){{.cards{{grid-template-columns:repeat(2,1fr)}}.grid{{grid-template-columns:1fr}}}}
    </style></head><body>
    <div class="top"><div><div class="eyebrow">MITACS RESEARCH PROTOTYPE</div>
    <h1>Adaptive Construction Safety AI</h1>
    <p>Unity → telemetry → learner model → adaptive agent → next scenario</p></div>
    <button onclick="location.reload()">↻ Refresh</button></div>

    <div class="cards">
    <div class="card"><div class="label">SUCCESS RATE</div><div class="big">{pct(m.get("overall_success_rate",0))}</div><div class="muted">latest session</div></div>
    <div class="card"><div class="label">MAX DIFFICULTY</div><div class="big">L{m.get("maximum_difficulty",0)}</div><div class="muted">observed</div></div>
    <div class="card"><div class="label">ADAPTIVE DECISIONS</div><div class="big">{m.get("adaptive_agent_decisions",0)}</div><div class="muted">agent decisions</div></div>
    <div class="card"><div class="label">AVG RESPONSE</div><div class="big">{m.get("average_response_time_ms",0):,.0f} ms</div><div class="muted">learner responses</div></div>
    </div>

    <div class="grid">
    <div class="card"><div class="title">Hazard Performance</div>
    <table><thead><tr><th>Hazard</th><th>Accuracy</th><th>Attempts</th><th>Avg Response</th></tr></thead>
    <tbody>{rows}</tbody></table></div>

    <div class="card"><div class="title">Agent Behaviour</div>
    <div class="stat"><span>Remediation</span><b>{m.get("remediation_instructions",0)}</b></div>
    <div class="stat"><span>Difficulty increases</span><b>{m.get("difficulty_increase_decisions",0)}</b></div>
    <div class="stat"><span>Scenario advances</span><b>{m.get("scenario_advances",0)}</b></div>
    <div class="stat"><span>Average difficulty</span><b>{m.get("average_difficulty",0):.2f}</b></div>
    <div class="title" style="margin-top:22px">Decision Distribution</div>{actions}</div>

    <div class="card"><div class="title">Learner Progression</div>
    <table><thead><tr><th>#</th><th>Hazard</th><th>Level</th><th>Outcome</th></tr></thead>
    <tbody>{progression}</tbody></table></div>

    <div class="card"><div class="title">Research Loop</div>
    <p>UNITY SCENARIO</p><p>↓</p><p>LEARNER TELEMETRY</p><p>↓</p><p>LEARNER MODEL</p><p>↓</p><p>ADAPTIVE POLICY</p><p>↓</p><p>NEXT SCENARIO</p></div>
    </div>
    <footer>Development/pilot metrics — not a research efficacy claim. Session {m.get("number_of_sessions",0)} · {m.get("total_learner_events",0)} learner events.</footer>
    </body></html>"""

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        data = render().encode()
        self.send_response(200)
        self.send_header("Content-Type","text/html; charset=utf-8")
        self.send_header("Content-Length",str(len(data)))
        self.end_headers()
        self.wfile.write(data)
    def log_message(self, *args):
        pass

if __name__ == "__main__":
    print("Dashboard: http://127.0.0.1:8050")
    print("Press Ctrl+C to stop.")
    HTTPServer(("127.0.0.1",8050),Handler).serve_forever()
