using Stratis.SmartContracts;

[Deploy]
public class OrderBooks : SmartContract
{
    public OrderBooks(ISmartContractState smartContractState) : base (smartContractState)
    {
    }
}