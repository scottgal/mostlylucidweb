# Part 3: Predict

The primary demonstration is now `predict-time` in `samples/RelevanceShop`, implemented by `samples/RelevancePredict/TemporalPredictionDemo.cs`. The original `predict` command remains the simpler event-count baseline. Article: [SHOW ME WHAT YOU GOT (Part 3): Predict](../../Mostlylucid/Markdown/show-me-what-you-got-part3-predict.md).

## Physical time is part of the representation

The optional `destinations.trajectory` configuration selects source points, a fast centroid half-life, coordinate/rate/acceleration scales and a forecast horizon in seconds. Three strictly ordered timestamped observations produce centroid velocity and acceleration. Irregular acceleration uses the separation between successive velocity midpoints. Long gaps reset derivatives and pending landmarks rather than asserting continuity. Existing configurations retain the Markov predictor and event-count horizon.

`PredictAsync` returns a real `TrajectoryFrame`: observed position, smoothed centroid, velocity, acceleration, elapsed time, and optional normal evidence. Temporal forecasts are anchored at `ObservedAt` and expire after `HorizonSeconds`. A clock reading alone does not invent a new observation. The single channel/worker and atomic store transactions remain the consumer boundary.

## Outcome learning

Completed episodes contribute their first motion-ready landmark per categorical state to bounded feature-cell populations. Cells maintain context centroids and actual within-window outcome counts. Nearby eligible populations, rather than screen geometry, supply empirical probabilities. Context includes centroid, velocity, acceleration and elapsed time. Minimum support/distance bounds abstain; unfinished episodes never become failures. These completed-episode estimates are uncalibrated and can be biased by informative loss of feedback.

## Qualified normal references

Normality is separate from outcome prediction. Explicit healthy-state admission updates a slow decayed mean/variance per application-defined reference group. Incident observations update live motion but cannot update the normal reference. Minimum effective mass, variance floors and retention make missing/old references explicit. Drift is RMS standardised displacement, not an incident probability. The sample uses a two-second live half-life and a 24-hour reference half-life.

## Reproducible evidence

Each domain trains on 600 earlier synthetic episodes with timing/feature noise, then evaluates 300 later visits at their first motion-ready landmark against a frozen outcome model. Qualified normal observations precede training/evaluation. Seed 814 gives:

| Domain | Scored | Censored | Model Brier | Training-only prior |
| --- | ---: | ---: | ---: | ---: |
| Shopping | 270 | 30 | 0.3405 | 0.7197 |
| Operations | 264 | 36 | 0.2180 | 0.6657 |

The replay captures 24 additional visits per domain. Its first three visits share the same observed vector but differ in direction or pace. Shopping A/B purchase estimates within 60 seconds are 85.5% and 22.0%; the slower C has 100% outside-window unresolved mass. Operational A/B have 76.0% and 22.6% outage estimates, with a stable normal centroid near 100 ms latency and 1% errors.

These simulator experiments establish the mechanism, not production calibration or causal uplift. They are separate fixtures from the state-only Bogus/Brier example; their scores are not a head-to-head benchmark.

## Visualisation

The temporal replay uses actual captured UTC timestamps, smoothed feature-coordinate positions and a selectable playback speed. Direction/pace comparisons pin two visits at their own recorded landmarks while retaining the population. Small identity offsets and interpolation are presentation choices; the C# engine supplies motion and forecasts. Service mode shows the learned normal marker and standardised deviation. Expired recorded windows are labelled rather than presented as current forecasts.

## Progression

[Part 4](part4-intervention-learning.md) adds controlled actions and measured responses. Record temporal context and its version at assignment; distinguish the forecast's observation-based horizon from the intervention's exposure-based attribution window. Destination desirability and intervention costs remain application-owned. [Part 5](part5-richer-representations.md) introduces richer image/text representations after the measurement contracts exist.
