using Stratis.SmartContracts;

[Deploy]
public class BuyOffer : SmartContract
{
    public BuyOffer(
        ISmartContractState smartContractState, 
        Address tokenAddress, 
        ulong tokenAmount,
        ulong tokenPrice) : base (smartContractState)
    {
        Assert(tokenAmount > 0, "Amount must be greater than 0");
        Assert(tokenPrice > 0, "Price must be greater than 0");
        Assert(Message.Value >= tokenAmount * tokenPrice, "Balance is not enough to cover cost");

        TokenAddress = tokenAddress;
        Buyer = Message.Sender;
        TokenAmount = tokenAmount;
        TokenPrice = tokenPrice;
        IsActive = true;
    }

    public Address TokenAddress
    {
        get => PersistentState.GetAddress(nameof(TokenAddress));
        private set => PersistentState.SetAddress(nameof(TokenAddress), value);
    }

    public Address Buyer
    {
        get => PersistentState.GetAddress(nameof(Buyer));
        private set => PersistentState.SetAddress(nameof(Buyer), value);
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

    public Transaction Sell(ulong amountToPurchase)
    {
        Assert(IsActive);
        Assert(Message.Sender != Buyer);
        Assert(TokenAmount >= amountToPurchase);

        var totalPrice = TokenPrice * amountToPurchase;
        Assert(Balance >= totalPrice, "Not enough funds to cover purchase.");

        var transferResult = Call(TokenAddress, 0, "TransferFrom", new object[] { Message.Sender, Buyer, amountToPurchase });

        Assert(transferResult.Success);

        Transfer(Message.Sender, totalPrice);

        var updatedAmount = TokenAmount - amountToPurchase;
        if (updatedAmount > 0)
        {
            TokenAmount = updatedAmount;
            // leftover amount return to Buyer?
        }
        else
        {
            CloseTradeExecute();
        }

        var txResult = new Transaction
        {
            Buyer = Buyer,
            Seller = Message.Sender,
            TokenAmount = amountToPurchase,
            TokenPrice = TokenPrice,
            TotalPrice = totalPrice
        };

        Log(txResult);

        return txResult;
    }

    public void CloseTrade()
    {
        Assert(Message.Sender == Buyer);

        CloseTradeExecute();
    }

    private void CloseTradeExecute()
    {
        if (Balance > 0)
        {
            Transfer(Buyer, Balance);
        }

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
            ContractBalance = Balance,
            TradeType = nameof(BuyOffer)
        };
    }

    public struct Transaction
    {
        [Index]
        public Address Seller;
        public Address Buyer;
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
        public ulong ContractBalance;
        public string TradeType;
    }
}
