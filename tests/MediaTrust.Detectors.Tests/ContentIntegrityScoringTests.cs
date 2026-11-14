using MediaTrust.Detectors.Analysis;

namespace MediaTrust.Detectors.Tests;

public sealed class ContentIntegrityScoringTests
{
    [Theory]
    [InlineData(true, 0.0, 0.0)]
    [InlineData(true, 7.2, 0.0)]
    [InlineData(true, 7.200001, 0.4)]
    [InlineData(true, 8.0, 0.4)]
    [InlineData(false, 0.0, 0.6)]
    [InlineData(false, 7.2, 0.6)]
    [InlineData(false, 7.200001, 1.0)]
    [InlineData(false, 8.0, 1.0)]
    public void CalculateScore_ReturnsExpectedRiskForHeaderAndEntropy(
        bool headerMatch, double entropy, double expectedScore)
    {
        // Arrange: inputs cover both header states and the exact entropy boundary.

        // Act
        var score = ContentIntegrityScoring.CalculateScore(headerMatch, entropy);

        // Assert
        Assert.Equal(expectedScore, score, precision: 10);
        Assert.InRange(score, 0.0, 1.0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CalculateScore_ExtremeEntropy_NeverExceedsOne(bool headerMatch)
    {
        // Arrange
        const double entropy = double.MaxValue;

        // Act
        var score = ContentIntegrityScoring.CalculateScore(headerMatch, entropy);

        // Assert
        Assert.InRange(score, 0.0, 1.0);
    }
}
