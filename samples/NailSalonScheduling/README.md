# Nail salon scheduling

Part 2 of the [packing and scheduling articles](../../Mostlylucid/Markdown/packing-algorithms-in-csharp-part-2.md).

One .NET 10 console application demonstrates employee rostering, client bookings, recommendation attributes and tentative customer preferences learned from past visits. No additional NuGet packages are required. The project links the `TimeRange` and gap finder source from Part 1.

The console is a harness for inspecting and testing the logic. We are deliberately leaving UX design and staff/customer screens for later.

```bash
dotnet run --project samples/NailSalonScheduling/NailSalonScheduling.csproj
dotnet run --project samples/NailSalonScheduling/NailSalonScheduling.csproj -- --check
```

- `CapacityCalendar`: available windows, cumulative capacity and all-or-nothing resource bundles in a single-threaded in-memory demo.
- `RosterPlanner`: assign continuous work blocks using skills, availability minus time off, daily-minute limits and a greedy preference for less-assigned staff. Unfilled needs remain visible.
- `AppointmentPlanner`: use a reviewed complete roster as technician availability; require a capable technician, suitable station capacity and a free client interval. Service and reset times can differ by resource. Blocked-client and employee-exclusion rules precede candidate generation. Confirmation reconstructs and rechecks the exact chosen offer.
- `SlotRecommender`: rank the complete feasible candidate pool with documented time, preferred-technician, continuity and quiet-station features. This is an inspectable weighted baseline, not a trained model or an integration with the Relevance package.

- `CustomerLearning`: estimate service, technician and time-of-day preferences from visits before the day being planned, with a minimum-support threshold and an explicit weekday fallback. The demo turns this profile into a proactive suggestion and still checks current availability.

Time is integer minutes from 09:00 on one example day. Appointment arrivals use a five-minute grid. Station capacity represents genuinely interchangeable seats. Breaks divide work windows; a continuous roster work block cannot cross an absence. The demo requires reset time to fit inside staff working hours and station opening hours.

These are teaching policies and independent code examples. The demo does not implement persistent multi-user transactions, a roster-editing workflow, customer authentication, group treatments or a workforce optimiser. Publishing a changed roster in a real application must account for already committed appointments. No algorithm in the sample silently moves confirmed appointments.
