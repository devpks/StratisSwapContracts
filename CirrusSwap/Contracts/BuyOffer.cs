using Stratis.SmartContracts;

[Deploy]
public class BuyOffer : SmartContract
{
    public BuyOffer(
        ISmartContractState smartContractState, 
        Address token, 
        ulong amount) : base (smartContractState)
    {
        Assert(amount > 0, "Amount must be greater than 0");

        Token = token;
        Owner = Message.Sender;
        Price = Message.Value;
        Amount = amount;
        IsActive = true;
    }

    public Address Token
    {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public Address Owner
    {
        get => PersistentState.GetAddress(nameof(Owner));
        private set => PersistentState.SetAddress(nameof(Owner), value);
    }

    public ulong Price
    {
        get => PersistentState.GetUInt64(nameof(Price));
        private set => PersistentState.SetUInt64(nameof(Price), value);
    }

    public ulong Amount
    {
        get => PersistentState.GetUInt64(nameof(Amount));
        private set => PersistentState.SetUInt64(nameof(Amount), value);
    }

    public bool IsActive 
    {
        get => PersistentState.GetBool(nameof(IsActive));
        private set => PersistentState.SetBool(nameof(IsActive), value);
    }

    public Transaction Sell()
    {
        Assert(IsActive);
        Assert(Message.Sender != Owner);

        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Message.Sender, Owner, Amount });

        Assert(transferResult.Success);

        Transfer(Message.Sender, Price);

        var txResult = new Transaction
        {
            Buyer = Owner,
            Seller = Message.Sender,
            SrcAmount = Amount,
            CrsAmount = Price
        };

        Log(txResult);

        IsActive = false;

        return txResult;
    }

    public void CancelTrade()
    {
        Assert(IsActive);
        Assert(Message.Sender == Owner);

        Transfer(Owner, Balance);

        IsActive = false;
    }

    public struct Transaction
    {
        public Address Buyer;
        public Address Seller;
        public ulong CrsAmount;
        public ulong SrcAmount;
    }
}
