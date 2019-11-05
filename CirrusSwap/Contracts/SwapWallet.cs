using Stratis.SmartContracts;

[Deploy]
public class SwapWallet : SmartContract
{
    public SwapWallet(ISmartContractState smartContractState)
        : base(smartContractState)
    {
        this.Owner = Message.Sender;
    }

    #region Properties
    public Address Owner {
        get => PersistentState.GetAddress(nameof(Owner));
        private set => PersistentState.SetAddress(nameof(Owner), value);
    }

    private Address NewOwner {
        get => PersistentState.GetAddress(nameof(NewOwner));
        set => PersistentState.SetAddress(nameof(NewOwner), value);
    }
    #endregion

    #region Methods

    #region Ownership
    public void RequestNewOwner(Address newOwner)
    {
        Assert(Message.Sender == Owner);

        NewOwner = newOwner;
    }

    public void AcceptOwnership()
    {
        Assert(Message.Sender == NewOwner);

        Owner = NewOwner;

        NewOwner = Address.Zero;
    }
    #endregion

    #region CRS Actions
    public ulong GetCrsBalance()
    {
        Assert(Message.Sender == Owner);
        
        return Balance;
    }

    public ITransferResult WithdrawCrs(ulong amount)
    {
        Assert(Message.Sender == Owner);

        Assert(Balance >= amount);

        return Transfer(Message.Sender, amount);
    }
    #endregion
    
    #region SRC Actions
    public bool WithdrawSrc(Address tokenAddress, ulong amount)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "TransferTo", new object[] { Message.Sender, amount });

        Assert(result.Success);

        return (bool)result.ReturnValue;
    }

    public bool TransferToSrcAddress(Address tokenAddress, Address toAddress, ulong amount)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "TransferTo", new object[] { toAddress, amount });

        Assert(result.Success);

        return (bool)result.ReturnValue;
    }

    public bool TransferFromSrcAddress(Address tokenAddress, Address fromAddress, ulong amount)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "TransferFrom", new object[] { fromAddress, Message.Sender, amount });

        Assert(result.Success);

        return (bool)result.ReturnValue;
    }

    public bool TransferFromSrcAddress(Address tokenAddress, Address fromAddress, Address toAddress, ulong amount)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "TransferFrom", new object[] { fromAddress, toAddress, amount });

        Assert(result.Success);

        return (bool)result.ReturnValue;
    }

    public bool ApproveSrcAddress(Address tokenAddress, Address spender, ulong currentAmount, ulong amount)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "Approve", new object[] { spender, currentAmount, amount });

        Assert(result.Success);

        return (bool)result.ReturnValue;
    }

    public ulong GetSrcAllowance(Address tokenAddress, Address owner, Address spender)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "Allowance", new object[] { owner, spender });

        Assert(result.Success);

        return (ulong)result.ReturnValue;
    }

    public ulong GetSrcBalance(Address tokenAddress)
    {
        Assert(Message.Sender == Owner);

        var result = Call(tokenAddress, 0, "GetBalance", new object[] { Message.Sender });

        Assert(result.Success);

        return (ulong)result.ReturnValue;
    }
    #endregion
    
    #region Trade Actions

    #endregion

    #endregion
}