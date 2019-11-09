using Stratis.SmartContracts;

[Deploy]
public class SwapWallet : SmartContract, ISwapWallet
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

    private Address GetTokenOrderBook(Address token)
    {
        var orderBook = PersistentState.GetAddress($"TokenOrderBook:{token}");

        if (orderBook == Address.Zero)
        {
            // Get the orderbook for the token from orderbooks
            var orderBooksResult = Call(Address.Zero, 0, "GetTokenOrderBook", new object[] { token } );

            Assert(orderBooksResult.Success);

            orderBook = (Address)orderBooksResult.ReturnValue;

            // Save the orderbook address for that token
            SetTokenOrderBook(token, orderBook);
        }

        return orderBook;
    }

    private Order[] GetOpenOrdersToFulfill(Address orderBook, string action, ulong price, ulong amount)
    {
        var orderBookResponse = Call(orderBook, amount, action, new object[] { price, amount });

        Assert(orderBookResponse.Success);

        var orders = (Order[])orderBookResponse.ReturnValue;

        return orders;
    }

    private void SetTokenOrderBook(Address token, Address orderbook)
    {
        PersistentState.SetAddress($"TokenOrderBook:{token}", orderbook);
    }

    private OpenTrade CreateNewTrade(ulong amount, ulong price)
    {
        var newTrade = Create<Trade>(amount, new object[] { });

        Assert(newTrade.Success);

        return new OpenTrade
        {
            TradeAmount = amount,
            TradePrice = price,
            TradeAddress = newTrade.NewContractAddress
        };
    }

    private Transaction[] BuildTransactionsList(Transaction[] transactions, Transaction transaction) 
    {
        var newLength = transactions.Length + 1;
        var newTransactions = new Transaction[newLength];

        for (var i = 0; i < transactions.Length; i++)
        {
            newTransactions[i] = transactions[i];
        }

        newTransactions[newLength] = transaction;

        return newTransactions;
    }

    public TransactionResponse Buy(Address token, ulong price, ulong amount) {
        // Verify sender is owner
        Assert(Message.Sender == Owner);

        // Create new TransactionResponse
        var transactionResponse = new TransactionResponse();

        // Get the order book
        var orderBook = GetTokenOrderBook(token);

        // Call the orderbook to get available sell offers at the limit price
        var orders = GetOpenOrdersToFulfill(orderBook, "FindAvailableSellOrdersToFulfill", price, amount);

        // Fulfill returned orders
        if (orders.Length > 0)
        {
            // loop through and satisfy returned offers
            foreach(var order in orders)
            {
                // Set the amount to trade with
                var amountToTrade = amount >= order.Amount ? order.Amount : amount;

                // Satisfy all or part of the order
                var orderResult = Call(order.TradeAddress, amountToTrade, "Buy", new object[] {});

                Assert(orderResult.Success);

                var orderTransaction = (Transaction)orderResult.ReturnValue;

                // deduct from amount
                amount -= amountToTrade;

                // Add successfull transaction to array
                transactionResponse.Transactions = BuildTransactionsList(transactionResponse.Transactions, orderTransaction);
            }
        }

        // If no orders to begin with or remaining amount after fulfilling other orders
        if (orders.Length == 0 || amount > 0) 
        {
            // remaining amount, create new contract
            var newTrade = CreateNewTrade(amount, price);
            transactionResponse.OpenTrade = newTrade;
        }
        
        return transactionResponse;
    }

    public TransactionResponse Sell(Address token, ulong amount, ulong price) {
        // Verify sender is owner
        Assert(Message.Sender == Owner);

        // Create new TransactionResponse
        var transactionResponse = new TransactionResponse();

        // Get the order book
        var orderBook = GetTokenOrderBook(token);

        // Call the orderbook to get available sell offers at the limit price
        var orders = GetOpenOrdersToFulfill(orderBook, "FindAvailableBuyOrdersToFulfill", price, amount);

        // Fulfill returned orders
        if (orders.Length > 0)
        {
            // loop through and satisfy returned offers
            foreach(var order in orders)
            {
                // Set the amount to trade with
                var amountToTrade = amount >= order.Amount ? order.Amount : amount;

                // Satisfy all or part of the order
                var orderResult = Call(order.TradeAddress, amountToTrade, "Sell", new object[] {});

                Assert(orderResult.Success);

                var orderTransaction = (Transaction)orderResult.ReturnValue;

                // deduct from amount
                amount -= amountToTrade;

                // Add successfull transaction to array
                transactionResponse.Transactions = BuildTransactionsList(transactionResponse.Transactions, orderTransaction);
            }
        }

        // If no orders to begin with or remaining amount after fulfilling other orders
        if (orders.Length == 0 || amount > 0) 
        {
            // remaining amount, create new contract
            var newTrade = CreateNewTrade(amount, price);
            transactionResponse.OpenTrade = newTrade;
        }
        
        return transactionResponse;
    }
    #endregion

    public struct Order
    {
        public Address TradeAddress;
        public ulong Price;
        public ulong Amount;
        public bool IsOpen;
    }

    public struct TransactionResponse
    {
        public Transaction[] Transactions;
        public OpenTrade OpenTrade;
    }

    public struct OpenTrade
    {
        public Address TradeAddress;
        public ulong TradeAmount;
        public ulong TradePrice;
    }
}