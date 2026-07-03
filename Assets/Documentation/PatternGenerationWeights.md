# Pattern Generation Weight Manual

This manual describes how pattern selection currently works in the project. It is based on the runtime implementation in:

- `Assets/Scripts/Core/Orbits/Generators/Patterns/ObstaclePatternDefinition.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/ObstaclePatternGenerationConfig.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/PatternLevelContentGenerator.cs`
- `Assets/Scripts/Core/Orbits/Generators/Patterns/WeightedPatternSelector.cs`

The values described here are relative selection weights, not percentages or guaranteed spawn frequencies.

## Editor Probability Tool

Open `Orbital Escape > Generation > Pattern Probabilities` in the Unity Editor.

The window calculates next-placement probability for the selected orbit index, difficulty, previous-pattern streak, and rift context. Lower/upper nearest inward-neighbor rift distances can be entered independently. It shows every configured pattern, its effective weight, flip-orientation count, normalized probability, and any reason it is currently unavailable.

The displayed probability is the configured candidate probability before live cursor-space, overlap, and rift-reservation filtering. Those runtime constraints can remove candidates and renormalize the final choice.

## Selection Overview

For every pattern position in an orbit band, generation follows this order:

1. Select the difficulty stage that contains the lower orbit index of the current orbit band.
2. Reject patterns that are disabled, outside their orbit-index range, contain locked slot types, have zero weight, or do not fit the orbit.
3. Create one candidate for the normal orientation.
4. Create a second candidate when `Allow Flip` is enabled.
5. Reject candidates that conflict with existing orbit objects or rift reservations.
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
                * NeighborRiftMultiplier
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

For patterns without rifts, both rift modifiers are `1.0`.

## Neighbor Rift Multiplier

A candidate rift compares its midpoint with rifts on the inward orbit (`current index - 1`). Distance is circular and measured as arc length in world units, so nearby angles across the `0 degrees` seam still count as close neighbors.

With the default avoidance distance of `26.18` units, the multiplier interpolates linearly from `0.25` at perfect alignment to `1.0` at or beyond `26.18` units:

| Nearest center distance | Multiplier |
|---:|---:|
| `0 units` | `0.25` |
| `13.09 units` | `0.625` |
| `26.18 units` or more | `1.0` |

The multiplier never reaches zero, so neighboring rifts remain possible. For a pattern placing multiple rifts, the strongest proximity penalty is used.

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

Each valid orientation is a separate candidate. Non-rift orientations receive the same effective weight, but rift orientations can differ because flipping changes the physical orbit whose neighbor context applies. When both orientations have the same context, a flippable pattern has twice the aggregate probability of an otherwise identical non-flippable pattern.

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
- Rift reservation count or spacing rules.
- A random rift reservation temporarily requiring a rift-containing candidate.
- The candidate selection limit being reached.

Rift reservations can substantially change observed frequencies. At the beginning of a build, each writable orbit receives random rift marker positions from `0` to its circumference, respecting the configured same-orbit center distance. When a pattern span reaches an unfulfilled marker, non-rift patterns are temporarily filtered out. Weights then choose among valid rift candidates. If no authored rift pattern can cover a marker, that reservation is skipped and a warning is logged. Candidates that would add rifts without a reservation are removed.

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

The examples below assume no inward-neighbor rift within `26.18` units, so the neighbor rift modifier is `1.0`.

### Fresh selection

At orbit index `0`, all four patterns are eligible. With no history and no reservation or placement restrictions:

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

These percentages apply only when every candidate is valid and no rift reservation is currently forcing a rift pattern.

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
6. Test with rift reservations active because they intentionally override the normal candidate pool.
7. Disable Fixed Seed when finished.

## Common Mistakes

- Treating a weight as a percentage. Weight `5` means five relative shares, not 5%.
- Forgetting that `Allow Flip` can create two full-weight candidates.
- Expecting stage modifier `0` to disable repetition under the current fallback implementation.
- Using `Max Orbit Index = 0` to mean orbit index zero only. It actually means unlimited.
- Overlapping stage boundaries and expecting the later stage to win.
- Giving every pattern many tags, causing frequent same-tag penalties.
- Expecting weights to override rift reservations, locked slots, or placement conflicts.
- Comparing results across different random seeds while tuning small weight changes.
- Expecting changes to update already generated orbits. Config snapshots are captured before each level build, so edits affect subsequent builds only.
