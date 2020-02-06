namespace SmartMath
{
    public interface IDecimals
    {
        ulong GetDelimiterFromDecimal(string amount);

        string ConvertToDecimalFromSatoshis(ulong amount);

        ulong ConvertToSatoshisFromDecimal(string amount);

        string AddDecimals(string amountOne, string amountTwo);

        string SubtractDecimals(string amountOne, string amountTwo);

        //string MultiplyDecimals(string amountOne, string amountTwo);

        //string DivideDecimals(string amountOne, string amountTwo);
    }
}
