using Stratis.SmartContracts;

[Deploy]
public class SellOrder : SmartContract
{
    /// <summary>
    /// Simple sell order contract providing functionality to accept crs tokens from
    /// buyers on a successful src transfer.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="tokenAddress">The address of the src token being sold.</param>
    /// <param name="tokenPrice">The price for each src token.</param>
    /// <param name="tokenAmount">The amount of the src token to sell.</param>
    public SellOrder(
        ISmartContractState smartContractState, 
        Address tokenAddress,
        ulong tokenPrice,
        ulong tokenAmount) : base (smartContractState)
    {
        Assert(tokenPrice > 0, "Price must be greater than 0");
        Assert(tokenAmount > 0, "Amount must be greater than 0");

        TokenAddress = tokenAddress;
        TokenPrice = tokenPrice;
        TokenAmount = tokenAmount;
        Seller = Message.Sender;
        IsActive = true;
    }

    public Address TokenAddress
    {
        get => PersistentState.GetAddress(nameof(TokenAddress));
        private set => PersistentState.SetAddress(nameof(TokenAddress), value);
    }

    public ulong TokenPrice
    {
        get => PersistentState.GetUInt64(nameof(TokenPrice));
        private set => PersistentState.SetUInt64(nameof(TokenPrice), value);
    }

    public ulong TokenAmount
    {
        get => PersistentState.GetUInt64(nameof(TokenAmount));
        private set => PersistentState.SetUInt64(nameof(TokenAmount), value);
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

    public Transaction Buy(ulong amountToPurchase)
    {
        Assert(IsActive, "Contract is not active.");
        Assert(Message.Sender != Seller, "Sender is owner.");

        amountToPurchase = TokenAmount >= amountToPurchase ? amountToPurchase : TokenAmount;

        var totalPrice = TokenPrice * amountToPurchase;
        Assert(Message.Value >= totalPrice, "Not enough funds to cover purchase.");

        var transferResult = Call(TokenAddress, 0, "TransferFrom", new object[] { Seller, Message.Sender, amountToPurchase });
        Assert((bool)transferResult.ReturnValue == true, "SRC transfer failure.");

        Transfer(Seller, totalPrice);

        var balance = Message.Value - totalPrice;
        if (balance > 0)
        {
            Transfer(Message.Sender, balance);
        }

        TokenAmount -= amountToPurchase;

        if (TokenAmount == 0)
        {
            IsActive = false;
        }

        var txResult = new Transaction
        {
            Buyer = Message.Sender,
            TokenPrice = TokenPrice,
            TokenAmount = amountToPurchase,
            TotalPrice = totalPrice,
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
            SellerAddress = Seller,
            TokenAddress = TokenAddress,
            TokenPrice = TokenPrice,
            TokenAmount = TokenAmount,
            OrderType = nameof(SellOrder),
            IsActive = IsActive,
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Buyer;

        public ulong TokenPrice;

        public ulong TokenAmount;

        public ulong TotalPrice;

        public ulong Block;
    }

    public struct OrderDetails
    {
        public Address SellerAddress;

        public Address TokenAddress;

        public ulong TokenPrice;

        public ulong TokenAmount;

        public string OrderType;

        public bool IsActive;
    }
}
