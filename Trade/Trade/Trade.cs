using Stratis.SmartContracts;

[Deploy]
public class Trade : SmartContract
{
    private const string buy = "buy";
    private const string sell = "sell";
    public Trade(
        ISmartContractState smartContractState, 
        string tradeAction, 
        Address token, 
        ulong amount, 
        ulong price) : base (smartContractState)
    {
        var action = tradeAction.ToLower();
        Assert(action == buy || action == sell, "Action must me buy or sell");
        Assert(amount > 0, "Amount must be greater than 0");
        Assert(price > 0, "Price must be greater than 0");

        if (action == buy) {
            // Verify the value sent covers the amount to buy and purchase price
            Assert(Message.Value > amount * price);
        }
        else if (action == sell)
        {
            // do stuff
        }

        Token = token;
        Owner = Message.Sender;
        OwnerAction = action;
        Amount = amount;
        Price = price;
    }

    public Tx Buy(ulong amount)
    {
        Assert(Message.Sender != Owner);
        Assert(OwnerAction != "buy", "Invalid operation, sell offers only.");

        // Verify
        Assert(amount <= Amount, "Cannot purchase more than what is beings sold");
        Assert(Message.Value >= amount * Price, "Not enough funds to cover purchase.");

        // Transfer SRC
        var transferResult = Call(Token, 0, "TransferFrom", new object[] {Owner, Message.Sender, amount});
        // Verify Successful Transfer
        Assert(transferResult.Success);

        // Transfer CRS amount * Price to seller
        Transfer(Owner, amount * Price);
        // Any remainder transfer back to buyer
        if (Balance > 0)
        {
            Transfer(Message.Sender, Balance);
        }

        // Update Buyer
        // Update Seller
        // Build and return txResult
        var txResult = new Tx() {TransactionId = "asdf", Buyer = GetBuyer(Message.Sender), Seller = GetSeller(Owner) };
        
        return txResult;
    }

    public Tx Sell(ulong amount)
    {
        Assert(Message.Sender != Owner);
        Assert(OwnerAction != "sell", "Invalid operation, buy offers only.");

        var txResult = new Tx() {TransactionId = "asdf", Buyer = GetBuyer(Message.Sender), Seller = GetSeller(Owner) };

        return txResult;
    }

    public Address Token {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public string OwnerAction {
        get => PersistentState.GetString(nameof(OwnerAction));
        private set => PersistentState.SetString(nameof(OwnerAction), value);
    }

    public ulong Price {
        get => PersistentState.GetUInt64(nameof(Price));
        private set => PersistentState.SetUInt64(nameof(Price), value);
    }

    public ulong Amount {
        get => PersistentState.GetUInt64(nameof(Amount));
        private set => PersistentState.SetUInt64(nameof(Amount), value);
    }

    public Address Owner {
        get => PersistentState.GetAddress(nameof(Owner));
        private set => PersistentState.SetAddress(nameof(Owner), value);
    }

    public Tx[] GetTransactions()
    {
        return PersistentState.GetArray<Tx>("Transactions");
    }

    public void SetNewTransaction(Tx test) {
        var transactions = GetTransactions();
        var currentLength = transactions.Length;
        var newLength = transactions.Length + 1;
        var newTransactions = new Tx[newLength];

        for (var i = 0; i < currentLength; i++)
        {
            newTransactions[i] = transactions[i];
        }

        newTransactions[newLength] = test;

        PersistentState.SetArray("Transactions", newTransactions);
    }

    public Buyer GetBuyer(Address buyer) {
        return PersistentState.GetStruct<Buyer>($"Buyer:{buyer}");
    }

    public Seller GetSeller(Address seller) {
        return PersistentState.GetStruct<Seller>($"Seller:{seller}");
    }

    public struct Buyer {
        public Address Address;
        public ulong Amount;
        public ulong AmountBought;
    }

    public struct Seller {
        public Address Address;
        public ulong Amount;
        public ulong AmountSold;
    }

    public struct Tx {
        [Index]
        public string TransactionId;
        public Buyer Buyer;
        public Seller Seller;
    }
}
