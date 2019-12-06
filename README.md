# Cirrus Swap

Cirrus Swap is a set of four contracts contracts working together with a UI to create a decentralized exchange on the Cirrus Sidechain for Stratis.

1. [BuyOrder Contract](#buy-order-contract)
2. [SellOrder Contract](#sell-order-contract)
3. [Orders Contract](#orders-contract)
4. [JsonConfig Contract](#json-config-contract)

## [Buy Order Contract](./CirrusSwap/BuyOrder)

A contract used to secure CRS and release after successful transfers of requested SRC token at a specific price.

### Buy Order Use Case

Johnny wants to buy 10 SRC tokens at .1 CRS each. Johnny creates a new **BuyOrder** contract specifying the token address he wants to buy, how many and at what price (in satoshis). The contract will validate Johnny's inputs and also ensure that he sent enough CRS tokens to cover the buy order. The contract will hold the CRS tokens until the order is filled or Johnny cancels the contract.

## [Sell Order Contract](./CirrusSwap/SellOrder)

A contract used to request a specific amount of an SRC token at a specifed price. Receives CRS tokens after successfully transferring SRC tokens.

### Sell Order Use Case

Johnny wants to sell 10 SRC tokens at .1 CRS each. Johnny creates a new **SellOrder** contract specifying the token address he wants to sell, how many and at what price (in satoshis). The contract will validate Johnny's inputs and be created.

After the contract is created, Johnny must call the `Approve` method on the token's contract that he wants to sell and _Approve_ the new order's contract address to spend at least the amount that he is ordering to sell. If Johnny is using CirrusSwapUI, this will do it for him but comes with a cost of the extra calls gas price.

This contract will release SRC tokens to buyers after validations. After transfer success of SRC, the contract will release CRS to the seller.

## [Orders Contract](./CirrusSwap/Orders)

A contract used to log new orders so users can find orders to fill without direct interaction. Orders are logged once, from the UI after creation and not called from within **BuyOrder** or **SellOrder** contracts.

## Orders Use Case

Johnny just made a **BuyOrder** and now has a contract address where users can sell him SRC tokens. He doesn't want to go search for sellers, so he sends his order details to the **Orders** contract. This will log his input and anyone can search the logs, for free, for orders to fill. If the seller is using CirrusSwapUI, this will make finding orders to fill easy.

### Example Struct

```csharp
public struct Order
{
  [Index]
  public Address Owner;

  [Index]
  public Address TokenAddress;
  
  public Address ContractAddress;

  public ulong Block;
}
```

## [Json Config Contract](./CirrusSwap/JsonConfig)

A contract used to log and feed the frontend small, non version specific JSON data. This is helpful when wanting to have a dynamic UI without relying on a centralized API.

### JSON Config Use Case

Tyler is a software developer and is building a DApp. He doesn't want any part of his DApp to talk to a centralized API but he needs to be able to update data without forcing users to upgrade the frontend. Tyler creates a **JsonConfig** contract so he can update small JSON configuration payloads for the frontend to interpret.

_Note:_ Minifiy payload for cheaper gas costs.

### Example JSON

```JSON
{
  "LatestUiVersion": "1.0.0",
  "LatestUiDownload": "https://github.com/mrtpain/CirrusSwapUI/releases",
  "OrdersContractAddress": "Cam6mmCcCv4zZN65ppF28tHLhT2DfwuU26",
  "FancyMessage": "December means Christmas!"
}
```
