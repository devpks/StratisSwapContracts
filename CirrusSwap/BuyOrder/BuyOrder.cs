using Stratis.SmartContracts;

[Deploy]
public class BuyOrder : SmartContract
{
    /// <summary>
    /// Simple buy order contract providing functionality to release crs tokens to
    /// sellers on a successful SRC transfer. 
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="tokenAddress">The address of the src token being bought.</param>
    /// <param name="tokenPrice">The price for each src token.</param>
    /// <param name="tokenAmount">The amount of the src token to buy.</param>
    public BuyOrder(
        ISmartContractState smartContractState, 
        Address tokenAddress,
        ulong tokenPrice,
        ulong tokenAmount) : base (smartContractState)
    {
        Assert(tokenPrice > 0, "Price must be greater than 0");
        Assert(tokenAmount > 0, "Amount must be greater than 0");
        Assert(Message.Value >= tokenAmount * tokenPrice, "Balance is not enough to cover cost");

        TokenAddress = tokenAddress;
        TokenPrice = tokenPrice;
        TokenAmount = tokenAmount;
        Buyer = Message.Sender;
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

    /// <summary>
    /// Method for sellers to call to transfer src tokens for crs.
    /// </summary>
    /// <param name="amountToSell">The amount of src tokens willing to sell.</param>
    /// <returns><see cref="Transaction"/></returns>
    public Transaction Sell(ulong amountToSell)
    {
        Assert(IsActive, "Contract is not active.");
        Assert(Message.Sender != Buyer, "Sender is owner.");

        amountToSell = TokenAmount >= amountToSell ? amountToSell : TokenAmount;

        var totalPrice = TokenPrice * amountToSell;
        Assert(Balance >= totalPrice, "Not enough funds to cover purchase.");

        var transferResult = Call(TokenAddress, 0, "TransferFrom", new object[] { Message.Sender, Buyer, amountToSell });
        Assert((bool)transferResult.ReturnValue == true, "SRC transfer failure.");

        Transfer(Message.Sender, totalPrice);

        var updatedAmount = TokenAmount - amountToSell;
        if (updatedAmount > 0)
        {
            TokenAmount = updatedAmount;
        }
        else
        {
            CloseOrderExecute();
        }

        var txResult = new Transaction
        {
            Seller = Message.Sender,
            TokenPrice = TokenPrice,
            TokenAmount = amountToSell,
            TotalPrice = totalPrice,
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
            TokenAddress = TokenAddress,
            TokenPrice = TokenPrice,
            TokenAmount = TokenAmount,
            ContractBalance = Balance,
            OrderType = nameof(BuyOrder),
            IsActive = IsActive,
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Seller;

        public ulong TokenPrice;

        public ulong TokenAmount;

        public ulong TotalPrice;

        public ulong Block;
    }

    public struct OrderDetails
    {
        public Address TokenAddress;

        public ulong TokenPrice;

        public ulong TokenAmount;

        public ulong ContractBalance;

        public string OrderType;

        public bool IsActive;
    }
}
