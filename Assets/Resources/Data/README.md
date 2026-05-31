# Game Data — Structure & Reference

This folder contains all the data files that drive the game's cards, connections, levels, and tag system. Here's how everything fits together.

---

## Overview

The game needs to know two things at runtime:
1. **Which cards can be played on which other cards** (the connection graph)
2. **What cards, hand, and deck each level starts with** (level definitions)

Both are built from the JSON files in this folder.

---

## Files

### `cards.json`

The master list of every card in the game. Each card looks like this:

```json
{
  "id":       "bee",
  "text":     "Bee",
  "image":    "bee",
  "category": "wildlife",
  "tags":     ["insect", "pollinator", "flier", "colony"]
}
```

| Field      | Purpose |
|------------|---------|
| `id`       | Internal key. Every other file refers to cards by this string. Must be unique and lowercase. |
| `text`     | What the player sees on the card face. |
| `image`    | Sprite name used to look up the card's artwork. |
| `category` | Broad grouping: `pet`, `wildlife`, `nature`, or `food`. |
| `tags`     | Semantic traits. These power the auto-connection system (see below). |

---

### `tag-rules.json`

Rules that **automatically create connections** between cards based on their tags.

```json
{"tag1": "pollinator", "tag2": "blooming", "strength": 0.90}
```

This single rule makes every `pollinator` card connect to every `blooming` card. So `bee` (pollinator) automatically connects to `flower`, `bush`, and `berry` (all blooming) — without writing those pairs anywhere.

Rules are symmetric: `tag1 ↔ tag2` works in both directions. A rule can also match a tag with itself:

```json
{"tag1": "pet", "tag2": "pet", "strength": 0.85}
```

This makes all pets connect to each other (dog ↔ cat).

**Strength** is a 0–1 value. Higher = stronger association. The game uses this to decide whether a drop is accepted (any connection exists) and could use it in future for scoring or visual feedback.

---

### `connections.json`

Explicit card-to-card connections. These either **add** connections that tags can't express, or **override** the strength of an auto-generated one.

```json
{"card1": "cat", "card2": "milk", "strength": 0.90}
```

Tags can't capture cultural knowledge like *"cats drink milk"* or *"mice love cheese"* — there's no tag on `cat` that implies dairy. Those go here.

If a pair already has an auto-generated connection from a tag rule, the explicit entry in this file wins (it replaces the tag-generated strength).

Like tag rules, all entries are symmetric: writing `cat ↔ milk` once covers both directions.

---

### `tags.json`

A reference file. **Does not affect gameplay.** It documents every tag that exists in the system.

```json
{"id": "pollinator", "label": "Pollinator", "group": "animal-behavior", "hasRule": true}
{"id": "mammal",     "label": "Mammal",     "group": "animal-type",     "hasRule": false}
```

| Field     | Purpose |
|-----------|---------|
| `id`      | Exact string used in `cards.json` tags arrays and `tag-rules.json`. |
| `label`   | Human-readable display name. |
| `group`   | Semantic category for organisation (see groups below). |
| `hasRule` | Whether this tag appears in at least one rule in `tag-rules.json`. |

**`hasRule` explained:** A tag with `hasRule: false` exists on cards but has no matching rule, so it never auto-generates a connection. `mammal` appears on dog, cat, wolf, bear, and more — but there is no `mammal ↔ mammal` rule, so two mammals don't auto-connect just because they're both mammals. These tags are descriptive metadata for now; adding a rule to `tag-rules.json` is all it takes to activate them.

#### Tag groups

| Group             | What belongs here |
|-------------------|-------------------|
| `animal-behavior` | How the animal acts: predator, prey, flier, swimmer, pollinator… |
| `animal-type`     | What kind of creature: mammal, insect, amphibian, rodent… |
| `diet`            | What it eats: carnivore, herbivore, omnivore |
| `domestication`   | Relationship to humans: pet, domestic |
| `food`            | Food properties: dairy, sweet, fruit, plant-food… |
| `habitat`         | Where it lives: forest-animal, aquatic, wetland, underground… |
| `lifecycle`       | Reproduction: egg-layer |
| `nature`          | Environments and natural features: forest, shelter, water… |
| `plant`           | Plant properties: blooming, tree-part, ground, seed… |
| `weather`         | Atmospheric: weather, sky, precipitation, winter… |

---

### `Levels/level_1.json`, `level_2.json`, …

