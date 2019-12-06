using Stratis.SmartContracts;

[Deploy]
public class Orders : SmartContract
{
    public Orders(ISmartContractState smartContractState)
        : base(smartContractState) { }

    public void AddOrder(Address tokenAddress, Address orderAddress)
    {
        Assert(tokenAddress != Address.Zero);
        Assert(orderAddress != Address.Zero);

        Log(new Order
        {
            Owner = Message.Sender,
            TokenAddress = tokenAddress,
            OrderAddress = orderAddress,
            Block = Block.Number
        });
    }

    public struct Order
    {
        [Index]
        public Address Owner;

        [Index]
        public Address TokenAddress;

        public Address OrderAddress;

        public ulong Block;
    }
}
