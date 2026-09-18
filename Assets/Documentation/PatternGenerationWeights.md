# Pattern Generation Weight Manual

This manual describes how pattern selection currently works in the project. It is based on the runtime implementation in:

- `Assets/Scripts/Core/Orbits/Generators/Patterns/ObstaclePatternDefinition.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/ObstaclePatternGenerationConfig.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/PatternLevelContentGenerator.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/WeightedPatternSelector.cs`

The values described here are relative selection weights, not percentages or guaranteed spawn frequencies.

## Per-Difficulty Configuration

`ObstaclePatternGenerationCatalog.asset` assigns one `ObstaclePatternGenerationConfig`
asset to each difficulty. Edit the matching `ObstaclePatternGeneration_<Difficulty>.asset`
to change its pattern library, orbit-index stages, seed, selection attempts, or band
phase without changing the other difficulties. The Pattern Probabilities window
uses this same catalog when its Difficulty selection changes. Pattern assets
still own their own difficulty weight modifiers; those weights are applied only
when the pattern is included in the selected difficulty's config.

## Editor Probability Tool

Open `Orbital Escape > Generation > Pattern Probabilities` in the Unity Editor.

The window calculates next-placement probability for the selected orbit index, difficulty, previous-pattern streak, and rift context. Lower/upper nearest inward-neighbor rift distances can be entered independently. It shows every configured pattern, its effective weight, flip-orientation count, normalized probability, and any reason it is currently unavailable.

The displayed probability is the configured candidate probability for non-rift
patterns before live cursor-space, overlap, and rift-marker filtering. Patterns
containing a Rift on either lane are quota-driven and excluded from the rows
and denominator. They remain available as a Previous Pattern for history-based
weighting. Runtime constraints can remove candidates and renormalize the final
choice.

`Safe Padding` defaults to 2 world units in each difficulty's pattern generation
config; 0 disables it. It adds
empty lower-orbit world distance between authored pattern spans only when a
non-Space slot at the end of one physical lane is followed by a non-Space slot
at the start of the opposite physical lane, after Allow Flip is resolved. A
same-lane obstacle continuation adds no padding. Existing empty cursor space
counts toward the configured distance, and the same rule applies across the
orbit seam. This can make a weighted candidate unable to fit; the probability
window does not simulate a particular cursor or previous orientation.

## Selection Overview

For every pattern position in an orbit band, generation follows this order:

1. Select the difficulty stage that contains the lower orbit index of the current orbit band.
2. Reject patterns that are disabled, outside their orbit-index range, contain locked slot types, have zero weight, or do not fit the orbit.
3. Create one candidate for the normal orientation.
4. Create a second candidate when `Allow Flip` is enabled.
5. Reject candidates that conflict with existing orbit objects or rift markers.
6. Calculate the effective weight of every remaining candidate.
7. Select one candidate through weighted random selection.

The probability of one valid candidate is:

```text
Candidate probability = Candidate effective weight / Sum of all valid candidate weights
```

Invalid candidates have no probability, regardless of their configured weight.

## Space Slots

`Space` is an empty layout slot that never spawns an orbit object. Each Space has its own `Length`, authored directly in its `ObstaclePatternDefinition`, with a minimum value of `0.01` world units.

Authored lengths are the base layout values. When a lane is placed on the physical upper orbit, angular compensation can expand its Space slots proportionally so both pattern lanes end at the same angle. Obstacle slot lengths remain unchanged.

## Effective Weight Formula

The runtime formula is:

```text
EffectiveWeight = BaseWeight
                * DifficultyWeightMultiplier
                * RepetitionModifier
```

`RepetitionModifier` is calculated as follows:

```text
Same pattern repeated N times:
    SamePatternWeightMultiplier ^ N

Different pattern sharing at least one tag with the previous pattern:
    SameTagWeightMultiplier

Otherwise:
    1.0
```

