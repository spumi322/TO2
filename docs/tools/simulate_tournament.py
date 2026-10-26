"""Plays one tournament (16 teams, 4 groups) through the API and writes the results + DB rows to a text file.

GroupsAndBracket (2 advance per group) by default, GroupsOnly with the "groups-only" argument.
Teams are S01..S16; the lower number always wins 2-0, so the outcome only depends on the random group draw.
"tiebreak": group matches follow TIEBREAK_SCRIPT instead (A..D = the group's teams by name, A = lowest team id),
so A, B and C all win 2 matches. Expected group order: B, C, A, D
(A 4-3 games: game difference puts A, the lowest id, 3rd; B and C both 2-1 with games 5-3: the lower team id puts B 1st).
Creates a throwaway tenant "SimTenantsim..." each run.

Usage: start the API (http://localhost:5161) with the local docker DB (container to2-postgres), then
    python -I docs/tools/simulate_tournament.py <out-file> [groups-only] [tiebreak]
Example (Git Bash, from repo root):
    cd src/webAPI && dotnet run                                                    # terminal 1
    python -I docs/tools/simulate_tournament.py "$TEMP/sim.txt" && cat "$TEMP/sim.txt"   # terminal 2
"""
import json, random, subprocess, sys, urllib.request, urllib.error

BASE = "http://localhost:5161/api"
OUT = sys.argv[1]
GROUPS_ONLY = "groups-only" in sys.argv[2:]
TIEBREAK = "tiebreak" in sys.argv[2:]
# group match -> (winner, games won by the loser)
TIEBREAK_SCRIPT = {"AB": ("A", 1), "BC": ("B", 1), "AC": ("C", 0), "AD": ("A", 0), "BD": ("B", 0), "CD": ("C", 1)}
token = None


def call(method, path, body=None):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read()
    except urllib.error.HTTPError as e:
        sys.exit(f"{method} {path} -> {e.code}: {e.read().decode()[:400]}")
    return json.loads(raw) if raw else None


def sql(q):
    r = subprocess.run(["docker", "exec", "to2-postgres", "psql", "-U", "to2user", "-d", "TO2Database", "-A", "-F", " | ", "-c", q],
                       capture_output=True, text=True, check=True)
    return r.stdout.strip()


u = f"sim{random.randint(0, 10**9)}"
token = call("POST", "/auth/register", {"userName": u, "email": f"{u}@example.com", "password": "Passw0rd!",
                                        "tenantName": f"SimTenant{u}"})["accessToken"]
setup = ({"format": 3, "numberOfGroups": 4} if GROUPS_ONLY
         else {"format": 2, "numberOfGroups": 4, "advancingPerGroup": 2})
tid = call("POST", "/tournaments", {"name": "Sim Cup", "description": "d", "maxTeams": 16, **setup})["id"]
teams = call("POST", f"/tournaments/{tid}/teams", {"names": [f"S{i:02d}" for i in range(1, 17)]})
name = {t["id"]: t["name"] for t in teams}


def play(standing_id, m, winner=None, loser_games=0):
    if winner is None:
        winner = min(m["teamAId"], m["teamBId"], key=lambda t: name[t])  # S01 beats S02 ...
    loser = m["teamBId"] if winner == m["teamAId"] else m["teamAId"]
    game_winners = [winner, loser, winner] if loser_games == 1 else [winner, winner]
    for game, game_winner in zip(m["games"], game_winners):
        r = call("PUT", f"/matches/{m['id']}/games/{game['id']}/result",
                 {"gameId": game["id"], "winnerId": game_winner, "matchId": m["id"], "standingId": standing_id,
                  "tournamentId": tid, "version": game["version"]})
    return r


def play_scripted(standing_id, m, group_names):
    label = {t: "ABCD"[group_names.index(name[t])] for t in (m["teamAId"], m["teamBId"])}
    winner_label, loser_games = TIEBREAK_SCRIPT["".join(sorted(label.values()))]
    winner = next(t for t, l in label.items() if l == winner_label)
    return play(standing_id, m, winner, loser_games)


