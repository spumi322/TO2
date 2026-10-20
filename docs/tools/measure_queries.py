"""Counts EF DB round trips (Executed DbCommand log lines) and response size per API request.

Usage: start the API with its console output going to a file, then
    python -I docs/tools/measure_queries.py <api-log-file> [teams groups advancing]
Example (Git Bash):
    cd src/webAPI && dotnet run > "$TEMP/to2-api.log" 2>&1            # terminal 1
    python -I docs/tools/measure_queries.py "$TEMP/to2-api.log" 32 8 2  # terminal 2, from repo root
Creates a throwaway tenant "VerTenantvtest..." with a GroupsAndBracket tournament and plays it to the end.
"""
import json, random, sys, time, urllib.request, urllib.error
from collections import OrderedDict

LOG = sys.argv[1]
TEAMS, GROUPS, ADV = (int(x) for x in sys.argv[2:5]) if len(sys.argv) > 4 else (16, 4, 2)
BASE = "http://localhost:5161/api"
token = None
rows = []


def log_lines():
    with open(LOG, encoding="utf-8", errors="replace") as f:
        return f.readlines()


def call(label, method, path, body=None):
    before = len(log_lines())
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read()
    except urllib.error.HTTPError as e:
        sys.exit(f"{method} {path} -> {e.code}: {e.read().decode()[:400]}")
    time.sleep(0.3)
    new = log_lines()[before:]
    q = sum(1 for l in new if "Executed DbCommand" in l)
    rows.append({"label": label, "q": q, "bytes": len(raw)})
    return json.loads(raw) if raw else None


u = f"vtest{random.randint(0, 10**9)}"
token = call("register", "POST", "/auth/register",
             {"userName": u, "email": f"{u}@example.com", "password": "Passw0rd!", "tenantName": f"VerTenant{u}"})["accessToken"]

for i in range(4):
    call("POST /tournaments (other)", "POST", "/tournaments", {"name": f"Other Cup {i}", "description": "d", "maxTeams": 8, "format": 1})
tid = call("POST /tournaments", "POST", "/tournaments",
           {"name": "Measure Cup", "description": "d", "maxTeams": TEAMS, "format": 2, "numberOfGroups": GROUPS, "advancingPerGroup": ADV})["id"]

call("POST /tournaments/{id}/teams (bulk)", "POST", f"/tournaments/{tid}/teams", {"names": [f"T{i}" for i in range(TEAMS)]})

for label, path in [("GET /tournaments (list)", "/tournaments"),
                    ("GET /tournaments/{id}", f"/tournaments/{tid}"), ("GET /tournaments/{id}/teams", f"/tournaments/{tid}/teams"),
                    ("GET /tournaments/{id}/state", f"/tournaments/{tid}/state")]:
    call(label, "GET", path)

call("POST start-groups", "POST", f"/tournaments/{tid}/start-groups")
groups = call("GET /standings/{id}/groups", "GET", f"/standings/{tid}/groups")


def score(kind, standing_id, m, game):
    r = call("tmp", "PUT", f"/matches/{m['id']}/games/{game['id']}/result",
             {"gameId": game["id"], "winnerId": m["teamAId"], "matchId": m["id"], "standingId": standing_id,
              "tournamentId": tid, "version": game["version"]})
    flag = ("[tournament done]" if r.get("tournamentFinished") else "[all groups done]" if r.get("allGroupsFinished")
            else "[group done]" if r.get("standingFinished") else "[match done]" if r.get("matchFinished") else "[game only]")
    rows[-1]["label"] = f"PUT game result ({kind}) {flag}"
    return r


for g in groups:
    for m in g["matches"]:
        for game in m["games"][:2]:
            score("group", g["id"], m, game)

call("GET /standings/{id}/groups (finished)", "GET", f"/standings/{tid}/groups")
call("POST start-bracket", "POST", f"/tournaments/{tid}/start-bracket")

done = False
while not done:
    br = call("GET /standings/{id}/bracket", "GET", f"/standings/{tid}/bracket")
    ready = [m for m in br["matches"] if not m["winnerId"] and m["teamAId"] and m["teamBId"] and m["games"]]
    if not ready:
        break
    for m in ready:
        for game in m["games"][:2]:
            if score("bracket", br["id"], m, game).get("tournamentFinished"):
                done = True

call("GET final-standings", "GET", f"/tournaments/{tid}/final-standings")
call("GET /tournaments (list, end)", "GET", "/tournaments")
call("GET /tournaments/{id}/state", "GET", f"/tournaments/{tid}/state")

agg = OrderedDict()
for r in rows:
    agg.setdefault(r["label"], []).append(r)
print(f"{'request':48} {'calls':>5} {'qmin':>5} {'qmax':>5} {'qavg':>6} {'bytesMax':>9}")
for label, rs in agg.items():
    qs = [r["q"] for r in rs]
    print(f"{label:48} {len(rs):5} {min(qs):5} {max(qs):5} {sum(qs)/len(qs):6.1f} {max(r['bytes'] for r in rs):9}")
print(f"tenant=VerTenant{u} tid={tid} teams={TEAMS} groups={GROUPS}")