The same-pattern and same-tag modifiers are mutually exclusive for a candidate. Repeating the exact same pattern uses only the same-pattern rule. It does not also receive the same-tag penalty.

## Rift Markers And Fulfillment

Each writable orbit receives a seeded random marker layout based on its rift quota and world-unit minimum spacing. Markers trigger authored rift patterns; they are not exact required rift centers.

Marker layouts are planned in ascending orbit-index order. The generator creates several complete layouts that satisfy the hard same-orbit spacing rule, scores them against markers and existing rifts on inward orbits, and selects one through weighted randomness. This keeps the configured quota when a complete layout is physically possible while making long radial rift chains less likely.

`Obstacle Clearance` expands each rift's placement-blocking interval on both edges and both sides of its owning orbit. It does not change the visible rift length. This prevents obstacle colliders from touching the player's entry or landing path even when authored patterns meet exactly at a slot boundary.

Marker eligibility uses the band's unwrapped world-distance cursor. Once a pattern reaches a marker, it stays due until an actual rift on that orbit consumes it. Clearance jumps cannot discard markers, and an ordinary non-rift pattern cannot advance while a marker is due.

Candidates that satisfy all due markers on both lanes are preferred. If no such candidate fits, weighted selection can choose a valid rift pattern that satisfies part of the demand. Each rift consumes exactly one due marker on its own orbit, oldest in band traversal order first. Remaining markers stay due for later patterns, including when the other lane was satisfied first. Candidates cannot add more rifts than the number of due markers on either orbit.

This may place a rift after its original marker when nearby lane markers or existing content cannot be served by a single pattern. Authored slot lengths, eligibility, weights, actual rift spacing, and obstacle overlap checks still apply.

At the orbit seam, a full pattern can use the trailing empty gap and wrap into unused space before the first pattern. Its authored span cannot overlap a previously placed pattern in that band. If necessary, its start moves backward within the unused trailing gap to fit.

Unfulfilled markers remain available to the orbit's next generated band. Final warnings report actual rifts versus target, including appended boundary content, plus pending markers. A quota can still fall short if marker allocation or compatible authored placements are exhausted; increasing weights cannot bypass those constraints.

## Cross-Orbit Marker Distribution

Cross-orbit avoidance changes marker-layout probability, not authored-pattern weight. For every marker, the generator compares its angle with markers and existing rifts on the configured number of inward orbits. Circular angular distance is converted to arc length using the current orbit radius, so positions across the `0 degrees` seam are compared correctly.

The default settings are:

| Setting | Default | Purpose |
|---|---:|---|
| `Cross Orbit Safe Distance` | `12 units` | Range over which an inward rift reduces layout probability. |
| `Cross Orbit Lookback` | `3` | Number of consecutive inward orbits considered. |
| `Consecutive Alignment Weight Multiplier` | `0.25` | Weight at exact alignment for each inward orbit. |

For one inward orbit, the factor interpolates linearly from `0.25` at exact alignment to `1.0` at 12 units or farther:

| Nearest center distance | Factor |
|---:|---:|
| `0 units` | `0.25` |
| `6 units` | `0.625` |
| `12 units` or more | `1.0` |

Factors multiply across the lookback. Exact alignment with rifts on three consecutive inward orbits produces `0.25 * 0.25 * 0.25 = 0.015625` layout weight. The factor never reaches zero, so nearby rifts and even long chains remain possible, only increasingly uncommon.

## Base Weight

`Base Weight` is configured on each `ObstaclePatternDefinition` asset.

- A larger value makes the pattern more likely relative to other valid patterns.
- `Base Weight = 0` makes the pattern ineligible.
- Doubling every pattern's base weight changes nothing because only their ratios matter.

Example with three non-flipped candidates:

| Pattern | Base weight | Probability |
|---|---:|---:|
| Spikes | 5 | 71.43% |
| Recovery | 1 | 14.29% |
| Rift | 1 | 14.29% |

The total weight is `5 + 1 + 1 = 7`. The Spikes probability is therefore `5 / 7`.

