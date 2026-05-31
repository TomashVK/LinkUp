# Uno Associations — Project Rules

## Code Style for Scripts

- **Unity 6, C# 9.0.** No file-scoped namespaces.
- **No underscore prefix** on private fields. Use `layout`, not `_layout`.
- **No alignment spaces.** Don't pad variable names with extra spaces to form columns.
- **No comments unless the WHY is non-obvious** — a hidden constraint, a workaround for a specific bug, or a subtle invariant. Never explain what the code does.
- **No multi-line comment blocks or docstrings.**
- `[SerializeField]` for inspector-exposed fields. All other fields are private.
- **Static events** for cross-component communication (e.g. `Card.Dropped`, `HandManager.CardLeftHand`). Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- **DOTween** for animations. **Unity Splines** (SplineContainer, BezierKnot) for hand card layout.
- **New Input System** via `PointerInputService` singleton — never poll `Input` directly.
- **TextMesh Pro** (`TMP_Text`) for all in-game text.
- **JsonUtility** cannot deserialize bare JSON arrays. Always wrap: `{"items":[...]}` using the `LoadArray<TWrapper,TItem>` helper in `LevelLoader`.
- When a constructor or method parameter name clashes with a field name, use `this.fieldName = paramName`.
- `ICardDrop.OnCardDrop` returns `bool` — `true` = accepted, `false` = snap back.

---

## Adding a Card

1. **`Assets/Resources/Data/cards.json`** — add one entry:
   ```json
   {"id":"penguin", "text":"Penguin", "image":"penguin", "category":"wildlife",
    "tags":["aquatic", "prey", "egg-layer", "winter"]}
   ```
   - `id` must be unique and lowercase. Everything else references cards by this string.
   - `image` is the sprite name for card artwork.
   - `tags` drive auto-connection. Pick from existing tags in `tags.json`.

2. **Tags do the work automatically.** The new card connects to any other card that matches a tag pair in `tag-rules.json`. No C# changes needed.

3. **`Assets/Resources/Data/connections.json`** — add explicit entries only for associations that tags can't express (cultural knowledge like "cats drink milk") or to override an auto-generated strength.

4. **If you use a new tag** (one not in `tags.json`):
   - Add the tag entry to `tags.json` with `"hasRule": false`.
   - If you also add a rule for it in `tag-rules.json`, set `"hasRule": true`.

No C# file changes are needed to add a card.

---

## Editing Cards and Tags

### Changing a card's properties
Edit the entry in `cards.json`. Changing `tags` immediately affects which auto-connections are generated at runtime.

### Adding a tag rule
Add one line to `tag-rules.json`:
```json
{"tag1": "egg-layer", "tag2": "egg-layer-product", "strength": 0.85}
```
Then update `hasRule` to `true` for both tags in `tags.json`.

Rules are symmetric — `tag1 ↔ tag2` covers both directions automatically.

### Adding or overriding an explicit connection
Add to `connections.json`:
```json
{"card1": "bear", "card2": "salmon", "strength": 0.90}
```
Explicit entries override any auto-generated strength for that pair.

### Strength values
- `0.90–1.00` — strong, obvious associations (cow↔milk, bee↔honey)
- `0.70–0.89` — clear associations (horse↔carrot, forest↔bear)
- `0.55–0.69` — weaker or non-obvious (worm↔apple, wind↔bird)

---

## Creating Levels

### File location
`Assets/Resources/Data/Levels/level_N.json` where N is the next integer. The loader reads `level_1`, `level_2`, … in order until one is missing.

### File format
Each level file is a **JSON array of variants**. One variant is picked at random when the level loads. On restart, the player may get a different variant.

All variants in one file should use the **same card pool** (same cards, different hand/deck split).

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

### Level design rules

1. **Minimum 1 draw required.** The player must draw at least one card to win. A level solvable with only starting hand cards is too easy and doesn't teach associations.

2. **Hand must not be in solve order.** If the optimal chain is A→B→C, the hand should not contain them in that sequence. The player should have to figure out the order.

3. **Deck larger than hand.** Aim for 4–6 deck cards and 2–4 hand cards.

4. **Star thresholds match the optimal draw count.** If the fastest possible win needs 2 draws, set `threeStarMaxDraws: 2`. Don't set it to 0 when 2 draws are unavoidable.

5. **Deck order matters.** The deck is drawn top-to-bottom (index 0 first). Place cards in the order the player should naturally draw them when following the optimal path.

6. **Verify every variant by tracing the chain.** Step through the full optimal path and confirm it reaches an empty hand. Also trace the most tempting wrong first move to confirm it either recovers or loses gracefully.

7. **Design at least one trap (recommended).** A trap is a tempting first move that strands a card permanently. Good traps teach the player something when they fall into them.

8. **Variants may use a different active card.** All variants share the same card pool, but each variant can start from a different card in that pool. The active card counts as one of the total cards, so swapping it changes the puzzle feel without changing the card set.
