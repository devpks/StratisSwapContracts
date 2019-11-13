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
        var action = tradeAction.ToLowerInvariant();
        Assert(action == buy || action == sell, "Action must me buy or sell");
        Assert(amount > 0, "Amount must be greater than 0");
        Assert(price > 0, "Price must be greater than 0");

        this.Token = token;
        this.Owner = Message.Sender;
        this.OwnerAction = action;
        this.Amount = amount;
        this.Price = price;
        this.TotalPrice = amount * price;

        if (action == buy) 
        {
            Assert(VerifyBuyAction());
        }
        else if (action == sell)
        {
            Assert(VerifySellAction());
        }

        this.IsActive = true;
    }

    private bool VerifyBuyAction()
    {
        // Verify the value sent covers the amount to buy and purchase price
        Assert(Message.Value > this.TotalPrice);

        return true;
    }

    private bool VerifySellAction()
    {
        // Verify the balance of this contracts address
        var result = Call(this.Token, 0, "Allowance", new object[] { });

        Assert(result.Success);

        Assert((ulong)result.ReturnValue >= this.TotalPrice);

        return true;
    }

    public void Destroy()
    {
        Assert(Message.Sender == Owner);

        if (this.OwnerAction == buy)
        {
            // Need to keep an updated balance through txs
            this.Transfer(this.Owner, this.Amount * this.Price);
        }
        else if (this.OwnerAction == sell)
        {
            // Need to keep an updated balance through txs
            var result = Call(this.Token, 0, "TransferTo", new object[] { this.Owner, this.Amount * this.Price });

            Assert(result.Success);
        }

        this.IsActive = false;
    }

    public ulong TotalPrice => this.Price * this.Amount;

    private bool IsActive 
    {
        get => PersistentState.GetBool(nameof(IsActive));
        set => PersistentState.SetBool(nameof(IsActive), value);
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

    public Buyer GetBuyer(Address buyer) {
        return PersistentState.GetStruct<Buyer>($"Buyer:{buyer}");
    }

    private void SetBuyer(Buyer buyer) {
        PersistentState.SetStruct($"Buyer:{buyer.Address}", buyer);
    }

    public Seller GetSeller(Address seller) {
        return PersistentState.GetStruct<Seller>($"Seller:{seller}");
    }

    private void SetSeller(Seller seller) {
        PersistentState.SetStruct($"Seller:{seller.Address}", seller);
    }

    #region Transactions
    public Transaction[] GetTransactions()
    {
        return PersistentState.GetArray<Transaction>("Transactions");
    }

    private void SetNewTransaction(Transaction transaction) 
    {
        var transactions = GetTransactions();
        var newLength = transactions.Length + 1;
        var newTransactions = new Transaction[newLength];

        for (var i = 0; i < transactions.Length; i++)
        {
            newTransactions[i] = transactions[i];
        }

        newTransactions[newLength] = transaction;

        PersistentState.SetArray("Transactions", newTransactions);
    }
    #endregion

    public Transaction Buy(ulong amount)
    {
        Assert(Message.Sender != Owner);
        Assert(OwnerAction != "buy", "Invalid operation, sell offers only.");

        var totalCost = amount * Price;
        // Verify
        Assert(amount <= Amount, "Cannot purchase more than what is beings sold");
        Assert(Message.Value >= totalCost, "Not enough funds to cover purchase.");

        // Transfer SRC
        var transferResult = Call(Token, 0, "TransferFrom", new object[] {Owner, Message.Sender, amount});
        // Verify Successful Transfer
        Assert(transferResult.Success);

        // Transfer CRS totalCost to seller
        Transfer(Owner, totalCost);
        // Any remainder transfer back to buyer
        // Balance record of who deposited x amount needs to be made for withdraw abilities
        if (Balance > 0)
        {
            Transfer(Message.Sender, Balance);
        }

        // Create, Set Buyer
        Buyer buyer = NewBuyer(Message.Sender, Token, amount, totalCost, Balance);
        SetBuyer(buyer);

        // Get, Edit, Set Seller
        Seller seller = GetSeller(Owner);
        seller.AmountRecieved = totalCost;
        seller.AmountSold = amount;
        seller.Balance -= amount;
        SetSeller(seller);

        // Build Transaction Result
        var txResult = new Transaction { Buyer = buyer, Seller = seller };

        // Save TransactionResult
        SetNewTransaction(txResult);
        
        return txResult;
    }

    public Transaction Sell(ulong amount)
    {
        Assert(Message.Sender != Owner);
        Assert(OwnerAction != "sell", "Invalid operation, buy offers only.");

        var txResult = new Transaction { Buyer = GetBuyer(Message.Sender), Seller = GetSeller(Owner) };

        return txResult;
    }

    #region Helpers
    private Buyer NewBuyer(
        Address address, Address token, ulong amountPurchasd, ulong amountSpent, ulong balance)
    {
        return new Buyer 
        {
            Address = address,
            Token = token,
            AmountPurchased = amountPurchasd,
            AmountSpent = amountSpent,
            Balance = balance
        };
    }

    private Seller NewSeller(
        Address address, Address token, ulong amountSold, ulong amountRevieved, ulong balance)
    {
        return new Seller 
        {
            Address = address,
            Token = token,
            AmountSold = amountSold,
            AmountRecieved = amountRevieved,
            Balance = balance
        };
    }
    #endregion
}
