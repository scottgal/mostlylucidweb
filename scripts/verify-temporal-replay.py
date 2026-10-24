#!/usr/bin/env python3
"""Check that article replays were captured from the current C# engine, allowing floating-point platform rounding."""

import argparse
import json
import math
from pathlib import Path


def compare(expected, actual, path):
    if isinstance(expected, dict):
        if not isinstance(actual, dict) or expected.keys() != actual.keys():
            raise AssertionError(f"{path}: object fields changed")
        for key, value in expected.items():
            compare(value, actual[key], f"{path}.{key}")
    elif isinstance(expected, list):
        if not isinstance(actual, list) or len(expected) != len(actual):
            raise AssertionError(f"{path}: array size changed")
        for index, (left, right) in enumerate(zip(expected, actual)):
            compare(left, right, f"{path}[{index}]")
    elif isinstance(expected, (int, float)) and not isinstance(expected, bool):
        if not isinstance(actual, (int, float)) or isinstance(actual, bool) or not math.isfinite(actual):
            raise AssertionError(f"{path}: expected a finite number")
        if not math.isclose(expected, actual, rel_tol=1e-8, abs_tol=1e-12):
            raise AssertionError(f"{path}: {expected} != {actual}")
    elif expected != actual:
        raise AssertionError(f"{path}: {expected!r} != {actual!r}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("expected", type=Path)
    parser.add_argument("actual", type=Path)
    arguments = parser.parse_args()
    for domain in ("shop", "operations"):
        filename = f"temporal-{domain}-replay.json"
        compare(json.loads((arguments.expected / filename).read_text()),
                json.loads((arguments.actual / filename).read_text()), filename)
        print(f"Verified {domain}: timestamps, motion, normal references and forecasts match the engine.")


if __name__ == "__main__":
    main()
