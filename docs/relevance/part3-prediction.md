# Part 3: Predict

Implemented sample: `samples/RelevancePredict`, exposed as `predict` in the existing RelevanceShop console tool. Article: [SHOW ME WHAT YOU GOT (Part 3): Predict](../../Mostlylucid/Markdown/show-me-what-you-got-part3-predict.md).

The objective is to make outcome learning directly visible before introducing controlled interventions. A fresh SQLite engine learns transition counts from a substantial synthetic population, forecasts destinations for new prefixes, persists across a restart, and updates the next forecast after an actual endpoint arrives.

## Data and representation

Bogus 35.6.5 supplies fake product/user metadata. A separately seeded simulator supplies observed journeys, final endpoints and incomplete feedback. The default has 240 products, 1,000 training visits and 400 later visits. Prefix states preserve comparison depth, cart progress, removals and returns. Simulator intent is private to generation; future labels never become input features. Counts/encoder versions and the fixed clock are explicit.

Optional local LLM copy is generated once, structurally validated, reviewed and frozen. Its overlay cannot change price, category, swatch, stock or labels. Copy remains metadata until an explicitly registered feature converter uses it.

## Learning and inference

Use the existing MarkovDestinationPredictor and SaveAsync/PredictAsync APIs. Outcomes increment empirical transition counts in the same transaction as deduplication. Sparse states abstain. Probabilities are for the next four transitions and preserve unresolved mass. A new cancellation changes the next checkout forecast from 306/348 to 306/349; its duplicate retry does not train twice.

A sample-only IDestinationPredictor adapter captures detached training counts. A second adapter freezes those counts for the evaluation engine while allowing ordinary subject-prefix ingestion. No core API expansion is needed.

## Evaluation

The article distinguishes destination probabilities from application-owned desirability. An illustrative utility mapping (purchase 1, cancel -0.5, leave 0) demonstrates expected utility without changing the predictor's configuration or counts. Unresolved mass is unknown value, not a zero-valued endpoint. This is interpretation of the forecast, not an implemented intervention policy or evidence of action uplift.

The held-out landmark is the first cart addition. Forecasts are captured before inspecting labels. Missing feedback is unscored, an observed horizon without an endpoint is unresolved, and sessions without a cart never enter the landmark population. Compare multiclass Brier score with a training-only landmark prior and report reliability bins separately.

The article's seed 814 yields 349 scored visits, 27 censored and 24 without a cart. Brier: model 0.4750, prior 0.6234. Three independent seeded test fixtures verify improvement over that baseline. This establishes the mechanism on the simulator, not production calibration or causal treatment effects.

## Progression

Part 3 supplies the expected-outcome baseline. [Part 4](part4-intervention-learning.md) adds directed interventions and measured responses: product/price/offer assignment, marketing messages, descriptions, missing information and allowed operational actions. The connection to DiSE is deliberate: make a bounded change, observe its response and update what the system knows. A later treatment-aware ML model can learn from that ledger.

[Part 5](part5-richer-representations.md) introduces richer image/text representations after those measurement and experimentation contracts exist.
