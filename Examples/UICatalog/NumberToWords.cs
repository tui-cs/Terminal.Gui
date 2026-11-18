#nullable enable

namespace UICatalog;

public static class NumberToWords
{
    private static readonly string [] _tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    private static readonly string [] _units =
    [
        "Zero",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine",
        "Ten",
        "Eleven",
        "Twelve",
        "Thirteen",
        "Fourteen",
        "Fifteen",
        "Sixteen",
        "Seventeen",
        "Eighteen",
        "Nineteen"
    ];

    public static string Convert (long i)
    {
        if (i < 20)
        {
            return _units [i];
        }

        if (i < 100)
        {
            return _tens [i / 10] + (i % 10 > 0 ? " " + Convert (i % 10) : "");
        }

        if (i < 1000)
        {
            return _units [i / 100]
                   + " Hundred"
                   + (i % 100 > 0 ? " And " + Convert (i % 100) : "");
        }

        if (i < 100000)
        {
            return Convert (i / 1000)
                   + " Thousand "
                   + (i % 1000 > 0 ? " " + Convert (i % 1000) : "");
        }

        if (i < 10000000)
        {
            return Convert (i / 100000)
                   + " Lakh "
                   + (i % 100000 > 0 ? " " + Convert (i % 100000) : "");
        }

        if (i < 1000000000)
        {
            return Convert (i / 10000000)
                   + " Crore "
                   + (i % 10000000 > 0 ? " " + Convert (i % 10000000) : "");
        }

        return Convert (i / 1000000000)
               + " Arab "
               + (i % 1000000000 > 0 ? " " + Convert (i % 1000000000) : "");
    }
}
