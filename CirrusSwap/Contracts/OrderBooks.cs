using Stratis.SmartContracts;

[Deploy]
public class OrderBooks : SmartContract
{
    public OrderBooks(ISmartContractState smartContractState) : base (smartContractState)
    {
    }

    public Address GetOrderBook(Address token)
    {
        return PersistentState.GetAddress($"OrderBook:{token}");
    }

    private void SetOrderBook(Address token, Address orderBook)
    {
        PersistentState.SetAddress($"OrderBook:{token}", orderBook);
    }

    public void AddOrderBook(Address token)
    {
        // Verify the order book does not already exist
        var orderBook = GetOrderBook(token);
        Assert(orderBook == Address.Zero, $"OrderBook already exists for token:{token} at {orderBook}");

        // Create new order book
        var newOrderBook = Create<OrderBook>(0, new object[] { /* Insert Params Here */ });
        Assert(newOrderBook.Success);
        
        // Persist new order book
        SetOrderBook(token, newOrderBook.NewContractAddress);
    }
}