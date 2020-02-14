using System;
using Stratis.SmartContracts;

[Deploy]
public class SellOrder : SmartContract
{
    /// <summary>
    /// Constructor for a sell order setting the token, price, and amount to sell.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="token">The address of the src token being sold.</param>
    /// <param name="price">The price for each src token.</param>
    /// <param name="amount">The amount of src token to sell.</param>
    public SellOrder(ISmartContractState smartContractState, Address token, string price, string amount)
        : base (smartContractState)
    {
        // New validation methods on the string
        //Assert(price > 0, "Price must be greater than 0");
        //Assert(amount > 0, "Amount must be greater than 0");

        Token = token;
        Price = price;
        Amount = amount;
        Seller = Message.Sender;
        IsActive = true;
    }

    private const char dot = '.';
    private const int maxDecimalLength = 8;
    private const ulong OneFullCoinInStratoshis = 100_000_000;

    public Address Token
    {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public string Price
    {
        get => PersistentState.GetString(nameof(Price));
        private set => PersistentState.SetString(nameof(Price), value);
    }

    public ulong PriceInStratoshis => ConvertToStratoshis(Price);

    public string Amount
    {
        get => PersistentState.GetString(nameof(Amount));
        private set => PersistentState.SetString(nameof(Amount), value);
    }

    public ulong AmountInStratoshis => ConvertToStratoshis(Amount);

    public Address Seller
    {
        get => PersistentState.GetAddress(nameof(Seller));
        private set => PersistentState.SetAddress(nameof(Seller), value);
    }

    public bool IsActive
    {
        get => PersistentState.GetBool(nameof(IsActive));
        private set => PersistentState.SetBool(nameof(IsActive), value);
    }

    public Transaction Buy(string amountToBuy)
    {
        // Todo: Will need extra validations on the string.
        Assert(IsActive, "Contract is not active.");
        Assert(Message.Sender != Seller, "Sender cannot be owner.");

        // Should this be a comparable method in the package?
        // Or just convert both to stratoshi to compare?
        ulong amountToBuyInStrats = ConvertToStratoshis(amountToBuy);
        amountToBuy = AmountInStratoshis >= amountToBuyInStrats ? amountToBuy : Amount;

        // Will be using the multiplication method of decimals
        ulong cost = MultiplyDecimalsReturnStratoshis(amountToBuy, Price);
        Assert(Message.Value >= cost, "Not enough funds to cover cost.");

        // AmountToBuy here will have to be ulong stratoshi value
        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Seller, Message.Sender, ConvertToStratoshis(amountToBuy) });
        Assert((bool)transferResult.ReturnValue == true, "Transfer failure.");

        Transfer(Seller, cost);

        var balance = Message.Value - cost;
        if (balance > 0)
        {
            Transfer(Message.Sender, balance);
        }

        // Using new subtract method
        Amount = SubtractDecimals(Amount, amountToBuy);

        // Will be checking the amount in stratoshi value
        if (AmountInStratoshis == 0)
        {
            IsActive = false;
        }

        var txResult = new Transaction
        {
            Buyer = Message.Sender,
            Price = Price,
            Amount = amountToBuy,
            Block = Block.Number
        };

        Log(txResult);

