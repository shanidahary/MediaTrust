using MediaTrust.Detectors.Analysis;

namespace MediaTrust.Detectors.Tests;

public sealed class DetectorUtilsTests
{
    [Fact]
    public void CalculateEntropy_EmptyData_ReturnsZero()
    {
        // Arrange
        byte[] data = [];

        // Act
        var entropy = DetectorUtils.CalculateEntropy(data);

        // Assert
        Assert.Equal(0.0, entropy);
    }

    [Fact]
    public void CalculateEntropy_RepeatedByte_ReturnsZero()
    {
        // Arrange
        var data = Enumerable.Repeat((byte)42, 4096).ToArray();

        // Act
        var entropy = DetectorUtils.CalculateEntropy(data);

        // Assert
        Assert.Equal(0.0, entropy);
    }

    [Fact]
    public void CalculateEntropy_UniformByteDistribution_ReturnsEight()
    {
        // Arrange
        var data = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

        // Act
        var entropy = DetectorUtils.CalculateEntropy(data);

        // Assert
        Assert.Equal(8.0, entropy, precision: 10);
    }
}
