using Stratis.SmartContracts;

public interface ITrade
{
    #region Properties
    Address Token { get; }
    Address Owner { get; }
    ulong Amount { get; }
    ulong Price { get; }
    #endregion

    #region Methods
    Buyer GetBuyer(Address buyer);
    void SetBuyer(Buyer buyer);
    Seller GetSeller(Address buyer);
    void SetSeller(Buyer buyer);
    #endregion
}