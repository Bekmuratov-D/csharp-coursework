namespace ClockCore;

/// <summary>Переводит время в разговорную русскую словесную форму ("десять минут восьмого", "без пяти два").</summary>
public static class RussianTimeWords
{
    private static readonly string[] UnitsNominativeFeminine =
        ["", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять"];

    private static readonly string[] UnitsGenitiveFeminine =
        ["", "одной", "двух", "трёх", "четырёх", "пяти", "шести", "семи", "восьми", "девяти"];

    private static readonly string[] Teens =
        ["десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать",
         "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"];

    private static readonly string[] TeensGenitive =
        ["десяти", "одиннадцати", "двенадцати", "тринадцати", "четырнадцати",
         "пятнадцати", "шестнадцати", "семнадцати", "восемнадцати", "девятнадцати"];

    // index 0 represents the hour "12" (noon/midnight)
    private static readonly string[] HourNominative =
        ["двенадцать", "час", "два", "три", "четыре", "пять", "шесть",
         "семь", "восемь", "девять", "десять", "одиннадцать"];

    private static readonly string[] HourOrdinalGenitive =
        ["двенадцатого", "первого", "второго", "третьего", "четвёртого", "пятого",
         "шестого", "седьмого", "восьмого", "девятого", "десятого", "одиннадцатого"];

    public static string ToWords(TimeOnly time)
    {
        var hour12 = time.Hour % 12;
        var nextHour12 = (hour12 + 1) % 12;
        var minute = time.Minute;

        var phrase = minute switch
        {
            0 => HourClockFull(hour12),
            <= 29 => $"{CardinalNominativeFeminine(minute)} {MinuteNoun(minute)} {HourOrdinalGenitive[nextHour12]}",
            30 => $"половина {HourOrdinalGenitive[nextHour12]}",
            _ => $"без {CardinalGenitiveFeminine(60 - minute)} {HourNominative[nextHour12]}"
        };

        return $"{phrase} {DayPart(time.Hour)}";
    }

    private static string HourClockFull(int hour12) => hour12 switch
    {
        0 => "двенадцать часов",
        1 => "час",
        _ => $"{HourNominative[hour12]} часов"
    };

    private static string CardinalNominativeFeminine(int n) =>
        n < 10 ? UnitsNominativeFeminine[n]
        : n < 20 ? Teens[n - 10]
        : n % 10 == 0 ? "двадцать" : $"двадцать {UnitsNominativeFeminine[n % 10]}";

    private static string CardinalGenitiveFeminine(int n) =>
        n < 10 ? UnitsGenitiveFeminine[n]
        : n < 20 ? TeensGenitive[n - 10]
        : n % 10 == 0 ? "двадцати" : $"двадцати {UnitsGenitiveFeminine[n % 10]}";

    private static string MinuteNoun(int n)
    {
        var mod100 = n % 100;
        if (mod100 is >= 11 and <= 14) return "минут";

        return (n % 10) switch
        {
            1 => "минута",
            2 or 3 or 4 => "минуты",
            _ => "минут"
        };
    }

    private static string DayPart(int hour24) => hour24 switch
    {
        >= 4 and < 12 => "утра",
        >= 12 and < 17 => "дня",
        >= 17 and < 24 => "вечера",
        _ => "ночи"
    };
}
