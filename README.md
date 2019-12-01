# Cirrus Swap

Cirrus Decentralized Exchange Contracts

Consists of four contracts separate contracts which are utlizied in CirrusSwapUI.

## [Buy Offer Contract](BuyOffer.md)

A contract used to secure CRS and release after successful transfers of requested SRC token at a specific price.

## Sell Offer Contract

A contract used to request a specific amount of an SRC token at a specifed price. Receives CRS tokens after successfully transferring SRC tokens.

## Offers Contract

This contract is used to log new offers so users can fill trades without direct interaction.

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

## Json Config Contract

This contract is used to feed the frontend non version specific JSON data.

_Note:_ Minifiy payload for better gas costs.

```JSON
{
  "UiVersion": "1.0.0",
  "OffersContract": "Cam6mmCcCv4zZN65ppF28tHLhT2DfwuU26",
  "FancyMessage": "December means Christmas!"
}
```
