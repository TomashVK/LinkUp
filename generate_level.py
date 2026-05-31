#!/usr/bin/env python3
"""
Level generator for Uno Associations.
Run from the project root: python3 generate_level.py

Mechanics accounted for:
  - Cards drawn from deck land in the reveal pile (costs 1 move each).
  - Cards played to the active slot cost 1 move (from hand or pile).
  - Taking a card from the pile to hand costs 1 move.
  - Restarting an empty deck costs 1 move.
  - Optimal path never draws unnecessary cards, so min_moves =
      (hand cards in chain × 1) + (deck cards in chain × 2).

Questions asked at runtime:
  1. Total cards in level       — how many cards appear in the whole puzzle
  2. Cards in starting hand     — how many the player holds at the start
  3. Minimum branch points      — fewest wrong-path traps acceptable
  4. Maximum branch points      — most wrong-path traps acceptable (99 = unlimited)
  5. Move buffer                — extra moves above optimal before continue panel triggers;
                                  also sets the gap between star thresholds
  6. Number of variants         — how many shuffled versions of the level to generate
"""

import json, random, re, sys
from pathlib import Path

DATA_DIR   = Path(__file__).parent / "Assets/Resources/Data"
LEVELS_DIR = DATA_DIR / "Levels"



# ─── Data loading ────────────────────────────────────────────────────────────

def load_json(path):
    with open(path) as f:
        return json.load(f)


def build_graph(cards, tag_rules, connections):
    """Returns {card_id: {neighbor_id: strength}}."""
    tag_map = {c["id"]: set(c.get("tags", [])) for c in cards}
    graph   = {c["id"]: {} for c in cards}

    ids = list(graph)
    for i in range(len(ids)):
        for j in range(i + 1, len(ids)):
            a, b = ids[i], ids[j]
            ta, tb = tag_map[a], tag_map[b]
            best = 0.0
            for rule in tag_rules:
                t1, t2 = rule["tag1"], rule["tag2"]
                if (t1 in ta and t2 in tb) or (t1 in tb and t2 in ta):
                    best = max(best, rule["strength"])
            if best > 0:
                graph[a][b] = best
                graph[b][a] = best

    for c in connections:
        a, b = c["card1"], c["card2"]
        if a in graph and b in graph:
            graph[a][b] = c["strength"]
            graph[b][a] = c["strength"]

    return graph


# ─── Chain finding ────────────────────────────────────────────────────────────

def find_chain(graph, all_ids, start, length, attempts=400):
    """
    DFS with backtracking: find a random path of exactly `length` cards
    starting from `start`.  Returns the path list, or None on failure.
    """
    all_set = set(all_ids)

    for _ in range(attempts):
        path    = [start]
        visited = {start}

        def dfs():
            if len(path) == length:
                return True
            cur       = path[-1]
            neighbors = [n for n in graph.get(cur, {}) if n in all_set and n not in visited]
            random.shuffle(neighbors)
            for n in neighbors:
                path.append(n);    visited.add(n)
                if dfs():
                    return True
                path.pop();        visited.remove(n)
            return False

        if dfs():
            return path

    return None


# ─── Move cost calculation ────────────────────────────────────────────────────

def min_moves_for_chain(chain, hand_set):
    """
    Minimum moves to empty hand_set while following chain.
    Each hand card played costs 1 move.
    Each deck card drawn+played costs 2 moves (draw to pile + play from pile).
    """
    chain_pos = {c: i for i, c in enumerate(chain)}
    last = max((chain_pos[c] for c in hand_set if c in chain_pos), default=0)
    optimal = chain[1:last + 1]
    hand_plays = sum(1 for c in optimal if c in hand_set)
    deck_plays = sum(1 for c in optimal if c not in hand_set)
    return hand_plays + deck_plays * 2


def deck_draws_needed(chain, hand_set):
    """Number of deck cards that must be drawn in the optimal path (for Rule 1)."""
    chain_pos = {c: i for i, c in enumerate(chain)}
    last = max((chain_pos[c] for c in hand_set if c in chain_pos), default=0)
    return sum(1 for c in chain[1:last + 1] if c not in hand_set)


