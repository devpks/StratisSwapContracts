using Stratis.SmartContracts;

[Deploy]
public class Trade : SmartContract
{
    private const string buy = "buy";
    private const string sell = "sell";
    private const ulong oneFullToken = 100_000_000;
    public Trade(
        ISmartContractState smartContractState, 
        string contractType, 
        Address token, 
        ulong amount, 
        ulong price) : base (smartContractState)
    {
        contractType = contractType.ToLowerInvariant();
        Assert(contractType == buy || contractType == sell, "Action must me buy or sell");
        Assert(amount > 0, "Amount must be greater than 0");
        Assert(price > 0, "Price must be greater than 0");

        this.Token = token;
        this.Owner = Message.Sender;
        this.ContractType = contractType;
        this.Amount = amount; // amount of src always in full e.g. (50adt)
        this.Price = price; // price per src in stratoshies e.g. 10_000_000 (.1crs)

        if (contractType == buy) 
        {
            Assert(Message.Value >= this.TradeBalance);
        }

        this.IsActive = true;
    }

    public ulong TradeBalance => this.Amount / (oneFullToken / this.Price);

    public bool IsActive 
    {
        get => PersistentState.GetBool(nameof(IsActive));
        private set => PersistentState.SetBool(nameof(IsActive), value);
    }

    public Address Token {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public string ContractType {
        get => PersistentState.GetString(nameof(ContractType));
        private set => PersistentState.SetString(nameof(ContractType), value);
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

    public void Destroy()
    {
        Assert(Message.Sender == Owner);

        if (this.ContractType == buy)
        {
            // Need to keep an updated balance through txs
            this.Transfer(this.Owner, this.Amount * this.Price);
        }
        else if (this.ContractType == sell)
        {
            // Need to keep an updated balance through txs
            var result = Call(this.Token, 0, "TransferTo", new object[] { this.Owner, this.Amount * this.Price });

            Assert(result.Success);
        }

        this.IsActive = false;
    }

    public Transaction Buy(ulong amount) // amount of src to buy in full e.g. (50 adt)
    {
        // Verify
        Assert(Message.Sender != Owner);
        Assert(ContractType == sell, "Invalid operation, buy offers only.");
        Assert(amount <= Amount, "Cannot purchase more than what is beings sold.");

        var totalCost = amount * Price;
        Assert(Message.Value >= totalCost, "Not enough funds to cover purchase.");

        // Convert amount from full amount to stratoshies
        // e.g. from 50 to 5_000_000_000
        ulong stratoshiAmount = 50 * 10000000;

        // Transfer SRC
        // Contract address must have necessary allowance
        var transferResult = Call(Token, 0, "TransferFrom", new object[] { Owner, Message.Sender, stratoshiAmount });
        
        // Verify Successful Transfer
        if (!transferResult.Success)
        {
            this.Destroy();
        }

        // Transfer CRS totalCost to seller
        Transfer(Owner, totalCost);
        // Any remainder transfer back to buyer
        // Balance record of who deposited x amount needs to be made for withdraw abilities
        if (Balance > 0)
        {
            Transfer(Message.Sender, Balance);
        }

        this.Amount -= amount;

        // if (this.Amount == 0)
        // {
        //     this.Destroy();
        // }

        // Log Transaction Result
        var txResult = new Transaction { Buyer = Message.Sender, Seller = Owner, SrcAmount = stratoshiAmount, CrsAmount = totalCost };
        Log(txResult);
        
        return txResult;
    }

    // public Transaction Sell(ulong amount)
    // {
    //     Assert(Message.Sender != Owner);
    //     Assert(ContractType != "sell", "Invalid operation, buy offers only.");

    //     var txResult = new Transaction { Buyer = Message.Sender, Seller = Owner, SrcAmount = stratoshiAmount, CrsAmount = totalCost };
    //     Log(txResult);

    //     return txResult;
    // }

    private ulong ToFull(ulong a)
    {
        // a / 100000000 = 50
        return a / 100000000;
    }

    public struct Transaction
    {
        public Address Buyer;
        public Address Seller;
        public ulong CrsAmount; // total amount traded
        public ulong SrcAmount; // total amount traded
    }
}
