using System;

namespace SmartMath
{
    public class Decimals : IDecimals
    {
        private const char dot = '.';

        public Decimals() { }

        public ulong GetDelimiterBasedOnAmount(string amount)
        {
            string[] splitAmount = amount.Split(dot);
            string delimiter = "1";
            int fractionalLength = splitAmount[1].Length;

            for (int i = 0; i < fractionalLength; i++)
            {
                delimiter = $"{delimiter}0";
            }

            ulong.TryParse(delimiter, out ulong formattedDelimiter);

            return formattedDelimiter;
        }
    }
}
