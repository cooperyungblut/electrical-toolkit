namespace ElectricalToolkit.Core.Panels;

public static class Imbalance
{
    public static double Percent(IReadOnlyCollection<double> phaseTotals)
    {
        double average = phaseTotals.Average();
        if (average == 0) return 0;
        double biggestDiff = phaseTotals.Max(phase => Math.Abs(phase - average));
        return (biggestDiff / average) * 100;
    }
}