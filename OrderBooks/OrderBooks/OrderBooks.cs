using Stratis.SmartContracts;

[Deploy]
public class OrderBook : SmartContract
{
    public Trade(ISmartContractState smartContractState) : base (smartContractState)
    {
    }
}