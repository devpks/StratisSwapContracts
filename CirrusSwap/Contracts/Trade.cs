using Stratis.SmartContracts;

[Deploy]
public class Trade : SmartContract
{
    private const string buy = "buy";
    private const string sell = "sell";
    public Trade(
        ISmartContractState smartContractState, 
        Address token, 
        ulong amount, 
        ulong sellPrice) : base (smartContractState)
    {
        var isBuyContract = Message.Value > 0 && sellPrice == 0;
        var isSellContract = Message.Value == 0 && sellPrice > 0;

        Assert(amount > 0, "Price must be greater than 0");
        Assert(isBuyContract || isSellContract);

        Token = token;
        Owner = Message.Sender;
        Amount = amount;

        if (isBuyContract)
        {
            ContractType = buy;
            Price = Message.Value;
        }
        else if (isSellContract)
        {
            ContractType = sell;
            Price = sellPrice;
        }

        IsActive = true;
    }

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
        Assert(IsActive);
        Assert(Message.Sender == Owner);

        // Any left over balance belongs to owner 
        if (ContractType == buy)
        {
            Transfer(Owner, Balance);
        }
        else if (ContractType == sell)
        {
            // hmmm message.value or balance here?
            Transfer(Message.Sender, Message.Value);
        }

        IsActive = false;
    }

    public Transaction Buy() // amount of src to buy in full e.g. (50 adt)
    {
        // Verify
        Assert(ContractType == sell, "Invalid operation, buy offers only.");
        Assert(Message.Value >= Price, "Not enough funds to cover purchase.");

        // Execute the transaction
        return ExecuteTransaction(Message.Sender, Owner);
    }

    public Transaction Sell()
    {
        // Verify
        Assert(ContractType == buy, "Invalid operation, sell offers only.");

        // Execute the transaction
        return ExecuteTransaction(Owner, Message.Sender);
    }

    private Transaction ExecuteTransaction(Address buyer, Address seller)
    {
        // Verify
        Assert(IsActive);
        Assert(Message.Sender != Owner);

        // Transfer SRC-Contract address must have necessary allowance
        var transferResult = Call(Token, 0, "TransferFrom", new object[] { seller, buyer, Amount });

        // Verify Successful Transfer
        if (!transferResult.Success)
        {
            Destroy();
        }

        // Transfer CRS totalCost to seller
        Transfer(seller, Price);

        // Any remainder transfer back to buyer
        if (Balance > 0)
        {
            // is this correct? message.value 
            Transfer(buyer, Balance);
        }

        var txResult = new Transaction
        {
            Buyer = buyer,
            Seller = seller,
            SrcAmount = Amount,
            CrsAmount = Price
        };

        Log(txResult);

        // Destroy contract
        IsActive = false;

        return txResult;
    }


    public struct Transaction
    {
        public Address Buyer;
        public Address Seller;
        public ulong CrsAmount; // total amount traded
        public ulong SrcAmount; // total amount traded
    }
}
