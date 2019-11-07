using Stratis.SmartContracts;

[Deploy]
public class Playground : SmartContract
{
    public Playground(ISmartContractState smartContractState) : base (smartContractState)
    {
    }

    public override void Receive()
    {
        this.SenderBalance = Message.Value;
    }

    public ulong SenderBalance
    {
        get => PersistentState.GetUInt64($"Balance:{Message.Sender}");
        private set => PersistentState.SetUInt64($"Balance:{Message.Sender}", value);
    }

    public ulong GetSenderBalance() 
    {
        return this.SenderBalance;
    }
    public ulong ContractBalance()
    {
        return this.Balance;
    }

    public Address GetOrderBook(Address token)
    {
        return PersistentState.GetAddress($"OrderBook:{token}");
    }

    private void SetOrderBook(Address token, Address orderBook)
    {
        PersistentState.SetAddress($"OrderBook:{token}", orderBook);
    }

    // public bool AddOrderBook(Address token, Address orderBook)
    // {
    //     var existingOrderBook = GetOrderBook(token);

    //     // What is the default return type of PersistentState.GetAddress()?
    //     if (existingOrderBook != null || existingOrderBook != Address.Zero)
    //     {
    //         return false;
    //     }

    //     var created = Create<OrderBook>(0, new object[] { });

    //     Assert(created.Success);
        
    //     SetOrderBook(token, created.NewContractAddress);

    //     return true;
    // }
}