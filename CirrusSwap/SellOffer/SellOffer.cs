using Stratis.SmartContracts;

[Deploy]
public class SellOffer : SmartContract
{
    /// <summary>
    /// Simple sell offer contract providing functionality to accept crs tokens from
    /// buyers on a successful src transfer.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="tokenAddress">The address of the src token being sold.</param>
    /// <param name="tokenPrice">The price for each src token.</param>
    /// <param name="tokenAmount">The amount of the src token to sell.</param>
    public SellOffer(
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

    /// <summary>
    /// Method for buyers to call to transfer crs tokens for src.
    /// </summary>
    /// <param name="amountToPurchase">The amount of src tokens willing to buy.</param>
    /// <returns><see cref="Transaction"/></returns>
    public Transaction Buy(ulong amountToPurchase)
    {
        Assert(IsActive);
        Assert(Message.Sender != Seller);
        Assert(TokenAmount >= amountToPurchase);

        var totalPrice = TokenPrice * amountToPurchase;
        Assert(Message.Value >= totalPrice, "Not enough funds to cover purchase.");

        var transferResult = Call(TokenAddress, 0, "TransferFrom", new object[] { Seller, Message.Sender, TokenAmount });

        Assert(transferResult.Success);

        Transfer(Seller, totalPrice);

        var balance = Message.Value - totalPrice;
        if (balance > 0)
        {
            Transfer(Message.Sender, balance);
        }

        var updatedAmount = TokenAmount - amountToPurchase;
        if (updatedAmount > 0)
        {
            TokenAmount = updatedAmount;
        }
        else
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

    public void CloseTrade()
    {
        Assert(Message.Sender == Seller);

        IsActive = false;
    }

    public TradeDetails GetTradeDetails()
    {
        return new TradeDetails
        {
            SellerAddress = Seller,
            TokenAddress = TokenAddress,
            TokenPrice = TokenPrice,
            TokenAmount = TokenAmount,
            TradeType = nameof(SellOffer),
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

    public struct TradeDetails
    {
        public Address SellerAddress;
        public Address TokenAddress;
        public ulong TokenPrice;
        public ulong TokenAmount;
        public string TradeType;
        public bool IsActive;
    }
}
