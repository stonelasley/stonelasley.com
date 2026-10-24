---
status: accepted
---

# Search launches on hybrid, with the vector leg's cosine floor at 0.7

v1 search fuses the keyword leg (FTS5, bm25) and the vector leg (bge-base-en-v1.5 on Vectorize) with Reciprocal Rank Fusion at k = 60 and equal weights, as built in #42. The one change from that build is the vector leg's cosine floor, raised from 0.6 to 0.7. The first search eval (#43), run on the frozen corpus of 7 Recipes and 17 Watches with 40 graded queries, picked it:

| nDCG@5      | overall | exact | paraphrase | conceptual | no-answer queries with results |
| ----------- | ------- | ----- | ---------- | ---------- | ------------------------------ |
| keyword     | 0.868   | 1.000 | 0.985      | 0.609      | 2 of 5                         |
| vector, 0.6 | 0.853   | 0.929 | 0.839      | 0.787      | 3 of 5                         |
| hybrid, 0.6 | 0.868   | 0.937 | 0.950      | 0.710      | 5 of 5                         |
| hybrid, 0.7 | 0.879   | 1.000 | 0.957      | 0.670      | 3 of 5                         |

At 0.6, the vector leg returns weak matches for nearly every query. Fusion lets them push exact answers down, so hybrid only ties keyword overall and fails the eval's gate. At 0.7, hybrid beats both legs overall, matches keyword on exact queries, and returns less noise for queries with no answer. Its scores per query type fall within the gate's 0.05 of the best single leg.

## Considered Options

- **Launch on keyword alone.** It's the simplest choice and the best on exact and paraphrase queries, but it is the worst leg on conceptual queries ("a post-workout snack that tastes like dessert"), which are the reason a vector leg exists.
- **Tune RRF k and the leg weights as well.** A sweep of 36 combinations found settings up to 0.889 overall. Each one clears a gate by a few thousandths and misses another by about as much. On 40 queries those differences are noise, and fitting to them would overfit. Changing one setting, with wide margins, is the honest choice.
- **Keep the 0.6 floor and launch on vector alone.** It's the best on conceptual queries, but the worst overall. It also misses exact references such as "116610".

## Consequences

- Conceptual queries score lower in hybrid at 0.7 (0.670) than at 0.6 (0.710). That is the price of exact matches and less no-answer noise. The nightly eval tracks it.
- Vector-only mode gets worse at 0.7. It isn't served, so it matters only as a leg of hybrid.
- `api/tests/Api.FunctionalTests/SearchEval/scores.json` holds this run as the baseline. A PR fails if any query type's hybrid nDCG@5 drops more than 0.02 below it. To accept a deliberate change, rewrite the file with `SL_EVAL_WRITE_SCORES=1`.
- Revisit this when the corpus grows. The tie-breaking margins rest on 24 Posts.
