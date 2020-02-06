using Xunit;
using Moq;
using SmartMath;

namespace SmartMath.Tests
{

    public class DecimalsTests
    {
        //private readonly Mock<IDecimals> MockDecimals;
        private readonly IDecimals _decimals;

        public DecimalsTests()
        {
            _decimals = new Decimals();
        }

        [Theory]
        // Minimum price 1 = .0001crs
        [InlineData("1.0000", 10_000, 100_000_000)]
        [InlineData("1.0000", 10_000, 100_000_000)]
        [InlineData("1.0000", 100_000, 1_000_000_000)]
        [InlineData("1.0000", 1_000_000, 10_000_000_000)]
        [InlineData("1.0000", 10_000_000, 100_000_000_000)]
        [InlineData("1.0000", 100_000_000, 1_000_000_000_000)]
        [InlineData("1234.5678", 23_450, 289_506_149_100)]
        [InlineData("19484.7657", 1_000, 194_847_657_000)]
        // 0.23 * 14.7656 = 0.3396088
        [InlineData("0.0230", 147_656, 33_960_880)]
        public void CanCalculate_Amount_FromString(string amount, ulong price, ulong expectedCost)
        {
            ulong delimiter = _decimals.GetDelimiterFromDecimal(amount);

            Assert.True(amount.Length >= 6);

            var splitAmount = amount.Split(".");

            ulong.TryParse(splitAmount[0], out ulong integer);
            ulong.TryParse(splitAmount[1], out ulong fractional);

            ulong integerTotal = integer * delimiter * price;
            ulong fractionalTotal = fractional * price;

            var cost = integerTotal + fractionalTotal;

            Assert.Equal(expectedCost, cost);
        }

        [Theory]
        [InlineData("1.00000001", 100_000_001)]
        [InlineData("1.0000001", 100_000_010)]
        [InlineData("1.000001", 100_000_100)]
        [InlineData("1.00001", 100_001_000)]
        [InlineData("1.0001", 100_010_000)]
        [InlineData("1.001", 100_100_000)]
        [InlineData("1.01", 101_000_000)]
        [InlineData("1.1", 110_000_000)]
        public void Convert_ToSatoshis_FromDecimal(string amount, ulong expectedCost)
        {
            var cost = _decimals.ConvertToSatoshisFromDecimal(amount);

            Assert.Equal(expectedCost, cost);
        }

        [Theory]
        [InlineData(100_000_001, "1.00000001")]
        [InlineData(10_000_001, "0.10000001")]
        [InlineData(12345, "0.00012345")]
        [InlineData(987_654_321, "9.87654321")]
        public void Convert_ToDecimal_FromSatoshis(ulong amount, string expectedCost)
        {
            var cost = _decimals.ConvertToDecimalFromSatoshis(amount);

            Assert.Equal(expectedCost, cost);
        }

        [Theory]
        [InlineData("1.00000001", 100_000_000)]
        [InlineData("1.0000001", 10_000_000)]
        [InlineData("1.000001", 1_000_000)]
        [InlineData("1.00001", 100_000)]
        [InlineData("1.0001", 10_000)]
        [InlineData("1.001", 1_000)]
        [InlineData("1.01", 100)]
        [InlineData("1.1", 10)]
        public void GetDelimiter_FromDecimal(string amount, ulong expectedDelimiter)
        {
            var formattedDelimiter = _decimals.GetDelimiterFromDecimal(amount);

            Assert.Equal(expectedDelimiter, formattedDelimiter);
        }

        //[Theory]
        //[InlineData("1.00000001", "1.00000001", "2.00000002")]
        //[InlineData("4.7654", "3.4732", "8.2386")]
        //public void CanAddTwoDecimalNumbers(string amountOne, string amountTwo, string expectedAmount)
        //{
        //    var formattedAmountOne = SplitAndFormatAmount(amountOne);
        //    var formattedAmountTwo = SplitAndFormatAmount(amountTwo);

        //    ulong totalFractional = formattedAmountOne.fractional + formattedAmountTwo.fractional;

        //    ulong totalInteger = formattedAmountOne.integer + formattedAmountTwo.integer;

        //    if (totalFractional >= 100_000_000)
        //    {
        //        totalInteger += 1;
        //        totalFractional -= 100_000_000;
        //    }

        //    string result = $"{totalInteger}.{totalFractional}";

        //    Assert.Equal(expectedAmount, result);

        //}

        //private AmountModel SplitAndFormatAmount(string amount)
        //{
        //    var delimiter = _decimals.GetDelimiterFromDecimal(amount);
        //    var splitAmount = amount.Split(".");

        //    ulong.TryParse(splitAmount[0], out ulong integer);
        //    ulong.TryParse(splitAmount[1], out ulong fractional);

        //    for (int i = 0; i < fractional.ToString().Length; i++)
        //    {
        //        splitAmount[1] += $"0{splitAmount[1]}";
        //    }

        //    ulong.TryParse(splitAmount[1], out ulong test);

        //    return new AmountModel
        //    {
        //        delimiter = delimiter,
        //        integer = integer,
        //        fractional = test
        //    };
        //}

        //public struct AmountModel
        //{
        //    public ulong integer;

        //    public ulong fractional;

        //    public ulong delimiter;
        //}
    }
}