Each level lives in its own file and contains an **array of variants**. When the level loads, one variant is picked at random. If the player restarts, they may get a different variant — same card pool, different arrangement.

The loader reads `level_1`, `level_2`, … in order until a file is missing, so adding a new level is just dropping in the next numbered file.

```json
[
  {
    "activeCard":        "cat",
    "hand":              ["bee", "rain", "fish"],
    "deck":              ["river", "flower", "cloud", "sun", "honey"],
    "threeStarMaxDraws": 2,
    "twoStarMaxDraws":   4
  },
  {
    "activeCard":        "cat",
    "hand":              ["flower", "fish", "bee"],
    "deck":              ["river", "rain", "cloud", "sun", "honey"],
    "threeStarMaxDraws": 2,
    "twoStarMaxDraws":   4
  }
]
```

| Field               | Purpose |
|---------------------|---------|
| `activeCard`        | The card that starts face-up in the centre slot. |
| `hand`              | Cards dealt to the player's hand at the start. |
| `deck`              | Remaining draw pile (top → bottom order). |
| `threeStarMaxDraws` | Draw count at or below this gets 3 stars. |
| `twoStarMaxDraws`   | Draw count at or below this gets 2 stars. Anything above gets 1 star. |

All card ids in a level definition must exist in `cards.json`.

---

## Level Design Rules

Follow these rules when designing any level variant:

1. **The level must require at least 1 draw to win.** A player should never be able to clear their hand using only the starting cards — drawing is the mechanic that teaches card associations.

2. **Hand cards must not be in solve order.** If the optimal chain is A→B→C, the hand should not contain A, B, C in that sequence. Shuffle the hand so players must figure out the order themselves.

3. **Deck must be larger than hand.** More cards to draw than cards in hand. A ratio of roughly 4–6 deck cards to 2–4 hand cards works well.

4. **Star thresholds must reflect actual minimum draws.** If the optimal solution requires 2 draws, set `threeStarMaxDraws` to 2. Don't reward perfection by setting it to 0 when 2 draws are unavoidable.

5. **All variants use the same card pool.** Variants in one level file should use the same set of cards, just arranged differently (different active card, different hand/deck split). This keeps the "same level, different layout" feel on restart.

6. **Verify every variant is solvable.** Before committing, trace the full optimal path and confirm it reaches 0 hand cards. Also confirm no other play order leads to an unsolvable state before the deck is empty — or if it does, make sure that's intentional (a trap).

7. **Design at least one trap per level (optional but recommended).** A trap is a tempting first move that seems correct but strands a card permanently. The best levels teach the player something when they fall into it.

---

## How connections are built at runtime

```
cards.json  ──┐
              ├──▶  auto-generated edges (tag rules applied to every card pair)
tag-rules.json┘
                  + connections.json overrides
                  ═══════════════════════════
                  = ConnectionGraph in memory
```

1. For every pair of cards, find the highest-strength tag rule where one card has `tag1` and the other has `tag2`. If one matches, add that edge.
2. For every entry in `connections.json`, set that edge's strength (adding it if it didn't exist, replacing it if it did).

Result: a graph where `CanPlay("cat", "milk")` returns true because of the explicit connection, and `CanPlay("bee", "flower")` returns true because the `pollinator ↔ blooming` rule created that edge.

---

## Adding a new card

1. **Add to `cards.json`** with an `id`, `text`, `image`, `category`, and `tags`.
2. **Tags do the work automatically.** If you tag the card `forest-animal`, it auto-connects to all other forest animals, and to `forest` itself.
3. **Add to `connections.json` only** for associations that tags can't capture, or to override an auto-generated strength.
4. **No changes needed** to `tag-rules.json`, `tags.json`, or any C# file.

Example — adding a penguin:

```json
// cards.json
{"id":"penguin", "text":"Penguin", "image":"penguin", "category":"wildlife",
 "tags":["aquatic", "flier", "prey", "egg-layer", "winter"]}

// connections.json — only if the auto-generated strength isn't right
{"card1":"penguin", "card2":"fish", "strength":0.90}
```

The tags alone auto-connect penguin to: fish, duck, frog, river, pond, snow, ice, bird, egg — all without touching any rule file.

---

## Adding a new tag rule

Add one line to `tag-rules.json` and update `hasRule` to `true` in `tags.json`:

```json
// tag-rules.json
{"tag1": "egg-layer", "tag2": "egg-layer-product", "strength": 0.85}
```

This immediately makes bird, duck, and any future egg-layer connect to egg — no card changes needed.