## Difficulty Weight Modifier

`Difficulty Weight Modifiers` is configured separately on each pattern asset. Every entry targets one `DifficultyType`.

```text
Adjusted base weight = BaseWeight * DifficultyWeightMultiplier
```

Recommended interpretation:

| Modifier | Effect |
|---:|---|
| `0.0` | Disables this pattern for that difficulty |
| `0.5` | Half of its normal weight |
| `1.0` | No change |
| `1.5` | 50% more weight |
| `2.0` | Double weight |

If a pattern has no entry for the active difficulty, the runtime uses `1.0`.

Use this modifier when the same authored pattern should have different importance between difficulty modes. Use `Base Weight` for its general importance across the game.

## Same Pattern Weight Multiplier

`Same Pattern Weight Multiplier` is configured per orbit-index stage in `ObstaclePatternGenerationConfig`.

It reduces the chance of immediately repeating the exact same pattern. The multiplier is exponential across a consecutive streak:

```text
RepetitionModifier = SamePatternWeightMultiplier ^ ConsecutiveRepeatCount
```

With the current early Basic-stage value of `0.8`:

| Previous consecutive selections | Repetition modifier |
|---:|---:|
| 0 | `1.0` |
| 1 | `0.8` |
| 2 | `0.64` |
| 3 | `0.512` |

For a pattern with base weight `5` and difficulty modifier `1`:

| Previous consecutive selections | Effective weight |
|---:|---:|
| 0 | `5.0` |
| 1 | `4.0` |
| 2 | `3.2` |
| 3 | `2.56` |

Important behavior:

- Normal and flipped versions share the same pattern identity. Selecting the opposite orientation still counts as repeating the same pattern.
- Selecting any different pattern ends the same-pattern streak.
- Pattern history is local to one adjacent-orbit band. It resets for the next band and for the next level build.
- Only the latest eight selections are retained, so the maximum tracked consecutive exponent is eight.
- A lower value creates stronger variety. A value near `1.0` allows frequent repeats.

### Current zero-value caveat

The Inspector allows `0`, but `PatternDifficultyStage.CreateSnapshot()` currently replaces values less than or equal to zero with the defaults:

- Same Pattern: `0.35`
- Same Tag: `0.75`

Therefore, setting either stage modifier to `0` does not produce zero weight with the current code.

## Same Tag Weight Multiplier

`Same Tag Weight Multiplier` is also configured per orbit-index stage. It reduces the chance of selecting a different pattern with a similar theme immediately after the previous pattern.

The current tags are:

- `Recovery`
- `Spike`
- `Trap`
- `Gravity`
- `Rift`
- `Compound`
- `Zigzag`

The penalty applies when:

1. The candidate is a different pattern asset from the previous selection.
2. The two patterns share at least one tag.

With the current early Basic-stage multiplier of `0.9`, a pattern with base weight `4` and difficulty modifier `1` receives:

```text
4 * 1 * 0.9 = 3.6 effective weight
```

Important behavior:

- Sharing multiple tags still applies the multiplier only once.
- Only the immediately previous pattern is checked.
- `PatternTag.None` never creates a tag match.
- The same-tag penalty does not stack with the same-pattern penalty.
- Assigning many broad tags makes a pattern more likely to be penalized after other patterns.

Use tags to describe the gameplay experience of a pattern, not every object it contains. For example, a difficult spike arrangement could use `Spike | Compound`, while a simple single spike pattern may only use `Spike`.

## Allow Flip And Aggregate Probability

`Allow Flip` creates a separate weighted candidate for each orientation:

- Candidate A: Orbit 1 is lower and Orbit 2 is upper.
- Candidate B: Orbit 2 is lower and Orbit 1 is upper.

Each valid orientation is a separate candidate with the same authored weight. Placement constraints can still reject one orientation while leaving the other available. A flippable pattern therefore has twice the aggregate probability of an otherwise identical non-flippable pattern when both orientations are valid.

Example:

