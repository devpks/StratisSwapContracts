using Stratis.SmartContracts;

[Deploy]
public class BuyOrder : SmartContract
{
    /// <summary>
    /// Constructor for a buy order setting the token, price, and amount to buy.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="address">The address of the src token being bought.</param>
    /// <param name="price">The price for each src token.</param>
    /// <param name="amount">The amount of src token to buy.</param>
    public BuyOrder(
        ISmartContractState smartContractState, 
        Address address,
        ulong price,
        ulong amount) : base (smartContractState)
    {
        Assert(price > 0, "Price must be greater than 0");
        Assert(amount > 0, "Amount must be greater than 0");
        Assert(Message.Value >= amount * price, "Balance is not enough to cover cost");

        Token = address;
        Price = price;
        Amount = amount;
        Buyer = Message.Sender;
        IsActive = true;
    }

    public Address Token
    {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public ulong Price
    {
        get => PersistentState.GetUInt64(nameof(Price));
        private set => PersistentState.SetUInt64(nameof(Price), value);
    }

    public ulong Amount
    {
        get => PersistentState.GetUInt64(nameof(Amount));
        private set => PersistentState.SetUInt64(nameof(Amount), value);
    }

    public Address Buyer
    {
        get => PersistentState.GetAddress(nameof(Buyer));
        private set => PersistentState.SetAddress(nameof(Buyer), value);
    }

    public bool IsActive
    {
        get => PersistentState.GetBool(nameof(IsActive));
        private set => PersistentState.SetBool(nameof(IsActive), value);
    }

    public Transaction Sell(ulong amountToSell)
    {
        Assert(IsActive, "Contract is not active.");
        Assert(Message.Sender != Buyer, "Sender cannot be owner.");

        amountToSell = Amount >= amountToSell ? amountToSell : Amount;

        var cost = Price * amountToSell;
        Assert(Balance >= cost, "Not enough funds to cover cost.");

        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Message.Sender, Buyer, amountToSell });
        Assert((bool)transferResult.ReturnValue == true, "Transfer failure.");

        Transfer(Message.Sender, cost);

        Amount -= amountToSell;

        if (Amount == 0)
        {
            CloseOrderExecute();
        }

        var txResult = new Transaction
        {
            Seller = Message.Sender,
            Price = Price,
            Amount = amountToSell,
            Block = Block.Number
        };

        Log(txResult);

        return txResult;
    }

    public void CloseOrder()
    {
        Assert(Message.Sender == Buyer);

        CloseOrderExecute();
    }

    private void CloseOrderExecute()
    {
        if (Balance > 0)
        {
            Transfer(Buyer, Balance);
        }

        IsActive = false;
    }

    public OrderDetails GetOrderDetails()
    {
        return new OrderDetails
        {
            Buyer = Buyer,
            Token = Token,
            Price = Price,
            Amount = Amount,
            IsActive = IsActive,
            OrderType = nameof(BuyOrder),
            ContractBalance = Balance
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Seller;

        public ulong Price;

        public ulong Amount;

        public ulong Block;
    }

    public struct OrderDetails
    {
        public Address Buyer;

        public Address Token;

        public ulong Price;

        public ulong Amount;

        public string OrderType;

        public bool IsActive;

        public ulong ContractBalance;
    }
}
