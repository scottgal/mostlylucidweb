namespace Mostlylucid.Relevance.Learning;

internal sealed class LearningState
{
    public LearningState() { }
    public Dictionary<string, InterventionVariant> Variants { get; set; } = [];
    public List<ContextGroup> Groups { get; set; } = [];
    public Dictionary<string, ArmEvidence> Arms { get; set; } = [];
    public List<string> Recent { get; set; } = [];
    public List<string> PendingResponses { get; set; } = [];
}
internal sealed class ContextGroup
{
    public ContextGroup() { }
    public string Id { get; set; } = "";
    public long Count { get; set; }
    public double[] Mean { get; set; } = [];
    public long Outcomes { get; set; }
    public double[][] Gram { get; set; } = [];
    public double[] Target { get; set; } = [];
    public void Observe(double[] context)
    {
        Count++;
        for (var i = 0; i < Mean.Length; i++) Mean[i] += (context[i] - Mean[i]) / Count;
    }
    public void Learn(double[] descriptor, double reward)
    {
        var x = new[] { 1.0 }.Concat(descriptor).ToArray();
        if (Target.Length == 0) { Target = new double[x.Length]; Gram = x.Select(_ => new double[x.Length]).ToArray(); }
        for (var i = 0; i < x.Length; i++)
        {
            Target[i] += x[i] * reward;
            for (var j = 0; j < x.Length; j++) Gram[i][j] += x[i] * x[j];
        }
        Outcomes++;
    }
    public double[] Coefficients(double penalty)
    {
        if (Target.Length == 0) return [];
        // Tiny ridge system, solved with partial pivoting. No neural training or numerical package required.
        var n = Target.Length;
        var a = Gram.Select((row, i) => row.Concat(new[] { Target[i] }).ToArray()).ToArray();
        for (var i = 0; i < n; i++) a[i][i] += penalty;
        for (var col = 0; col < n; col++)
        {
            var pivot = Enumerable.Range(col, n - col).MaxBy(i => Math.Abs(a[i][col]));
            (a[col], a[pivot]) = (a[pivot], a[col]);
            var divisor = a[col][col];
            for (var j = col; j <= n; j++) a[col][j] /= divisor;
            for (var row = 0; row < n; row++)
            {
                if (row == col) continue;
                var scale = a[row][col];
                for (var j = col; j <= n; j++) a[row][j] -= scale * a[col][j];
            }
        }
        return a.Select(row => row[n]).ToArray();
    }
}
internal sealed class ArmEvidence
{
    public ArmEvidence() { }
    public long Assigned { get; set; }
    public long Completed { get; set; }
    public long Censored { get; set; }
    public long Applied { get; set; }
    public long Viewed { get; set; }
    public long Clicked { get; set; }
    public long Failed { get; set; }
    public double Sum { get; set; }
    public double SumSquares { get; set; }
}
internal sealed class DecisionState
{
    public DecisionState() { }
    public string RequestHash { get; set; } = "";
    public InterventionDecision Decision { get; set; } = null!;
    public List<InterventionEvent> Events { get; set; } = [];
    public HashSet<string> Stages { get; set; } = [];
    public bool Closed { get; set; }
}
internal sealed record Prepared(string RequestHash, string Group, long Support, InterventionVariant[] Variants);
