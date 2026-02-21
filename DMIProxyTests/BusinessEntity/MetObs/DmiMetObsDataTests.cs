namespace DMIProxy.BusinessEntity.MetObs.UnitTests;

[TestClass]
public class DmiMetObsDataTests
{
    /// <summary>
    /// Verifies that when the features collection is empty RainToday returns 0.0.
    /// Input: features = empty list.
    /// Expected: 0.0 (no rainfall).
    /// </summary>
    [TestMethod]
    public void RainToday_NoFeatures_ReturnsZero()
    {
        // Arrange
        var data = new DmiMetObsData
        {
            features = new List<Feature>()
        };

        // Act
        var result = data.RainToday();

        // Assert
        Assert.AreEqual(0.0, result, "Expected RainToday to be 0.0 when there are no features.");
    }

    /// <summary>
    /// Verifies that features observed before today are excluded from the sum.
    /// Input: one feature observed yesterday with a valid rain parameter.
    /// Expected: 0.0 (feature not counted).
    /// </summary>
    [TestMethod]
    public void RainToday_NoTodayFeatures_ReturnsZero()
    {
        // Arrange
        var yesterdayObserved = DateTime.Today.AddDays(-1).AddHours(12); // safely yesterday
        var features = new List<Feature>
        {
            new Feature
            {
                properties = new Properties
                {
                    observed = yesterdayObserved,
                    parameterId = "precip_past1h",
                    value = 5.0
                }
            }
        };
        var data = new DmiMetObsData
        {
            features = features
        };

        // Act
        var result = data.RainToday();

        // Assert
        Assert.AreEqual(0.0, result, "Features observed before today should not be included in RainToday.");
    }

    /// <summary>
    /// Verifies that RainToday sums rain values for features observed today.
    /// Input: two features observed today with precip_past1h values 1.5 and 2.5.
    /// Expected: sum = 4.0.
    /// </summary>
    [TestMethod]
    public void RainToday_TodayFeatures_SumsValues()
    {
        // Arrange
        var now = DateTime.Now;
        var features = new List<Feature>
        {
            new Feature
            {
                properties = new Properties
                {
                    observed = now,
                    parameterId = "precip_past1h",
                    value = 1.5
                }
            },
            new Feature
            {
                properties = new Properties
                {
                    observed = now,
                    parameterId = "precip_past1h",
                    value = 2.5
                }
            }
        };
        var data = new DmiMetObsData
        {
            features = features
        };

        // Act
        var result = data.RainToday();

        // Assert
        Assert.AreEqual(4.0, result, 1e-9, "RainToday should sum rain values for features from today.");
    }

    /// <summary>
    /// Verifies that if a selected today's feature contains NaN as its rain value, the result is NaN.
    /// Input: one feature observed today with value = NaN and parameterId precip_past1h.
    /// Expected: result is NaN.
    /// </summary>
    [TestMethod]
    public void RainToday_TodayFeatureWithNaN_ReturnsNaN()
    {
        // Arrange
        var features = new List<Feature>
        {
            new Feature
            {
                properties = new Properties
                {
                    observed = DateTime.Now,
                    parameterId = "precip_past1h",
                    value = double.NaN
                }
            }
        };
        var data = new DmiMetObsData
        {
            features = features
        };

        // Act
        var result = data.RainToday();

        // Assert
        Assert.IsTrue(double.IsNaN(result), "RainToday should return NaN when any included feature's Rain1h() is NaN.");
    }

    /// <summary>
    /// Verifies that summing with positive infinity yields positive infinity.
    /// Input: two today's features, one with PositiveInfinity and one with finite value.
    /// Expected: PositiveInfinity.
    /// </summary>
    [TestMethod]
    public void RainToday_WithPositiveInfinity_ReturnsPositiveInfinity()
    {
        // Arrange
        var features = new List<Feature>
        {
            new Feature
            {
                properties = new Properties
                {
                    observed = DateTime.Now,
                    parameterId = "precip_past1h",
                    value = double.PositiveInfinity
                }
            },
            new Feature
            {
                properties = new Properties
                {
                    observed = DateTime.Now,
                    parameterId = "precip_past1h",
                    value = 1.0
                }
            }
        };
        var data = new DmiMetObsData
        {
            features = features
        };

        // Act
        var result = data.RainToday();

        // Assert
        Assert.IsTrue(double.IsPositiveInfinity(result), "Positive infinity in the inputs should result in PositiveInfinity for the sum.");
    }

