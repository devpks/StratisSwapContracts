using Stratis.SmartContracts;

[Deploy]
public class SellOffer : SmartContract
{
    public SellOffer(
        ISmartContractState smartContractState, 
        Address token, 
        ulong amount, 
        ulong price) : base (smartContractState)
    {
        Assert(amount > 0, "Amount must be greater than 0");
        Assert(price > 0, "Price must be greater than 0");

        Token = token;
        Owner = Message.Sender;
        Amount = amount;
        Price = price;
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

    public Transaction Buy()
    {
        Assert(IsActive);
        Assert(Message.Sender != Owner);
        Assert(Message.Value >= Price, "Not enough funds to cover purchase.");

        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Owner, Message.Sender, Amount });

        Assert(transferResult.Success);

        Transfer(Owner, Price);

        var balance = Message.Value - Price;
        if (balance > 0)
        {
            Transfer(Message.Sender, balance);
        }

        var txResult = new Transaction
        {
            Buyer = Message.Sender,
            Seller = Owner,
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
