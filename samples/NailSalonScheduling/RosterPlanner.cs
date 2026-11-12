using PackingCalendars;

namespace NailSalonScheduling;

public static class RosterPlanner
{
    public static Roster Build(IReadOnlyList<Technician> technicians, IEnumerable<ShiftNeed> needs)
    {
        foreach (var technician in technicians)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technician.MaxDailyMinutes);

        var resources = technicians.Select(technician => new CalendarResource(technician.Id, 1,
            technician.Availability.SelectMany(window =>
                Availability.FindGaps(window, technician.TimeOff, minimumLength: 1)).ToArray()));
        var calendar = new CapacityCalendar(resources);
        var assigned = new List<ShiftAssignment>();
        var unfilled = new List<ShiftNeed>();
        var pending = needs.ToList();

        List<Technician> Eligible(ShiftNeed need)
        {
            var eligible = new List<Technician>();
            foreach (var technician in technicians)
            {
                var hasSkill = need.RequiredService is null || technician.Services.Contains(need.RequiredService);
                if (!hasSkill) continue;

                var assignedMinutes = assigned.Where(shift => shift.TechnicianId == technician.Id)
                    .Sum(shift => (long)shift.Time.Length);
                if (assignedMinutes + need.Time.Length > technician.MaxDailyMinutes) continue;

                var demand = new Demand(technician.Id, need.Time);
                if (calendar.CanReserve(new[] { demand })) eligible.Add(technician);
            }
            return eligible;
        }

        while (pending.Count > 0)
        {
            // Recalculate after every assignment; the remaining feasible choices have changed.
            var next = pending.Select(need => (Need: need, Candidates: Eligible(need)))
                .OrderBy(item => item.Candidates.Count)
                .ThenBy(item => item.Need.Time.Start)
                .ThenBy(item => item.Need.Id, StringComparer.Ordinal).First();
            pending.Remove(next.Need);
            var choice = next.Candidates.OrderBy(technician => assigned
                    .Where(shift => shift.TechnicianId == technician.Id).Sum(shift => (long)shift.Time.Length))
                .ThenBy(technician => technician.Id, StringComparer.Ordinal).FirstOrDefault();
            if (choice is null)
            {
                unfilled.Add(next.Need);
                continue;
            }
            if (!calendar.TryReserve(new[] { new Demand(choice.Id, next.Need.Time) }))
                throw new InvalidOperationException("The draft calendar changed unexpectedly.");
            assigned.Add(new ShiftAssignment(next.Need.Id, choice.Id, next.Need.Time));
        }
        return new Roster(assigned, unfilled);
    }
}
