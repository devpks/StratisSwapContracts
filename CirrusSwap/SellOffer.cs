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
    /// <param name="tokenAmount">The amount of the src token to sell.</param>
    /// <param name="tokenPrice">The price for each src token.</param>
    public SellOffer(
        ISmartContractState smartContractState, 
        Address tokenAddress, 
        ulong tokenAmount, 
        ulong tokenPrice) : base (smartContractState)
    {
        Assert(tokenAmount > 0, "Amount must be greater than 0");
        Assert(tokenPrice > 0, "Price must be greater than 0");

        TokenAddress = tokenAddress;
        Seller = Message.Sender;
        TokenAmount = tokenAmount;
        TokenPrice = tokenPrice;
        IsActive = true;
    }

    public Address TokenAddress
    {
        get => PersistentState.GetAddress(nameof(TokenAddress));
        private set => PersistentState.SetAddress(nameof(TokenAddress), value);
    }

    public Address Seller
    {
        get => PersistentState.GetAddress(nameof(Seller));
        private set => PersistentState.SetAddress(nameof(Seller), value);
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
            Seller = Seller,
            TokenAmount = amountToPurchase,
            TokenPrice = TokenPrice,
            TotalPrice = totalPrice
        };

        Log(txResult);

        return txResult;
    }

    /// <summary>
    /// Closes offer from further trades
    /// </summary>
    public void CloseTrade()
    {
        Assert(Message.Sender == Seller);

        IsActive = false;
    }

    public TradeDetails GetTradeDetails()
    {
        return new TradeDetails
        {
            IsActive = IsActive,
            TokenAddress = TokenAddress,
            TokenPrice = TokenPrice,
            TokenAmount = TokenAmount,
            SellerAddress = Seller,
            TradeType = nameof(SellOffer)
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Buyer;
        public Address Seller;
        public ulong TokenAmount;
        public ulong TokenPrice;
        public ulong TotalPrice;
    }

    public struct TradeDetails
    {
        public bool IsActive;
        public ulong TokenAmount;
        public ulong TokenPrice;
        public Address TokenAddress;
        public Address SellerAddress;
        public string TradeType;
    }
}
