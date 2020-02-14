using Stratis.SmartContracts;

[Deploy]
public class Orders : SmartContract
{
    /// <summary>
    /// Constructor for orders contract that logs general info specific to an order.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    public Orders(ISmartContractState smartContractState)
        : base(smartContractState) { }

    public void AddOrder(Address order, Address token)
    {
        Assert(order != Address.Zero && token != Address.Zero);

        Log(new OrderLog
        {
            Owner = Message.Sender,
            Token = token,
            Order = order,
            Block = Block.Number
        });
    }

    public void UpdateOrder(Address order, Address token, string orderTxHash)
    {
        Assert(order != Address.Zero
            && token != Address.Zero
            && !string.IsNullOrEmpty(orderTxHash));

        Log(new UpdatedOrderLog
        {
            Token = token,
            Order = order,
            OrderTxHash = orderTxHash,
            Block = Block.Number
        });
    }

    public struct UpdatedOrderLog
    {
        [Index]
        public Address Token;

        [Index]
        public Address Order;

        public string OrderTxHash;

        public ulong Block;
    }


    public struct OrderLog
    {
        [Index]
        public Address Owner;

        [Index]
        public Address Token;

        public Address Order;

        public ulong Block;
    }
}