        return txResult;
    }

    public void CloseOrder()
    {
        Assert(Message.Sender == Seller);

        IsActive = false;
    }

    public OrderDetails GetOrderDetails()
    {
        return new OrderDetails
        {
            Seller = Seller,
            Token = Token,
            Price = Price,
            Amount = Amount,
            IsActive = IsActive,
            OrderType = nameof(SellOrder)
        };
    }

    public ulong CalculateTotals(string amount, ulong price)
    {
        ulong delimiter = 10_000;

        var numbers = amount.Split(".");

        ulong.TryParse(numbers[0], out ulong bigNumber);
        ulong.TryParse(numbers[1], out ulong smallNumber);

        ulong bigNumberTotal = bigNumber * delimiter * price;
        ulong smallNumberTotal = smallNumber * price;

        return bigNumberTotal + smallNumberTotal;
    }

    public struct Transaction
    {
        [Index]
        public Address Buyer;

        public string Price;

        public string Amount;

        public ulong Block;
    }

    public struct OrderDetails
    {
        public Address Seller;

        public Address Token;

        public string Price;

        public string Amount;

        public string OrderType;

        public bool IsActive;
    }

    /// <summary>
    /// Convert to a decimal string based on stratoshi amount provided.
    /// </summary>
    /// <param name="amount">Amount in stratoshis to convert to a decimal string.</param>
    /// <returns>The a decimal string equal to the amount of stratoshis provided.</returns>
    public string ConvertToDecimal(ulong amount)
    {
        string amountString = amount.ToString();
        // Set a reference to the amountStringLength so it does not get overwritten
        // (e.g. Without this reference setting amountString to a new value adjusts it's length
        // if amountString.Length were used.)
        int amountStringLength = amountString.Length;

        // Value is over 1 full number, just insert a decimal point where necessary.
        // Todo: Evaluate here if it would be better to compare amount to OneFullCoinInStratoshis
        if (amountStringLength > 8)
        {
            return amountString.Insert(amountStringLength - maxDecimalLength, ".");
        }

        // Loop through and prefix the amount string with zeros.
        // (e.g. If amount is 10_000_000 (7 length) would be 8 - 7, would loop once.
        // Setting the amountString value to "010000000".
        for (int i = 0; i < maxDecimalLength - amountStringLength; i++)
        {
            // Prefix the string with a zero every loop through
            amountString = $"0{amountString}";
        }

        // Add the leading zero and decimal point (e.g. 0.010000000)
        return $"0.{amountString}";
    }

    /// <summary>
    /// Convert a decimal string to the equivalent amount in stratoshis
    /// </summary>
    /// <param name="amount">Amount in string decimal format to convert to stratoshis.</param>
    /// <returns>Stratoshi amount equal to the provided decimal string.</returns>
    public ulong ConvertToStratoshis(string amount)
    {
        // Get the delimiter value based on the provided string decimal amount.
        ulong delimiter = GetDelimiterFromDecimal(amount);

        // Split amount on the ".".
        // Todo: Add validations and checks that there is a "." in the first place
        string[] splitAmount = amount.Split(dot);

        // Parse the amount integer and fractional to a ulong.
        ulong.TryParse(splitAmount[0], out ulong integer);
        ulong.TryParse(splitAmount[1], out ulong fractional);

        // Multiply the full integer amount by 100_000_000 stratoshis
        // (e.g. 2 * 100_000_000 = 200_000_000)
        ulong integerAmount = integer * OneFullCoinInStratoshis;

        // Multiple the fractional by the delimiter providing the accurate stratoshi amount
        // (e.g. .12304 = 12_304 x 100 = 12_304_000 stratoshis
        ulong fractionalAmount = fractional * delimiter;

        // Add the integer and fractional stratoshi amounts to get result
        return integerAmount + fractionalAmount;
    }

    /// <summary>
    /// Subtract a decimal string from another.
    /// </summary>
    /// <param name="amountOne">The first decimal string amount to be subtracted against.</param>
    /// <param name="amountTwo">The second decimal string to be subtracted from amount one.</param>
    /// <returns>Decimal string result of amountOne - amountTwo</returns>
    public string SubtractDecimals(string amountOne, string amountTwo)
    {
        // Convert each amount to it's value in stratoshis
        ulong amountOneStratoshis = ConvertToStratoshis(amountOne);
        ulong amountTwoStratoshis = ConvertToStratoshis(amountTwo);

        // Subtract amount two from amount one
        ulong finalAmountStratoshis = amountOneStratoshis - amountTwoStratoshis;

        // Return result converted back to a decimal string
        return ConvertToDecimal(finalAmountStratoshis);
    }

    /// <summary>
    /// Return a the delimiter based on the string decimal passed in.
    /// </summary>
    /// <param name="amount">String decimal amount value to get delimiter for.</param>
    /// <returns>Delimiter based on a string value passed in. (e.g. Passing in "1.123"
    /// will return a delimiter of 100_000. Where 123 * 100_000 would equal 12_300_000.</returns>
    public ulong GetDelimiterFromDecimal(string amount)
    {
        // The beginning of the delmiter string we'll be adjusting
        string delimiterString = "1";

        // Todo: Add validations and checks that there is a "." in the first place
        string[] splitAmount = amount.Split(dot);
        int fractionalLength = splitAmount[1].Length;

        // Loop through and append the necessary amount of 0's to the delimiter string.
        // Begin with the length of the fractional. (e.g. If fractional = 12345, length = 5
        // as long as 5 < 8 append necessary 0's to the delimiter string, results in = 1_000.
        // 12_345 * 1_000 = 12_345_000 the correct value in stratoshis)
        for (int i = fractionalLength; i < 8; i++)
        {
            delimiterString = $"{delimiterString}0";
        }

        // Parse the delimter string into a ulong
        ulong.TryParse(delimiterString, out ulong formattedDelimiter);

        return formattedDelimiter;
    }

    // Todo: Refactor and cleanup this entire method.
    // Todo: Limit mulitplication to 4 decimal places max.
    // Validations prior to actually doing math.
    public ulong MultiplyDecimalsReturnStratoshis(string amountOne, string amountTwo)
    {
        // Prep amountOne
        var amountOneIndex = amountOne.IndexOf(dot);
        amountOne = amountOne.Remove(amountOneIndex, 1);

        var amountOneDecimals = amountOne.Length - amountOneIndex;

        ulong.TryParse(amountOne, out ulong amountOneNumber);

        // Prep amountTwo
        var amountTwoIndex = amountTwo.IndexOf(dot);
        amountTwo = amountTwo.Remove(amountTwoIndex, 1);

        var amountTwoDecimals = amountTwo.Length - amountTwoIndex;

        ulong.TryParse(amountTwo, out ulong amountTwoNumber);

        // Quick Maffs
        var resultString = (amountOneNumber * amountTwoNumber).ToString();
        var resultStringLength = resultString.Length;
        var decimalsToCarry = amountOneDecimals + amountTwoDecimals;
        var startIndex = resultStringLength - decimalsToCarry;

        if (startIndex < 0)
        {
            for (int i = 0; i < decimalsToCarry - resultStringLength; i++)
            {
                // Prefix the string with a zero every loop through
                resultString = $"0{resultString}";
            }
        }

        startIndex = resultString.Length - decimalsToCarry;

        // Insert Decimal Back into string
        resultString = resultString.Insert(startIndex, dot.ToString());

        // Convert and return
        return ConvertToStratoshis(resultString);
    }
}
