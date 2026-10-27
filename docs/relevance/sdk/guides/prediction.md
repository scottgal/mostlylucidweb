# Outcome prediction

Configure observed states, absorbing outcomes and a horizon. The supplied predictors are usable directly; the application supplies state/outcome meanings and real observations.

`DestinationDefinition` defaults: `States/Outcomes` empty, `HorizonEvents=8`, `MinTransitions=10`, `Smoothing=0.5`, `Trajectory=null`. States require 2–64 distinct entries; outcomes are a nonempty subset. Event horizon is 1–256 transitions, not seconds. Smoothing is a nonnegative symmetric pseudo-count, used only in supported rows.

```csharp
definition.Destinations = new DestinationDefinition
{
    States = ["view", "basket", "purchase", "cancel"],
    Outcomes = ["purchase", "cancel"]
};
```

The default Markov predictor uses pooled first-order transition counts within the engine space. Destinations are absorbing during propagation. Insufficiently observed transitions retain unresolved probability. `DestinationPrediction.Outcomes` plus `RemainingProbability` accounts for total mass; do not renormalise away unsupported mass. Baseline outputs are uncalibrated and say `IsCalibrated=false`.

## Physical-time trajectories

Set `Destinations.Trajectory` and use input-sourced measurements. The configured points must total at most 64 coordinates. Three time-ordered samples are needed for centroid velocity and acceleration; irregular sampling uses actual seconds and interval midpoints. Derivative scaling is part of the representation, not a probability conversion.

| TrajectoryDefinition property | Default | Meaning |
|---|---:|---|
| Points | empty | Ordered configured measurement points |
| HorizonSeconds | 60 | Observation-anchored forecast window |
| HalfLifeSeconds | 5 | Fast centroid smoothing half-life |
| MaxGapSeconds | 300 | Larger gaps reset motion/landmarks |
| PositionScale | 1 | Centroid matching scale |
| VelocityScale | 0.01 | Coordinate units/second per feature unit |
| AccelerationScale | 0.001 | Coordinate units/second² per feature unit |
| ElapsedScaleSeconds | 60 | Episode elapsed-time matching scale |
| BinWidth | 0.5 | Feature-cell width |
| MaxDistance | 1 | Maximum RMS matched-context distance |
| NearestCohorts | 8 | Supported cells combined in a forecast |
| MinExamples | 20 | Minimum matched completed-episode support |
| MaxCohorts | 2048 | Bound; least recently updated cell is evicted |
| BaselineStates | empty | Explicit states allowed to update normal references |
| BaselineHalfLifeSeconds | 3600 | Slow normal-reference half-life |
| MinBaselineMass | 10 | Effective mass needed for a normal reference |
| MinBaselineVariance | 0.0001 | Variance floor for normalised displacement |

An observed endpoint may omit all measurement points; it closes the episode without inventing a position or derivative. Partial measurements are rejected. Use a new subject/episode identity after closure. Each completed episode contributes its first motion-ready landmark per observed state, preventing repeated samples from multiplying votes.

Normal-reference admission is explicit. Supply `baseline_group` when baseline learning is enabled; unrelated services must not share a normal. Incident states do not update that norm. RMS standardised displacement is diagnostic distance, not incident probability.

## Result interpretation

Markov statuses include `no_state`, `insufficient_evidence`, `estimated`, `observed_destination`. Temporal statuses add `no_trajectory`, `insufficient_motion`, `insufficient_trajectory_evidence`, `stale_trajectory`, `estimated_trajectory`. Read the result's observation time and horizon. An elapsed forecast becomes stale when its observation-anchored window expires; repeated reads do not move that window.

Temporal results set `HorizonEvents=0` and supply `HorizonSeconds` plus `Trajectory`. Support is matched completed landmarks, not calibrated confidence. Missing endpoints and selection effects can bias the observed population. `IDestinationPredictor` is the extension point for a different validated estimator.
