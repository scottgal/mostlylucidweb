namespace NailSalonScheduling;

public sealed record SlotPreferences(int TargetStart, string? PreviousTechnicianId = null, bool PreferQuiet = false);
public sealed record SlotFeatures(double TimeFit, double PreferredTechnician, double Continuity, double QuietStation);
public sealed record RecommendedSlot(Offer Offer, SlotFeatures Features, double Score);

public static class SlotRecommender
{
    public static IReadOnlyList<long> PreferenceWeights { get; } = Array.AsReadOnly(new long[] { 45, 30, 15, 10 });

    public static IReadOnlyList<RecommendedSlot> Recommend(AppointmentPlanner planner, AppointmentRequest request,
        SlotPreferences preferences, IReadOnlyList<Station> stations, int count = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if (preferences.TargetStart < request.EarliestStart || preferences.TargetStart > request.LatestStart)
            throw new ArgumentException("Target start must be inside the requested arrival window.");
        var stationById = stations.ToDictionary(station => station.Id, StringComparer.Ordinal);
        // Rank the feasible pool before limiting it; early truncation can discard the best personal match.
        var feasible = planner.FindOffers(request, limit: int.MaxValue).Offers;
        return feasible.Select(offer =>
            {
                var features = Describe(offer, request, preferences, stationById[offer.StationId]);
                return new RecommendedSlot(offer, features, Score(features));
            })
            .OrderByDescending(slot => slot.Score)
            .ThenBy(slot => slot.Offer.Start)
            .ThenBy(slot => slot.Offer.TechnicianId, StringComparer.Ordinal)
            .ThenBy(slot => slot.Offer.StationId, StringComparer.Ordinal)
            .Take(count).ToArray();
    }

    public static SlotFeatures Describe(Offer offer, AppointmentRequest request,
        SlotPreferences preferences, Station station)
    {
        var windowMinutes = Math.Max(1, request.LatestStart - request.EarliestStart);
        var distanceMinutes = Math.Abs((double)offer.Start - preferences.TargetStart);
        var timeFit = 1 - Math.Min(1, distanceMinutes / windowMinutes);

        var preferred = request.PreferredTechnicianId is not null &&
            offer.TechnicianId == request.PreferredTechnicianId;
        var continuity = preferences.PreviousTechnicianId is not null &&
            offer.TechnicianId == preferences.PreviousTechnicianId;
        var quiet = preferences.PreferQuiet && station.Quiet;

        return new SlotFeatures(timeFit, preferred ? 1 : 0, continuity ? 1 : 0, quiet ? 1 : 0);
    }

    private static double Score(SlotFeatures features) =>
        (PreferenceWeights[0] / 100.0) * features.TimeFit +
        (PreferenceWeights[1] / 100.0) * features.PreferredTechnician +
        (PreferenceWeights[2] / 100.0) * features.Continuity +
        (PreferenceWeights[3] / 100.0) * features.QuietStation;
}
