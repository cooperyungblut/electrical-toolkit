namespace ElectricalToolkit.Core.Panels;

public static class Objective
{
    // What the balancers minimize. Lower means more even phases.
    public static double SumOfSquares(IEnumerable<double> phaseTotals) => phaseTotals.Sum(t => t * t);
}