using DMIProxy.BusinessEntity.MetObs;

namespace DMIProxyTests.BusinessEntity.MetObs;

[TestClass]
public class PropertiesTests
{
    /// <summary>
    /// Verifies ThisHour returns expected results for a set of observed times around the 60-minute boundary.
    /// Conditions tested:
    /// - observed == now (should be within hour)
    /// - observed 59 minutes 59 seconds ago (should be within hour)
    /// - observed exactly 60 minutes ago relative to captured reference time (should be outside hour)
    /// - observed 61 minutes ago (should be outside hour)
    /// - observed 1 minute in the future (should be within hour)
    /// - observed 1 day in the future (should be within hour)
    /// Expected: boolean indicating whether observed time is less than 60 minutes before DateTime.UtcNow at method invocation.
    /// </summary>
    [TestMethod]
    public void ThisHour_VariousObservedTimes_ReturnsExpectedBoolean()
    {
        // Arrange
        var referenceNow = DateTime.UtcNow;

        var cases = new (string CaseName, DateTime Observed, bool Expected)[]
        {
            ("Now", referenceNow, true),
            // 59 minutes 59 seconds ago -> definitely less than 60 minutes
            ("59m59s_Ago", referenceNow.AddMinutes(-60).AddSeconds(1), true),
            // Captured reference used to create exactly 60 minutes ago; since DateTime.UtcNow at call will be >= referenceNow,
            // span will be >= 60 and therefore ThisHour should return false.
            ("Exactly_60_Minutes_Ago", referenceNow.AddMinutes(-60), false),
            ("61_Minutes_Ago", referenceNow.AddMinutes(-61), false),
            ("1_Minute_In_Future", referenceNow.AddMinutes(1), true),
            ("1_Day_In_Future", referenceNow.AddDays(1), true),
        };

        foreach (var (caseName, observedValue, expected) in cases)
        {
            // Act
            var sut = new Properties
            {
                observed = observedValue
            };

            bool actual;
            try
            {
                actual = sut.ThisHour();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Case '{caseName}' threw an unexpected exception: {ex}");
                return; // unreachable but keeps flow clear
            }

            // Assert
            Assert.AreEqual(expected, actual, $"Case '{caseName}' expected {expected} but was {actual}. Observed={observedValue:o}, ReferenceNow={referenceNow:o}");
        }
    }

    /// <summary>
    /// Verifies that Rain1h returns the stored value when the parameterId equals "precip_past1h".
    /// Input conditions: parameterId set to exact match "precip_past1h" and various double value edge cases.
    /// Expected: method returns the same double value (NaN validated via Double.IsNaN).
    /// </summary>
    [TestMethod]
    public void Rain1h_ParameterIsPrecipPast1h_ReturnsValue_ForVariousDoubleEdgeCases()
    {
        // Arrange
        var testValues = new List<double>
        {
            0.0,
            -1.5,
            1.5,
            double.MinValue,
            double.MaxValue,
            double.PositiveInfinity,
            double.NegativeInfinity,
            double.NaN
        };

        foreach (var expected in testValues)
        {
            var sut = new Properties
            {
                parameterId = "precip_past1h",
                value = expected
            };

            // Act
            double actual = sut.Rain1h();

            // Assert
            if (double.IsNaN(expected))
            {
                Assert.IsTrue(double.IsNaN(actual), $"Expected NaN for input value {expected}.");
            }
            else
            {
                Assert.AreEqual(expected, actual, $"Rain1h should return the original value for input {expected}.");
            }
        }
    }

}