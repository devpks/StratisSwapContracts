using Stratis.SmartContracts;

[Deploy]
public class Orders : SmartContract
{
    public Orders(ISmartContractState smartContractState) : base(smartContractState)
    { }

    public void AddOrder(string orderType, ulong tokenAmount, ulong tokenPrice,
        Address tokenAddress, Address contractAddress)
    {
        Assert(orderType == "Buy" || orderType == "Sell");
        Assert(tokenAmount > 0);
        Assert(tokenPrice > 0);
        Assert(tokenAddress != Address.Zero);
        Assert(contractAddress != Address.Zero);

        Log(new Order
        {
            Owner = Message.Sender,
            OrderType = orderType,
            TokenAmount = tokenAmount,
            TokenPrice = tokenPrice,
            TokenAddress = tokenAddress,
            ContractAddress = contractAddress,
            Block = Block.Number
        });
    }

    public struct Order
    {
        [Index]
        public Address Owner;

        [Index]
        public Address TokenAddress;

        public Address ContractAddress;

        public ulong TokenAmount;

        public ulong TokenPrice;

        public string OrderType;

        public ulong Block;
    }
}
