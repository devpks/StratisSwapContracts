using Stratis.SmartContracts;

[Deploy]
public class OrderBook : SmartContract
{
    public OrderBook(ISmartContractState smartContractState) : base (smartContractState)
    {
        // Open questions
        // Do we want to track pricing?
        // Do we want to track totals at order prices?
        // Do we want to track numOpenTrades on each side buy/sell?
        // Do we want to record closed orders?
    }

    public Address Token {
        get => PersistentState.GetAddress(nameof(Token));
        private set => PersistentState.SetAddress(nameof(Token), value);
    }

    public ulong CurrentPrice {
        get => PersistentState.GetUInt64(nameof(CurrentPrice));
        private set => PersistentState.SetUInt64(nameof(CurrentPrice), value);
    }

    private ulong MinimumBuyPrice {
        get => PersistentState.GetUInt64(nameof(MinimumBuyPrice));
        set => PersistentState.SetUInt64(nameof(MinimumBuyPrice), value);
    }

    private ulong MaximumBuyPrice {
        get => PersistentState.GetUInt64(nameof(MaximumBuyPrice));
        set => PersistentState.SetUInt64(nameof(MaximumBuyPrice), value);
    }

    private ulong MinimumSellPrice {
        get => PersistentState.GetUInt64(nameof(MinimumSellPrice));
        set => PersistentState.SetUInt64(nameof(MinimumSellPrice), value);
    }

    private ulong MaximumSellPrice {
        get => PersistentState.GetUInt64(nameof(MaximumSellPrice));
        set => PersistentState.SetUInt64(nameof(MaximumSellPrice), value);
    }

    public Order[] GetBuyOrdersAtPrice(ulong price) {
        return PersistentState.GetArray<Order>($"OpenBuyOrdersAt:{price}"); // OpenBuyOrdersAt:1000000
    }

    public Order[] GetSellOrdersAtPrice(ulong price) {
        return PersistentState.GetArray<Order>($"OpenSellOrdersAt:{price}"); // OpenSellOrdersAt:1000000
    }

    private Order[] ArrangeOrdersResponse(Order[] orders, Order order)
    {
        var newLength = orders.Length + 1;
        var ordersList = new Order[newLength];

        for (var i = 0; i < orders.Length; i++)
        {
            ordersList[i] = orders[i];
        }

        ordersList[newLength] = order;

        return ordersList;
    }

    ///<summary>
    /// Used when a user wants to buy, check the sell orders first. Return any
    /// sell orders found that are less than the specified price.
    /// John wants to buy at 1adt per at a maximum price of 1crs per, check 
    /// and return sell orders that are set at prices up 1crs per adt.
    ///</summary>
    ///<returns>Returns array of orders for wallet to fulfill</returns>
    public Order[] FindAvailableSellOrdersToFulfill(ulong maxPrice, ulong maxAmount)
    {
        var ordersResponse = new Order[0];
        // If there are orders to fulfill at the desired prices
        if (MinimumSellPrice <= maxPrice)
        {
            ulong totalAmountAtPrice = 0;
            
            for (var i = MinimumSellPrice; i <= maxPrice; i++)
            {
                // Get sell orders for the given price
                var sellOrders = GetSellOrdersAtPrice(i);
                
                // if none, on to the next price
                if (sellOrders.Length == 0) 
                {
                    continue;
                }

                foreach (var order in sellOrders)
                {
                    totalAmountAtPrice += order.Amount;

                    // Gather all orders here
                    ordersResponse = ArrangeOrdersResponse(ordersResponse, order);

                    if (totalAmountAtPrice > maxAmount) 
                    {
                        break;
                    }
                } 

                if (totalAmountAtPrice > maxAmount) 
                {
                    break;
                }               
            }
        }

        return ordersResponse;
    }

    ///<summary>
    /// Used when a user wants to sell, check the buy orders first. Return any
    /// buy orders found that are at least the specified price.
    /// John wants to sell at 1crs per 1alt, check and return buy orders
    /// that are set up to 1crs per 1alt.
    ///</summary>
    public Order[] FindAvailableBuyOrdersToFulfill(ulong minPrice, ulong maxAmount)
    {
        var ordersResponse = new Order[0];
        // If there are orders to fulfill at the desired prices
        if (MaximumBuyPrice >= minPrice)
        {
            ulong totalAmountAtPrice = 0;
            
            for (var i = minPrice; i <= MaximumBuyPrice; i++)
            {
                // Get sell orders for the given price
                var buyOrders = GetBuyOrdersAtPrice(i);
                
                // if none, on to the next price
                if (buyOrders.Length == 0) 
                {
                    continue;
                }

                foreach (var order in buyOrders)
                {
                    totalAmountAtPrice += order.Amount;

                    // Gather all orders here
                    ordersResponse = ArrangeOrdersResponse(ordersResponse, order);

                    if (totalAmountAtPrice > maxAmount) 
                    {
                        break;
                    }
                } 

                if (totalAmountAtPrice > maxAmount) 
                {
                    break;
                }               
            }
        }

        return ordersResponse;
    }

    public struct Order
    {
        public Address TradeAddress;
        public ulong Price;
        public ulong Amount;
        public bool IsOpen;
    }
}