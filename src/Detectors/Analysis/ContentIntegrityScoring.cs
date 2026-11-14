namespace MediaTrust.Detectors.Analysis;

public static class ContentIntegrityScoring
{
    public static double CalculateScore(bool headerMatch, double entropy)
    {
        var score = 0.0;
        if (!headerMatch) score += 0.6;
        if (entropy > 7.2) score += 0.4;

        return Math.Min(score, 1.0);
    }
}
