namespace DMIProxy.BusinessEntity.UnitTests;

/// <summary>
/// Tests for DateTimeProvider.
/// </summary>
[TestClass]
public class DateTimeProviderTests
{
    /// <summary>
    /// Verifies that UtcNow returns a DateTime with Kind = Utc and that the returned time
    /// is close to System.DateTime.UtcNow within a provided tolerance.
    /// Input conditions: toleranceMilliseconds - maximum allowed difference in milliseconds
    /// between provider.UtcNow and System.DateTime.UtcNow at the time of the call.
    /// Expected result: No exception; result.Kind == DateTimeKind.Utc; absolute difference
    /// between times is less than or equal to toleranceMilliseconds.
    /// </summary>
    /// <param name="toleranceMilliseconds">Allowed difference in milliseconds.</param>
    [TestMethod]
    [DataRow(1000)]
    [DataRow(5000)]
    public void UtcNow_ReturnsUtcDateTimeWithinTolerance(int toleranceMilliseconds)
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        DateTime providerValue = provider.UtcNow;
        DateTime systemValue = DateTime.UtcNow;
        double deltaMs = Math.Abs((providerValue - systemValue).TotalMilliseconds);

        // Assert
        Assert.AreEqual(DateTimeKind.Utc, providerValue.Kind, "UtcNow must return a DateTime with Kind == Utc.");
        Assert.IsTrue(deltaMs <= toleranceMilliseconds,
            $"UtcNow difference to System.DateTime.UtcNow was {deltaMs}ms which exceeds tolerance {toleranceMilliseconds}ms.");
    }

    /// <summary>
    /// Ensures calling UtcNow does not throw and returns a value within DateTime range.
    /// Input conditions: none.
    /// Expected result: No exception thrown; returned value is between DateTime.MinValue and DateTime.MaxValue
    /// and has Kind == Utc.
    /// </summary>
    [TestMethod]
    public void UtcNow_DoesNotThrowAndReturnsValidDateTime()
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        DateTime result = provider.UtcNow;

        // Assert
        Assert.AreEqual(DateTimeKind.Utc, result.Kind, "UtcNow must return DateTimeKind.Utc.");
        Assert.IsTrue(result >= DateTime.MinValue && result <= DateTime.MaxValue,
            "UtcNow returned a DateTime outside the valid DateTime range.");
    }

    /// <summary>
    /// Verifies that DateTimeProvider.Now returns a non-default local DateTime and that it is consistent
    /// with the system times (DateTime.Now and DateTime.UtcNow) within the provided tolerance.
    /// Input conditions: toleranceMillis - maximum allowed difference in milliseconds between provider values and system values.
    /// Expected: The returned DateTime has Kind == Local, is not DateTime.MinValue/MaxValue/default, and is within toleranceMillis
    /// of both DateTime.Now and DateTime.UtcNow (after conversion to UTC).
    /// </summary>
    /// <param name="toleranceMillis">Allowed difference in milliseconds between provider's timestamps and system timestamps.</param>
    [TestMethod]
    [DataRow(50)]
    [DataRow(1000)]
    [DataRow(5000)]
    public void Now_IsLocalAndMatchesSystemTimeWithinTolerance(int toleranceMillis)
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        DateTime providerNow = provider.Now;
        DateTime systemNow = DateTime.Now;
        DateTime providerNowUtc = providerNow.ToUniversalTime();
        DateTime systemUtcNow = DateTime.UtcNow;

        // Assert
        // 1) Kind should be Local
        Assert.AreEqual(DateTimeKind.Local, providerNow.Kind, "Now should have DateTimeKind.Local.");

        // 2) Should not be default(min) or default(max)
        Assert.AreNotEqual(default(DateTime), providerNow, "Now should not be default(DateTime).");

        // 3) providerNow should be close to DateTime.Now within tolerance
        double diffMillisLocal = Math.Abs((providerNow - systemNow).TotalMilliseconds);
        Assert.IsTrue(diffMillisLocal <= toleranceMillis,
            $"Provider.Now differs from DateTime.Now by {diffMillisLocal}ms which exceeds tolerance {toleranceMillis}ms.");

        // 4) providerNow.ToUniversalTime() should be close to DateTime.UtcNow within tolerance
        double diffMillisUtc = Math.Abs((providerNowUtc - systemUtcNow).TotalMilliseconds);
        Assert.IsTrue(diffMillisUtc <= toleranceMillis,
            $"Provider.Now.ToUniversalTime() differs from DateTime.UtcNow by {diffMillisUtc}ms which exceeds tolerance {toleranceMillis}ms.");
    }
}