# ─── Variant helpers ──────────────────────────────────────────────────────────

def make_variant(chain, hand_size):
    """
    Randomly split chain[1:] into hand / deck.
    Returns (hand, deck, min_moves) or None.
    """
    playable = chain[1:]
    if hand_size > len(playable):
        return None

    chain_pos = {c: i for i, c in enumerate(chain)}
    sorted_hand_key = lambda c: chain_pos.get(c, 0)

    for _ in range(800):
        shuffled = list(playable)
        random.shuffle(shuffled)
        hand     = shuffled[:hand_size]
        hand_set = set(hand)
        draws    = deck_draws_needed(chain, hand_set)

        if draws < 1:
            continue   # Rule 1: at least one deck draw required

        # Rule 2: hand must not be in solve order
        sorted_hand = sorted(hand, key=sorted_hand_key)
        for _ in range(50):
            random.shuffle(hand)
            if hand != sorted_hand:
                break
        else:
            continue   # couldn't unsort — skip this split

        deck      = [c for c in playable if c not in hand_set]
        min_moves = min_moves_for_chain(chain, hand_set)
        return hand, deck, min_moves

    return None


def make_path_desc(chain, hand_set):
    parts = [chain[0]]
    for c in chain[1:]:
        if c not in hand_set:
            parts.append(f"[draw {c}]")
        parts.append(c)
    return " → ".join(parts)


# ─── Trap detection ───────────────────────────────────────────────────────────

def find_branch_points(graph, chain):
    """
    Returns a list of (position, active_card, correct_next, [wrong_alternatives]).
    """
    pool_set = set(chain)
    branches = []
    for i in range(len(chain) - 1):
        active  = chain[i]
        correct = chain[i + 1]
        wrongs  = [c for c in graph.get(active, {}) if c in pool_set and c != correct and c != active]
        if wrongs:
            branches.append((i, active, correct, wrongs))
    return branches


# ─── JSON formatting ─────────────────────────────────────────────────────────

def format_level_json(data):
    """Serialize level data with primitive arrays collapsed onto one line."""
    s = json.dumps(data, indent=2, ensure_ascii=False)
    def collapse(m):
        inner = re.sub(r'\s+', ' ', m.group(1).strip())
        return '[' + inner + ']'
    return re.sub(r'\[\s*((?:[^\[\]{}])*?)\s*\]', collapse, s, flags=re.DOTALL)


# ─── Next level number ────────────────────────────────────────────────────────

def next_level_id():
    i = 1
    while (LEVELS_DIR / f"level_{i}.json").exists():
        i += 1
    return i


# ─── Input helpers ────────────────────────────────────────────────────────────

def prompt_int(label, lo=1, hi=999):
    while True:
        try:
            v = int(input(f"{label}: ").strip())
            if lo <= v <= hi:
                return v
            print(f"  Enter a number between {lo} and {hi}.")
        except ValueError:
            print("  Enter a valid integer.")


# ─── Main ─────────────────────────────────────────────────────────────────────

