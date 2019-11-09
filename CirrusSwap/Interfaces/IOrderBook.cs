using Stratis.SmartContracts;

public interface IOrderBook
{
    #region Properties
    
    #endregion

    #region Methods
    ///<summary>
    /// Creates a new Buy Order and returns new Trade Contract
    ///</summary>
    void CreateBuyOrder();
    void CreateSellOrder();

    void FulfillSellOrder();
    void FulfillBuyOrder();

    void CloseOrder(Address tradeContract);

    ulong[] GetOpenBuyOrderPrices();
    ulong[] GetOpenSellOrderPrices();

    void GetOpenOrdersAtPrice(ulong price);

    void UpsertOpenSellOrderPrice(ulong price);
    void UpsertOpenBuyOrderPrice(ulong price);

    void GetPrice();
    #endregion
}