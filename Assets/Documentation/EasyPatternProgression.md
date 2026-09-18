# Easy Pattern Progression

## Bands

Generation uses the physical lower orbit index of a band, not the live score.
All bounds are inclusive. Max Orbit Index 0 means no upper bound.

| Orbit indices | Challenge library | Status |
|---|---|---|
| 0-6 | Five opening patterns | Authored |
| 7-12 | Three opening patterns + three newly introduced patterns | Authored |
| 13-18 | The three patterns introduced at 7 + three new patterns | Authored |
| 19-24 | The three patterns introduced at 13 + three new patterns | Authored |
| 25-29 | Every configured Easy challenge pattern (14 total) | All-pattern mix |
| 30+ | Every configured Easy challenge pattern (14 total) | All-pattern mix |

Recovery and rift patterns remain available separately and do not count toward
the five/six challenge patterns.

At 25+, Ignore Pattern Orbit Range re-enables patterns that expired in earlier
bands. All other pattern eligibility, weights, and placement rules still apply.

## Opening Five

| Pattern | Base weight | Flip | Eligible indices | Carried into 7-12 |
|---|---:|---|---|---|
| 2Spikes | 2 | Yes | 0-12 | Yes |
| Mirror_3Spikes | 4 | No | 0-12 | Yes |
| Mirror_2Spikes+Gap | 4 | No | 0-12 | Yes |
| 2Mirrors+1Spike | 4 | No | 0-6 | No |
| SingleSpikeRecovery | 2 | Yes | 0-6 | No |

The first four keep their previously authored lane layouts.
SingleSpikeRecovery introduces an isolated spike with space before and after it;
despite its name it is a challenge pattern, not the empty Recovery pattern.

## Three Introduced At 7

| Pattern | Base weight | Flip | Purpose |
|---|---:|---|---|
| Mirror_2Spikes+SmallGap | 4 | No | Existing tighter variant of the wide-gap mirror |
| Mirror_5Spikes | 2 | Yes | Existing denser alternating spike sequence |
| 3SpikeRun | 2 | Yes | A compact wider spike cluster with an open opposite lane |

These join the three retained opening patterns at 7-12. All three continue into
13-18, where the opening patterns expire. They return at 25+ in the all-pattern
mix.

## Three Introduced At 13

| Pattern | Base weight | Flip | Purpose |
|---|---:|---|---|
| Alternating_3Triples | 2 | Yes | Three wider clusters with two lane switches |
| Alternating_3Pairs | 2 | Yes | Two lane switches across three pairs |
| TriplePairTriplePair | 2 | Yes | Three lane switches through mixed clusters |

These continue into 19-24 and remain available at 25+.

## Three Introduced At 19

| Pattern | Base weight | Flip | Purpose |
|---|---:|---|---|
| Alternating_4Pairs | 2 | Yes | Four short clusters across alternating lanes |
| Alternating_4Triples | 2 | Yes | Four wider clusters across alternating lanes |
| Alternating_3Quads | 2 | Yes | Three dense four-spike clusters with two lane switches |

These join the three retained 13-18 patterns at 19-24. They remain available
at 25+ alongside every other Easy challenge pattern.

## New Layouts

S(x) is a Space slot with length x. P is one Spike slot.
P2, P3, and P4 mean two, three, or four consecutive Spike slots, not a length override.
Lengths below use the current SpikeConfig.SpikeArcLength of 0.5 world units.

| New pattern | Orbit 1 Slots | Orbit 2 Slots | Each lane's authored length |
|---|---|---|---:|
| SingleSpikeRecovery | S(1.5), P, S(1.5) | S(3.5) | 3.5 |
| 3SpikeRun | S(1), P3, S(1) | S(3.5) | 3.5 |
| Alternating_3Triples | S(1.5), P3, S(6), P3, S(1.5) | S(5.3), P3, S(5.2) | 12 |
| Alternating_3Pairs | S(1.25), P2, S(5.6), P2, S(1) | S(4.55), P2, S(4.3) | 9.85 |
| TriplePairTriplePair | S(1.25), P3, S(5.6), P3, S(4.3) | S(5.05), P2, S(6.1), P2, S(1) | 14.15 |
| Alternating_4Pairs | S(2.5), P2, S(5.2), P2, S(4.1) | S(5.6), P2, S(5.2), P2, S(1) | 13.8 |
| Alternating_4Triples | S(2.5), P3, S(5.7), P3, S(4.6) | S(6.1), P3, S(5.7), P3, S(1) | 15.8 |
| Alternating_3Quads | S(2.5), P4, S(6.2), P4, S(1) | S(6.6), P4, S(5.1) | 13.7 |

The three patterns introduced at 13 now leave about 2.2-2.3 units between
successive obstacle clusters on different lanes before upper-lane compensation,
and contain two or three lane switches. The patterns introduced at 19 leave
2.1 units between successive clusters and contain two or three lane switches.
Their shorter spans prevent long obstacle-free stretches. All eight new assets
are enabled for Easy only through difficulty modifiers.
Previously authored assets keep their existing difficulty availability.

The three 7-18 patterns are also shorter: 3SpikeRun is 3.5 units per lane,
Mirror_2Spikes+SmallGap is 7.5, and Mirror_5Spikes is 7. Their obstacle
sequences and lane alignment remain unchanged.

Physical upper-lane spaces still expand proportionally with orbit radius.
Allow Flip swaps the authored lanes; it does not reverse slot order.
New patterns have spaces at both ends to make random concatenation and
reverse-direction approaches more forgiving. Playtesting is still required
to tune reaction windows, collider clearance, and the effect of radius.

## Weight Balance

Each challenge contributes fresh aggregate weight 4:
- Flip enabled: base weight 2 times two orientations.
- Flip disabled: base weight 4 times one orientation.

At 0-6 the five challenges total 20. At 7-12, 13-18, and 19-24, the three
carried-over challenges total 12 and the three new challenges total 12.
This gives equal initial weight to old and new among challenge candidates.
At 25+ all 14 challenge patterns have a fresh aggregate weight of 4 each.

This is a library and baseline-weight balance, not a strict spawn quota.
Same-pattern/tag penalties, remaining space, existing obstacles, rift markers,
and rejected flip orientations can change actual selection frequencies.
Recovery is outside this split; rifts continue using their marker-driven flow.

The probability window can display the baseline distribution with Previous
Pattern set to None. Use orbit indices 0, 7, 13, 19, and 25 to inspect the
libraries; use 6/7, 12/13, 18/19, and 24/25 to compare the boundaries.
