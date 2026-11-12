# Packing and calendar examples

Companion to [Packing Algorithms in C# From Boxes to Restaurant Bookings](../../Mostlylucid/Markdown/packing-algorithms-in-csharp.md).

Requires the .NET 10 SDK. No extra NuGet packages.

From the repository root:

```bash
dotnet run --project samples/PackingCalendars/PackingCalendars.csproj
dotnet run --project samples/PackingCalendars/PackingCalendars.csproj -- --check
```

The demo compares first fit, best fit and first-fit decreasing, then finds an accessible restaurant table for a three-person party. `MultiResourcePacking.cs` adds a small example of checking independent CPU and memory limits; its boundary cases run with `--check`. Time is measured in minutes from 18:00 during a single service.

`--check` runs boundary cases and 500 generated scenarios, comparing gap finding with an independent minute-by-minute oracle and verifying packing capacity and item preservation. Failures throw and return a nonzero exit code.

`BoxGeometry.cs` checks axis-aligned 3D box containment, up to six orientations, and overlap. `SphereGeometry.cs` checks sphere overlap. The article walks through a three-carton layout and interlocking L-shaped cell sets. These are geometry building blocks and worked layouts, rather than complete 3D or irregular-shape optimisers.

The restaurant search returns suggestions, one earliest start per feasible table-gap pair. It does not reserve tables, coordinate concurrent requests, join tables or optimise a batch of reservations. Candidate ranking prefers earliest arrival, then fewer unused seats, then preservation of an unneeded accessibility feature, with table ID as a deterministic tie break.