| Pattern | Effective weight per orientation | Valid orientations | Aggregate weight |
|---|---:|---:|---:|
| Pattern A, flip enabled | 2 | 2 | 4 |
| Pattern B, flip disabled | 2 | 1 | 2 |

Pattern A has twice the selection probability in this case.

When every compared pattern allows flipping, their relative ratios remain unchanged. If only some patterns allow flipping, account for the additional candidate when tuning base weights.

An orientation rejected by placement rules contributes no weight. A flippable pattern can therefore have one or two active candidates depending on the current orbit state.

## Constraints Override Weights

Weights are evaluated only after eligibility and placement constraints. A high weight cannot force an invalid pattern to spawn.

A candidate can be removed because of:

- Disabled pattern asset.
- `Base Weight` or effective weight equal to zero.
- Current lower orbit index outside the pattern's Min/Max Orbit Index range.
- A slot type locked by the active difficulty stage.
- Pattern length exceeding the orbit circumference.
- Collision with an existing object or a two-sided rift.
- Rift marker count or spacing rules.
- A random rift marker temporarily requiring a rift-containing candidate.
- The candidate selection limit being reached.

Rift markers can substantially change observed frequencies. At the beginning of a build, each writable orbit receives a weighted-random marker layout from `0` to its circumference. The hard same-orbit center distance controls validity; cross-orbit settings only influence which complete valid layout is selected. When a pattern span reaches an unfulfilled marker, non-rift patterns are temporarily filtered out. Weights then choose among valid rift candidates. If no authored rift pattern can cover a marker, that marker remains unfulfilled and a warning is logged. Candidates that would add rifts without crossing a marker are removed.

Each orbit band also receives one seeded random angular phase. Both lanes rotate together, preserving the authored layout while preventing every band from beginning visually at `0 degrees`.

`Max Selection Attempts` limits the number of candidates collected at one cursor position. Flip orientations count as separate candidates. With the current value of `64` and a small pattern library, this limit has no practical effect. With a much larger pattern library, asset order can matter after the limit is reached.

## Orbit Index Ranges And Stage Boundaries

Pattern and stage ranges use inclusive comparisons:

```text
orbitIndex >= MinOrbitIndex && orbitIndex <= MaxOrbitIndex
```

`Max Orbit Index = 0` is converted to an unlimited upper range and means "no upper limit."

Difficulty stages are sorted by Min Orbit Index, and the first matching stage is used. Because both ends are inclusive, shared boundaries belong to the earlier stage.

For the current Basic profile, the effective ranges are:

| Actual lower orbit index range | Unlocked slots | Same Pattern | Same Tag |
|---|---|---:|---:|
| `0 <= orbitIndex <= 10` | Space, Spike, Rift | `0.8` | `0.9` |
| `10 < orbitIndex <= 25` | Space, Spike, Impulse Trap, Rift | `0.35` | `0.75` |
| `orbitIndex > 25` | All slots, including Gravity Well | `0.35` | `0.75` |

At exactly orbit index `10`, the first stage is used. At exactly orbit index `25`, the second stage is used.

If any slot in a pattern is locked, the entire pattern is rejected. A mixed pattern containing Spike and Gravity Well is therefore unavailable until Gravity Well is unlocked.

## Current Basic Difficulty Example

The current Basic configuration has four patterns:

| Pattern | Tags | Base | Allow Flip | Fresh aggregate weight |
|---|---|---:|---:|---:|
| Rift | Rift | `1` | Yes | `2` |
| Zigzag Three Jumps | Spike, Zigzag | `4` | No | `4` |
| Zigzag Two Jumps | Spike, Zigzag | `5` | Yes | `10` |
| Recovery Two Spaces | Recovery | `2` | Yes | `4` |

Every pattern has a Basic difficulty multiplier of `1`, either explicitly or through the missing-entry fallback. Flip-enabled patterns contribute two candidates when both orientations are valid; Zigzag Three Jumps contributes one.

