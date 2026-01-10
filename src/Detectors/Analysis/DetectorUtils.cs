namespace MediaTrust.Detectors.Analysis;

public static class DetectorUtils
{
    public static double CalculateEntropy(byte[] data)
    {
        var freq = new int[256];
        foreach (var b in data)
            freq[b]++;

        double entropy = 0;
        foreach (var f in freq)
        {
            if (f == 0) continue;
            var p = (double)f / data.Length;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }
}