    /// <summary>
    /// Ensures that when no feature in the features list represents the current hour, Rain1h returns 0.0.
    /// Condition: features is an empty list.
    /// Expected result: 0.0 returned without exception.
    /// </summary>
    [TestMethod]
    public void Rain1h_NoFeatures_ReturnsZero()
    {
        // Arrange
        var sut = new DmiMetObsData
        {
            features = new List<Feature>()
        };

        // Act
        var result = sut.Rain1h();

        // Assert
        Assert.AreEqual(0.0, result);
    }

    /// <summary>
    /// Ensures that when a feature for the current hour exists and is a rain measurement
    /// (parameterId == "precip_past1h"), Rain1h returns the properties.value.
    /// Condition: single Feature with observed == now, parameterId == "precip_past1h", value finite.
    /// Expected result: returned value equals the property's value.
    /// </summary>
    [TestMethod]
    public void Rain1h_MatchingFeatureThisHour_ReturnsValue()
    {
        // Arrange
        double expected = 12.34;
        var feature = new Feature
        {
            properties = new Properties
            {
                observed = DateTime.UtcNow,
                parameterId = "precip_past1h",
                value = expected
            }
        };

        var sut = new DmiMetObsData
        {
            features = new List<Feature> { feature }
        };

        // Act
        var result = sut.Rain1h();

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Ensures that Rain1h returns special double values as provided by the matched Feature.
    /// Condition: single Feature matching this hour with NaN value and precip_past1h parameter.
    /// Expected result: result is NaN.
    /// </summary>
    [TestMethod]
    public void Rain1h_MatchingFeatureWithNaNValue_ReturnsNaN()
    {
        // Arrange
        var feature = new Feature
        {
            properties = new Properties
            {
                observed = DateTime.UtcNow,
                parameterId = "precip_past1h",
                value = double.NaN
            }
        };

        var sut = new DmiMetObsData
        {
            features = new List<Feature> { feature }
        };

        // Act
        var result = sut.Rain1h();

        // Assert
        Assert.IsTrue(double.IsNaN(result));
    }

    /// <summary>
    /// Verify that when no feature is marked as belonging to this month the result is zero.
    /// Input: features with observed dates before the start of the current month.
    /// Expected: RainThisMonth returns 0.0.
    /// </summary>
    [TestMethod]
    public void RainThisMonth_NoFeaturesThisMonth_ReturnsZero()
    {
        // Arrange
        var oldObserved = DateTime.Today.AddMonths(-1).AddDays(-1);
        var props = new Properties
        {
            created = DateTime.UtcNow,
            observed = oldObserved,
            parameterId = "precip_past1h",
            stationId = string.Empty,
            value = 5.5,
            noValuesInCalculation = 0,
            timeResolution = string.Empty,
            qcStatus = string.Empty,
            from = oldObserved,
            to = oldObserved,
            calculatedAt = DateTime.UtcNow,
            validity = true
        };
        var features = new List<Feature>
        {
            new Feature { id = Guid.NewGuid(), type = "Feature", properties = props }
        };
        var sut = new DmiMetObsData
        {
            features = features,
            numberReturned = features.Count,
            links = new List<Link>(),
            timeStamp = DateTime.UtcNow,
            type = "FeatureCollection"
        };

        // Act
        var result = sut.RainThisMonth();

        // Assert
        Assert.AreEqual(0.0, result);
    }

    /// <summary>
    /// Verify that RainThisMonth sums rain measurements from features observed this month and rounds to 2 decimals.
    /// Input: two features observed this month with precip_past1h values that sum to a value requiring rounding.
    /// Expected: Correct rounded sum (Math.Round behavior with 2 decimals).
    /// </summary>
    [TestMethod]
    public void RainThisMonth_FeaturesThisMonth_SumsAndRoundsCorrectly()
    {
        // Arrange
        var observed = DateTime.Today; // within current month
        var p1 = new Properties
        {
            created = DateTime.UtcNow,
            observed = observed,
            parameterId = "precip_past1h",
            stationId = string.Empty,
            value = 1.235, // will contribute to rounding behavior
            noValuesInCalculation = 0,
            timeResolution = string.Empty,
            qcStatus = string.Empty,
            from = observed,
            to = observed,
            calculatedAt = DateTime.UtcNow,
            validity = true
        };
        var p2 = new Properties
        {
            created = DateTime.UtcNow,
            observed = observed,
            parameterId = "precip_past1h",
            stationId = string.Empty,
            value = 2.001,
            noValuesInCalculation = 0,
            timeResolution = string.Empty,
            qcStatus = string.Empty,
            from = observed,
            to = observed,
            calculatedAt = DateTime.UtcNow,
            validity = true
        };

        var features = new List<Feature>
        {
            new Feature { id = Guid.NewGuid(), type = "Feature", properties = p1 },
            new Feature { id = Guid.NewGuid(), type = "Feature", properties = p2 },
            // Add a non-this-month feature with a non-precip parameter to ensure filtering works and no invocation occurs
            new Feature
            {
                id = Guid.NewGuid(),
                type = "Feature",
                properties = new Properties
                {
                    created = DateTime.UtcNow,
                    observed = DateTime.Today.AddMonths(-1),
                    parameterId = "temperature",
                    stationId = string.Empty,
                    value = 999.9,
                    noValuesInCalculation = 0,
                    timeResolution = string.Empty,
                    qcStatus = string.Empty,
                    from = DateTime.Today.AddMonths(-1),
                    to = DateTime.Today.AddMonths(-1),
                    calculatedAt = DateTime.UtcNow,
                    validity = true
                }
            }
        };

        var sut = new DmiMetObsData
        {
            features = features,
            numberReturned = features.Count,
            links = new List<Link>(),
            timeStamp = DateTime.UtcNow,
            type = "FeatureCollection"
        };

        // Act
        var result = sut.RainThisMonth();

        // Sum = 1.235 + 2.001 = 3.236 -> rounded to 2 decimals = 3.24
        // Assert
        Assert.AreEqual(3.24, result);
    }

    /// <summary>
    /// Verify handling of special double values (NaN, PositiveInfinity, NegativeInfinity) returned by Rain1h.
    /// Input: a feature observed this month with precip_past1h set to NaN/Infinity.
    /// Expected: The resulting value after Sum and Round preserves NaN/Infinity behavior.
    /// </summary>
    [TestMethod]
    public void RainThisMonth_SpecialDoubleValues_PreservesSpecialValues()
    {
        // Arrange / Act / Assert for NaN
        {
            var observed = DateTime.Today;
            var propsNaN = new Properties
            {
                created = DateTime.UtcNow,
                observed = observed,
                parameterId = "precip_past1h",
                stationId = string.Empty,
                value = double.NaN,
                noValuesInCalculation = 0,
                timeResolution = string.Empty,
                qcStatus = string.Empty,
                from = observed,
                to = observed,
                calculatedAt = DateTime.UtcNow,
                validity = true
            };
            var sutNaN = new DmiMetObsData
            {
                features = new List<Feature> { new Feature { id = Guid.NewGuid(), type = "Feature", properties = propsNaN } },
                numberReturned = 1,
                links = new List<Link>(),
                timeStamp = DateTime.UtcNow,
                type = "FeatureCollection"
            };

            var resultNaN = sutNaN.RainThisMonth();
            Assert.IsTrue(double.IsNaN(resultNaN), "Expected result to be NaN when a contributing value is NaN.");
        }

        // PositiveInfinity
        {
            var observed = DateTime.Today;
            var propsPosInf = new Properties
            {
                created = DateTime.UtcNow,
                observed = observed,
                parameterId = "precip_past1h",
                stationId = string.Empty,
                value = double.PositiveInfinity,
                noValuesInCalculation = 0,
                timeResolution = string.Empty,
                qcStatus = string.Empty,
                from = observed,
                to = observed,
                calculatedAt = DateTime.UtcNow,
                validity = true
            };
            var sutPosInf = new DmiMetObsData
            {
                features = new List<Feature> { new Feature { id = Guid.NewGuid(), type = "Feature", properties = propsPosInf } },
                numberReturned = 1,
                links = new List<Link>(),
                timeStamp = DateTime.UtcNow,
                type = "FeatureCollection"
            };

            var resultPosInf = sutPosInf.RainThisMonth();
            Assert.IsTrue(double.IsPositiveInfinity(resultPosInf), "Expected result to be PositiveInfinity when a contributing value is PositiveInfinity.");
        }

        // NegativeInfinity
        {
            var observed = DateTime.Today;
            var propsNegInf = new Properties
            {
                created = DateTime.UtcNow,
                observed = observed,
                parameterId = "precip_past1h",
                stationId = string.Empty,
                value = double.NegativeInfinity,
                noValuesInCalculation = 0,
                timeResolution = string.Empty,
                qcStatus = string.Empty,
                from = observed,
                to = observed,
                calculatedAt = DateTime.UtcNow,
                validity = true
            };
            var sutNegInf = new DmiMetObsData
            {
                features = new List<Feature> { new Feature { id = Guid.NewGuid(), type = "Feature", properties = propsNegInf } },
                numberReturned = 1,
                links = new List<Link>(),
                timeStamp = DateTime.UtcNow,
                type = "FeatureCollection"
            };

            var resultNegInf = sutNegInf.RainThisMonth();
            Assert.IsTrue(double.IsNegativeInfinity(resultNegInf), "Expected result to be NegativeInfinity when a contributing value is NegativeInfinity.");
        }
    }

    /// <summary>
    /// Verifies that AllRecived returns true when the numberReturned equals the features count.
    /// Conditions:
    /// - Tests an empty features list with numberReturned = 0.
    /// - Tests a non-empty features list with numberReturned equal to the list size.
    /// Expected:
    /// - Method returns true for each matching pair.
    /// </summary>
    [TestMethod]
    public void AllRecived_FeaturesCountEqualsNumberReturned_ReturnsTrue()
    {
        // Arrange
        var testCases = new List<(int featuresCount, int numberReturned)>
        {
            (0, 0),   // boundary: empty collection equals zero
            (3, 3)    // typical non-empty equality case
        };

        foreach (var (featuresCount, numberReturned) in testCases)
        {
            var data = new DmiMetObsData
            {
                features = Enumerable.Range(0, featuresCount).Select(_ => new Feature()).ToList(),
                numberReturned = numberReturned
            };

            // Act
            var result = data.AllRecived();

            // Assert
            Assert.IsTrue(result, $"Expected AllRecived to be true when features.Count == numberReturned ({featuresCount}).");
        }
    }

    /// <summary>
    /// Verifies that AllRecived returns false when the numberReturned does not equal the features count.
    /// Conditions tested include:
    /// - features empty but numberReturned > 0
    /// - features non-empty but numberReturned negative
    /// - features small but numberReturned extremely large (int.MaxValue)
    /// - features non-empty but numberReturned is int.MinValue
    /// Expected:
    /// - Method returns false for each mismatched pair and does not throw.
    /// </summary>
    [TestMethod]
    public void AllRecived_FeaturesCountNotEqualNumberReturned_ReturnsFalse()
    {
        // Arrange
        var testCases = new List<(int featuresCount, int numberReturned)>
        {
            (0, 1),                    // empty vs positive
            (2, -1),                   // small positive count vs negative numberReturned
            (1, int.MaxValue),         // cannot match very large numberReturned
            (5, int.MinValue)          // cannot match very small (negative) numberReturned
        };

        foreach (var (featuresCount, numberReturned) in testCases)
        {
            var data = new DmiMetObsData
            {
                features = Enumerable.Range(0, featuresCount).Select(_ => new Feature()).ToList(),
                numberReturned = numberReturned
            };

            // Act
            var result = data.AllRecived();

            // Assert
            Assert.IsFalse(result, $"Expected AllRecived to be false when features.Count ({featuresCount}) != numberReturned ({numberReturned}).");
        }
    }
}