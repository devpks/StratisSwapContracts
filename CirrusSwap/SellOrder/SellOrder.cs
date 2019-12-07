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
    /// <param name="amount">The amount of the src token to sell.</param>
    public SellOrder(ISmartContractState smartContractState, Address token, ulong price, ulong amount)
        : base (smartContractState)
    {
        Assert(price > 0, "Price must be greater than 0");
        Assert(amount > 0, "Amount must be greater than 0");

        Token = token;
        Price = price;
        Amount = amount;
        Seller = Message.Sender;
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

    public Transaction Buy(ulong amountToBuy)
    {
        Assert(IsActive, "Contract is not active.");
        Assert(Message.Sender != Seller, "Sender is owner.");

        amountToBuy = Amount >= amountToBuy ? amountToBuy : Amount;

        var cost = Price * amountToBuy;
        Assert(Message.Value >= cost, "Not enough funds to cover purchase.");

        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Seller, Message.Sender, amountToBuy });
        Assert((bool)transferResult.ReturnValue == true, "SRC transfer failure.");

        Transfer(Seller, cost);

        var balance = Message.Value - cost;
        if (balance > 0)
        {
            Transfer(Message.Sender, balance);
        }

        Amount -= amountToBuy;

        if (Amount == 0)
        {
            IsActive = false;
        }

        var txResult = new Transaction
        {
            Buyer = Message.Sender,
            Price = Price,
            Amount = amountToBuy,
            TotalPrice = cost,
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
            OrderType = nameof(SellOrder),
            IsActive = IsActive,
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Buyer;

        public ulong Price;

        public ulong Amount;

        public ulong TotalPrice;

        public ulong Block;
    }

    public struct OrderDetails
    {
        public Address Seller;

        public Address Token;

        public ulong Price;

        public ulong Amount;

        public string OrderType;

        public bool IsActive;
    }
}