finished, last = False, {}
call("POST", f"/tournaments/{tid}/start-groups")
for g in call("GET", f"/standings/{tid}/groups"):
    group_names = sorted(t["name"] for t in g["teams"])
    for m in g["matches"]:
        r = play_scripted(g["id"], m, group_names) if TIEBREAK else play(g["id"], m)
        if r.get("tournamentFinished"):
            finished, last = True, r

if not GROUPS_ONLY:
    call("POST", f"/tournaments/{tid}/start-bracket")
while not finished:
    br = call("GET", f"/standings/{tid}/bracket")
    ready = [m for m in br["matches"] if not m["winnerId"] and m["teamAId"] and m["teamBId"]]
    if not ready:
        break
    for m in ready:
        r = play(br["id"], m)
        if r.get("tournamentFinished"):
            finished, last = True, r

final = call("GET", f"/tournaments/{tid}/final-standings")
group_tables = call("GET", f"/standings/{tid}/groups")
state = call("GET", f"/tournaments/{tid}/state")

with open(OUT, "w", encoding="utf-8") as f:
    w = lambda s="": f.write(s + "\n")
    w(f"tournament {tid}, finished flag from last result: {finished}, state: {state['status'] if 'status' in state else state}")
    w("\n== API final-standings (team | placement | status | eliminationRound)")
    for p in final:
        w(" | ".join([name.get(p['teamId'], str(p['teamId']))] + [f"{k}={v}" for k, v in p.items() if k not in ("teamId", "teamName")]))
    w("\n== Last game result response: finalStandings (pipeline, before commit)")
    for p in last.get("finalStandings") or []:
        w(" | ".join([name.get(p['teamId'], str(p['teamId']))] + [f"{k}={v}" for k, v in p.items() if k not in ("teamId", "teamName")]))
    w("\n== API group tables, in returned order (team W-L, games, status)")
    for g in group_tables:
        w(f"{g['name']}: " + ", ".join(f"{t['name']} {t['wins']}-{t['losses']} g{t['gameWins']}-{t['gameLosses']} s{t['status']}"
                                     for t in g["teams"]))
    w("\n== DB Tournaments (Status)")
    w(sql(f'SELECT "Status" FROM "Tournaments" WHERE "Id" = {tid}'))
    w("\n== DB Standings")
    w(sql(f'SELECT "Name", "StandingType", "IsSeeded", "IsFinished" FROM "Standings" WHERE "TournamentId" = {tid} ORDER BY "Id"'))
    w("\n== DB GroupEntries")
    w(sql(f'SELECT s."Name" AS grp, g."TeamName", g."Wins", g."Losses", g."Points", g."GameWins", g."GameLosses", g."Status", g."Eliminated" FROM "GroupEntries" g '
          f'JOIN "Standings" s ON s."Id" = g."StandingId" WHERE g."TournamentId" = {tid} ORDER BY s."Id", g."Points" DESC, g."TeamName"'))
    w("\n== DB TournamentTeams (team | FinalPlacement | EliminatedInRound | finalized)")
    w(sql(f'SELECT t."Name", tt."FinalPlacement", tt."EliminatedInRound", tt."ResultFinalizedAt" IS NOT NULL AS finalized '
          f'FROM "TournamentTeams" tt JOIN "Teams" t ON t."Id" = tt."TeamId" WHERE tt."TournamentId" = {tid} '
          f'ORDER BY tt."FinalPlacement" NULLS LAST, t."Name"'))
    w("\n== DB bracket Matches (round | seed | teamA | teamB | winner)")
    w(sql(f'SELECT m."Round", m."Seed", a."Name", b."Name", wt."Name" FROM "Matches" m JOIN "Standings" s ON s."Id" = m."StandingId" '
          f'LEFT JOIN "Teams" a ON a."Id" = m."TeamAId" LEFT JOIN "Teams" b ON b."Id" = m."TeamBId" LEFT JOIN "Teams" wt ON wt."Id" = m."WinnerId" '
          f'WHERE s."TournamentId" = {tid} AND s."StandingType" = 2 ORDER BY m."Round", m."Seed"'))
print(f"done: tournament {tid} -> {OUT}")
