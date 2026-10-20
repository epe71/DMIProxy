namespace DMIProxy.ApplicationService;

public class TrimForecast
{
    public static string TrimText(string text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        text = text.Replace("Temp.", "Temperatur", StringComparison.InvariantCultureIgnoreCase);

        text = CutTextFrom(text, "I aften");
        text = CutTextFrom(text, "I nat");
        text = CutTextFrom(text, "I morgen");
        text = CutTextFrom(text, "Mandag");
        text = CutTextFrom(text, "Tirsdag");
        text = CutTextFrom(text, "Onsdag");
        text = CutTextFrom(text, "Torsdag");
        text = CutTextFrom(text, "Fredag");
        text = CutTextFrom(text, "Lørdag");
        text = CutTextFrom(text, "Søndag");
        text = CutTextFrom(text, "I løbet af aftenen");

        return text;
    }

    private static string CutTextFrom(string text, string cutAfter)
    {
        var index = text.IndexOf(cutAfter, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            if (index > 0)
            {
                var precedingText = text.AsSpan(0, index).TrimEnd();
                if (precedingText.Length > 0 && ".!?".Contains(precedingText[^1], StringComparison.OrdinalIgnoreCase))
                {
                    return text.Substring(0, index).TrimEnd();
                }
            }

            index = text.IndexOf(cutAfter, index + cutAfter.Length, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}
