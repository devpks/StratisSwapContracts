# Cirrus Swap

Cirrus Swap is a set of four contracts contracts working together with a UI to create a decentralized exchange on the Cirrus Sidechain for Stratis.

1. [BuyOffer Contract](#buy-offer-contract)
2. [SellOffer Contract](#sell-offer-contract)
3. [Offers Contract](#offers-contract)
4. [JsonConfig Contract](#json-config-contract)

## [Buy Offer Contract](./CirrusSwap/BuyOffer)

A contract used to secure CRS and release after successful transfers of requested SRC token at a specific price.

### Buy Offer Use Case

Johnny wants to buy 10 SRC tokens at .1 CRS each. Johnny creates a new **BuyOffer** contract specifying the token address he wants to buy, how many and at what price (in satoshies). The contract will validate Johnny's inputs and also ensure that he sent enough CRS tokens to cover the buy order. The contract will hold the CRS tokens until the order is filled or Johnny cancels the contract.  

## [Sell Offer Contract](./CirrusSwap/SellOffer)

A contract used to request a specific amount of an SRC token at a specifed price. Receives CRS tokens after successfully transferring SRC tokens.

### Sell Offer Use Case

Johnny wants to sell 10 SRC tokens at .1 CRS each. Johnny creates a new **SellOffer** contract specifying the token address he wants to sell, how many and at what price (in satoshies). The contract will validate Johnny's inputs and be created.

After the contract is created, Johnny must call the `Approve` method on the token's contract that he wants to sell and _Approve_ the new offer's contract address to spend at least the amount that he is offering to sell. If Johnny is using CirrusSwapUI, this will do it for him but comes with a cost of the extra calls gas price.

This contract will release SRC tokens to buyers after validations. After transfer success of SRC, the contract will release CRS to the seller.

## [Offers Contract](./CirrusSwap/Offers)

A contract used to log new offers so users can fill trades without direct interaction. Offers are logged once, from the UI after creation and not called from within **BuyOffer** or **SellOffer** contracts.

## Offers Use Case

Johnny just made a **BuyOffer** and now has a contract address where users can sell him SRC tokens. He doesn't want to go search for sellers, so he sends his offer details to the **Offers** contract. This will log his input and anyone can search the logs, for free, for orders to fill. If the seller is using CirrusSwapUI, this will make finding orders to fill easy.

```csharp
public struct Offer
{
  [Index]
  public Address Owner;

  [Index]
  public Address TokenAddress;
  public Address ContractAddress;
  public ulong TokenAmount;
  public ulong TokenPrice;
  public string TradeAction;
  public ulong Block;
}
```

## [Json Config Contract](./CirrusSwap/JsonConfig)

A contract used to log and feed the frontend small, non version specific JSON data. This is helpful when wanting to have a dynamic UI without relying on a centralized API.

### JSON Config Use Case

Tyler is a software developer and is building a DApp. He doesn't want any part of his DApp to talk to a centralized API but he needs to be able to update data without forcing users to upgrade the frontend. Tyler creates a **JsonConfig** contract so he can update small JSON configuration payloads for the frontend to interpret.

_Note:_ Minifiy payload for cheaper gas costs.

```JSON
{
  "LatestUiVersion": "1.0.0",
  "LatestUiDownload": "https://github.com/mrtpain/CirrusSwapUI/releases",
  "OffersContractAddress": "Cam6mmCcCv4zZN65ppF28tHLhT2DfwuU26",
  "FancyMessage": "December means Christmas!"
}
```
