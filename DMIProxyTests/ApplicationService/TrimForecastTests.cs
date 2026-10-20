namespace DMIProxy.ApplicationService.UnitTests;

[TestClass]
public class TrimForecastTests
{
    [TestMethod]
    [DataRow("Temp. 20°C", "Temperatur 20°C")]
    [DataRow("temp. 20°C", "Temperatur 20°C")]
    public void TrimText_TemperatureAbbreviation_ExpandsCaseInsensitively(string text, string expected)
    {
        // Arrange

        // Act
        var result = TrimForecast.TrimText(text);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("I aften og i nat skyet med stedvis regn. Temp. mellem 7 og 10 grader og let til frisk vind omkring øst. I morgen bliver der først på dagen stedvist lidt sol, men efterhånden bliver det overskyet sydfra",
             "I aften og i nat skyet med stedvis regn. Temperatur mellem 7 og 10 grader og let til frisk vind omkring øst.")]
    [DataRow("Forecast text. I aften bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. I nat bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. I morgen bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Mandag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Tirsdag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Onsdag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Torsdag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Fredag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Lørdag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. Søndag bliver det klart.", "Forecast text.")]
    [DataRow("Forecast text. I løbet af aftenen bliver det klart.", "Forecast text.")]
    public void TrimText_ForecastSectionMarker_RemovesMarkerAndFollowingText(string text, string expected)
    {
        // Arrange

        // Act
        var result = TrimForecast.TrimText(text);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TrimText_NoForecastSectionMarker_ReturnsUnchangedText()
    {
        // Arrange
        const string text = "The forecast remains unchanged.";

        // Act
        var result = TrimForecast.TrimText(text);

        // Assert
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void TrimText_Null_ReturnsEmptyString()
    {
        // Arrange

        // Act
        var result = TrimForecast.TrimText(null!);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}
