using Stratis.SmartContracts;

[Deploy]
public class Offers : SmartContract
{
    public Offers(ISmartContractState smartContractState) : base(smartContractState)
    { }

    public void AddOffer(string tradeAction, ulong tokenAmount, ulong tokenPrice,
        Address tokenAddress, Address contractAddress)
    {
        Log(new Offer
        {
            Owner = Message.Sender,
            TradeAction = tradeAction,
            TokenAmount = tokenAmount,
            TokenPrice = tokenPrice,
            TokenAddress = tokenAddress,
            ContractAddress = contractAddress
        });
    }

    public struct Offer
    {
        [Index]
        public Address Owner;
        [Index]
        public Address TokenAddress;
        public Address ContractAddress;
        public ulong TokenAmount;
        public ulong TokenPrice;
        public string TradeAction;
    }
}
