using System;

namespace SmartMath
{
    public class Decimals : IDecimals
    {
        private const char dot = '.';

        public Decimals() { }

        public string AddDecimals(string amountOne, string amountTwo)
        {
            throw new NotImplementedException();
        }

        public string ConvertToDecimalFromSatoshis(ulong amount)
        {
            string amountString = amount.ToString();
            int amountLength = amountString.Length;
            const int decimalLength = 8;

            if (amountString.Length <= 8)
            {
                for (var i = 0; i < decimalLength - amountLength; i++)
                {
                    amountString = $"0{amountString}";
                }

                amountString = $"0.{amountString}";

                return amountString;
            }
            else
            {
                return amountString.Insert(amountString.Length - decimalLength, ".");
            }
        }

        public ulong ConvertToSatoshisFromDecimal(string amount)
        {
            var delimiter = GetDelimiterFromDecimal(amount);

            var splitAmount = amount.Split(dot);

            ulong.TryParse(splitAmount[0], out ulong integer);
            ulong.TryParse(splitAmount[1], out ulong fractional);

            ulong integerAmount = integer * delimiter;
            ulong fractionalAmount = fractional;

            return integerAmount + fractionalAmount;
        }

        public ulong GetDelimiterFromDecimal(string amount)
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

        public string SubtractDecimals(string amountOne, string amountTwo)
        {
            throw new NotImplementedException();
        }
    }
}
