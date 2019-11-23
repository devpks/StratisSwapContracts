# Cirrus Swap
Cirrus Decentralized Exchange Contracts

Consists of two contracts at this time, a Buy Offer contract and a SellOffer contract. 

## Buy Offer Contract

## Sell Offer Contract
Users create these for src token sales.

### Creating the contract

**ByteCode**

```text
0000001231203120312030123012
```

**Hash**

```text
asdlkfjasdlfkjasdlfkjasdf
```

**Parameters** 

- `Address tokenAddress` - The contact address of the token for sale.
- `ulong tokenAmount` - The amount of src tokens to sell in full. (e.g. 1 for 1src token)
- `ulong tokenPrice` - The amount of crs per src token in satoshi amounts (e.g. 1crs = 100_000_000).

**Post Deployment**

After the contract is created the new contract address must be Approved to spend the amount of src tokens specified in the `tokenAmount` parameter.

- Call the `Approve(Address tradeContractAddress, ulong oldBalance, ulong newBalance)` method at the `tokenAddress` of the src tokent to be traded.
  - Supply the new trade contract address to spend up to the `tokenAmount` specified.
- Upon success, the `tradeContract` address will have an allowance that will be used to settle the transaction.

### Cancelling the contract

The creator of the contract has abilities to cancel the contract. This sets the `IsActive` flag to false preventing further user interaction.

- Call the `CloseTrade` method on the trade contract address

### Get Trade Details

Prior to attempting to settle a contract, make a local call to `GetTradeDetails` to preview the contracts state. Returns a `TradeDetails` struct. 

```csharp
public struct TradeDetails
{
  public bool IsActive; // True if trade is open
  public ulong TokenAmount; // Amount of srcToken for sale
  public ulong TokenPrice; // Ask price of each src token
  public Address TokenAddress; // Address of src token for sale
}
```

**Note** - Prior to attempting to settle a sell contract, you may also want to make a local call to the src tokens contract to verify that the contract address is approved to send the necessary amount of src tokens. Safe gaurds are in place for the contract isn't properly approved, but gas for calling the contract will be lost.

### Filling a Trade

Since this is a sell contract, all trades will call the `Buy(ulong amountToPurchase)` method supplying the amount of src tokens they'd like to purchase as a parameter.

- Validations are ran to fail fast prior to settling any part of the trade.
  - Fail fast if necessary
- Src token amount is sent to the buyer.
  - Upon success, crs token is transferred to seller.
  - Upon failure, no funds will move.
- Any remaining balance from buyer, return to them
- Check if the `TokenAmount` to sell is 0 or if the contract is not fully filled.
  - If fully filled - close contract and return `Transaction` result
  - If not full filled - Update `TokenAmount` and return `Transaction` result

```csharp
// Index Buyer for all transactions as Seller value is constant.
public struct Transaction
{
    [Index]
    public Address Buyer;
    public Address Seller;
    public ulong TokenAmount; // Amout of src token sold
    public ulong TokenPrice; // Price of each src token sold
    public ulong TotalPrice; // Total price of transaction
}
```

