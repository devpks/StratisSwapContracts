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

    public void AddOrder(Address token, Address order)
    {
        Assert(token != Address.Zero && order != Address.Zero);

        Log(new OrderLog
        {
            Owner = Message.Sender,
            Token = token,
            Order = order,
            Block = Block.Number
        });
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