The examples below describe authored-pattern selection after marker layout planning and assume no rift marker is currently forcing a rift pattern.

### Fresh selection

At orbit index `0`, all four patterns are eligible. With no history and no marker or placement restrictions:

```text
Rift:                 1 * 2 orientations = 2
Zigzag Three Jumps:  4 * 1 orientation  = 4
Zigzag Two Jumps:    5 * 2 orientations = 10
Recovery:             2 * 2 orientations = 4
Total: 20
```

| Pattern | Approximate probability |
|---|---:|
| Zigzag Two Jumps | `50.00%` |
| Zigzag Three Jumps | `20.00%` |
| Recovery | `20.00%` |
| Rift | `10.00%` |

These percentages apply only when every candidate is valid and no rift marker is currently forcing a rift pattern.

### After Zigzag Two Jumps was selected once

The same-pattern multiplier is `0.8`. Zigzag Three Jumps receives the same-tag multiplier of `0.9`:

```text
Zigzag Two Jumps:    5 * 0.8 * 2 = 8
Zigzag Three Jumps:  4 * 0.9 * 1 = 3.6
Recovery:            2 * 1.0 * 2 = 4
Rift:                1 * 1.0 * 2 = 2
Total: 17.6
```

| Pattern | Approximate probability |
|---|---:|
| Zigzag Two Jumps | `45.45%` |
| Recovery | `22.73%` |
| Zigzag Three Jumps | `20.45%` |
| Rift | `11.36%` |

### Basic difficulty modifier experiment

Set Zigzag Two Jumps' Basic difficulty modifier to `0.5`, while keeping all other current values:

```text
Zigzag Two Jumps: 5 * 0.5 * 2 = 5
All other patterns:             10
Total: 15
```

The fresh probabilities become:

| Pattern | Approximate probability |
|---|---:|
| Zigzag Two Jumps | `33.33%` |
| Zigzag Three Jumps | `26.67%` |
| Recovery | `26.67%` |
| Rift | `13.33%` |

This is the cleanest way to make one pattern less common only in Basic difficulty without changing its global base weight.

## Practical Tuning Guide

Use `Base Weight` when:

- A pattern should generally be common or rare in every difficulty.
- You are establishing the main ratio between patterns.

Use `Difficulty Weight Modifier` when:

- A pattern should be emphasized or suppressed in one difficulty.
- You want to disable a pattern for one difficulty with `0`.

Use `Same Pattern Weight Multiplier` when:

- Exact repeated blocks are too noticeable.
- You want stronger alternation between authored patterns.
- Use a lower number for stronger anti-repetition behavior.

Use `Same Tag Weight Multiplier` when:

- Different patterns of the same gameplay family appear too often in sequence.
- You want to alternate between Recovery, Spike, Trap, Rift, and Gravity experiences.

Suggested tuning workflow:

1. Temporarily enable `Use Fixed Seed` to compare changes against the same generated sequence.
2. Set base weights first and inspect fresh distribution.
3. Add difficulty modifiers for difficulty-specific balance.
4. Tune Same Pattern to control exact repetition.
5. Tune Same Tag to control thematic repetition.
6. Test with rift markers active because they intentionally override the normal candidate pool.
7. Disable Fixed Seed when finished.

## Common Mistakes

- Treating a weight as a percentage. Weight `5` means five relative shares, not 5%.
- Forgetting that `Allow Flip` can create two full-weight candidates.
- Expecting stage modifier `0` to disable repetition under the current fallback implementation.
- Using `Max Orbit Index = 0` to mean orbit index zero only. It actually means unlimited.
- Overlapping stage boundaries and expecting the later stage to win.
- Giving every pattern many tags, causing frequent same-tag penalties.
- Expecting weights to override rift markers, locked slots, or placement conflicts.
- Comparing results across different random seeds while tuning small weight changes.
- Expecting changes to update already generated orbits. Config snapshots are captured before each level build, so edits affect subsequent builds only.