def main():
    print("=== Uno Associations — Level Generator ===\n")
    print("Tip: deck size = total cards − hand size. Hand must have at least 1 card;\n"
          "     deck must have at least 1 card (so hand < total).\n")

    total_cards  = prompt_int("1. Total cards in level (4–20)", 4, 20)
    hand_size    = prompt_int(f"2. Cards in starting hand (1–{total_cards - 1})", 1, total_cards - 1)
    min_branches = prompt_int("3. Min branch points / traps (0–10)", 0, 10)
    max_branches = prompt_int(f"4. Max branch points / traps ({min_branches}–99, or 99 for unlimited)",
                              min_branches, 99)
    buffer       = prompt_int("5. Move buffer — extra moves above optimal before continue panel "
                              "triggers (1–15; lower = harder star thresholds)", 1, 15)
    num_variants = prompt_int("6. Number of variants to generate (1–6)", 1, 6)

    deck_size = total_cards - hand_size

    print(f"\n{total_cards} cards ({hand_size} hand + {deck_size} deck), "
          f"{min_branches}–{max_branches} branch points, "
          f"move buffer ×{buffer}")

    level_id = next_level_id()
    print(f"Generating level {level_id} with {num_variants} variant(s)...\n")

    cards     = load_json(DATA_DIR / "cards.json")
    tag_rules = load_json(DATA_DIR / "tag-rules.json")
    conns     = load_json(DATA_DIR / "connections.json")
    graph     = build_graph(cards, tag_rules, conns)
    all_ids   = [c["id"] for c in cards]

    # ── Find the card pool ────────────────────────────────────────────────────
    seed_chain  = None
    best_approx = None

    for attempt in range(80):
        start = random.choice(all_ids)
        c = find_chain(graph, all_ids, start, total_cards + 1)
        if c is None or len(c) < total_cards + 1:
            continue
        b = len(find_branch_points(graph, c))
        if min_branches <= b <= max_branches:
            seed_chain = c
            break
        if b >= min_branches and best_approx is None:
            best_approx = c

    if seed_chain is None:
        if best_approx is not None:
            b = len(find_branch_points(graph, best_approx))
            print(f"  Note: could not find a chain within {min_branches}–{max_branches} "
                  f"branch points; using one with {b} instead.")
            seed_chain = best_approx
        else:
            print(f"\nCould not find a chain with at least {min_branches} branch points.")
            print("Try fewer total cards, a smaller hand, or fewer required branch points.")
            sys.exit(1)

    pool     = seed_chain
    pool_set = set(pool)
    print(f"Card pool ({len(pool)} cards): {', '.join(pool)}\n")

    # ── Build variants ────────────────────────────────────────────────────────
    variants     = []
    used_actives = set()

    for v in range(num_variants):
        if v == 0:
            active = pool[0]
        else:
            candidates = [c for c in pool_set if c not in used_actives]
            active = random.choice(candidates) if candidates else random.choice(list(pool_set))

        chain = find_chain(graph, list(pool_set), active, len(pool_set))
        if chain is None or len(chain) < len(pool_set):
            print(f"  Variant {v + 1}: could not find a full chain from '{active}', skipping.")
            continue

        used_actives.add(active)
        branches = find_branch_points(graph, chain)

        result = make_variant(chain, hand_size)
        if result is None:
            print(f"  Variant {v + 1}: FAILED — could not satisfy constraints, skipping.")
            continue

        hand, deck, min_moves = result
        hand_set = set(hand)

        three_star = min_moves
        two_star   = min_moves + buffer
        max_moves  = min_moves + buffer * 2

        chain_pos     = {c: i for i, c in enumerate(chain)}
        last_hand_pos = max(chain_pos[c] for c in hand_set)
        optimal_chain = chain[:last_hand_pos + 1]

        path_desc  = make_path_desc(optimal_chain, hand_set)
        worst_path = " → ".join(chain) + f"  ({len(deck)} draws, {max_moves} moves)"

        branch_summary = ", ".join(
            f"{ac}→{correct} (trap: {'/'.join(wrongs)})"
            for _, ac, correct, wrongs in branches
        ) if branches else "none"

        print(f"  Variant {v + 1}: active={active}  hand={hand}  deck={deck}")
        print(f"    minMoves={min_moves}  3★≤{three_star}  2★≤{two_star}  maxMoves={max_moves}"
              f"  branches=[{branch_summary}]")

        variants.append({
            "activeCard":      active,
            "hand":            hand,
            "deck":            deck,
            "maxMoves":        max_moves,
            "threeStarMoves":  three_star,
            "twoStarMoves":    two_star,
            "optimalPath":     list(optimal_chain),
            "pathDescription": path_desc,
            "worstPath":       worst_path,
        })

    if not variants:
        print("\nNo variants generated.  Try fewer total cards or a smaller hand size.")
        sys.exit(1)

    out_path = LEVELS_DIR / f"level_{level_id}.json"
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(format_level_json(variants))

    print(f"\n✓  Written to {out_path}")


if __name__ == "__main__":
    main()
