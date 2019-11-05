using Stratis.SmartContracts;

[Deploy]
public class OrderBook : SmartContract
{
    public OrderBook(ISmartContractState smartContractState) : base (smartContractState)
    {
        // Check open order prices [ 100, 101, 105, 110 ]
        // Check last sale price
        // Get closes relative price depending on action buy or sell
        // Get orders at that price
    }

    public Address Token {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public ulong CurrentPrice {
        get => PersistentState.GetUInt64(nameof(CurrentPrice));
        private set => PersistentState.SetUInt64(nameof(CurrentPrice), value);
    }

    public Order[] GetOrdersAtPrice(ulong price) {
        // OpenOrdersAt:1000000
        return PersistentState.GetArray<Order>($"OpenOrdersAt:{price}");
    }

    public ulong[] GetOpenOrderPrices()
    {
        return PersistentState.GetArray<ulong>("OpenOrderPrices");
    }

    public void SetOpenOrderPrices(ulong price)
    {
        // Read, arrange, adjust open order prices
    }

    private void SetOrderAtPrice(Order order)
    {
        // save order to end of array
    }

    public void PreparePlaceOrder()
    {
        // User wants to buy or sell
        // Check order book for qualifying Trades
        // If some, return address
        // If none, return message to create new order
    }

    public void PlaceOrder()
    {
        // Fullfill existing trade, if one, 
    }

    // Can we share these?
    public struct Order 
    {
        public ulong Price;
    }

    public struct Buyer
    {

    }

    public struct Seller
    {

    }
}