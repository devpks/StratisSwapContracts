# Stratis Swap

Stratis Swap is a set of four contracts contracts working together with a UI to create a decentralized exchange on the Stratis Sidechain of Stratis.

1. [BuyOrder Contract](#buy-order-contract)
2. [SellOrder Contract](#sell-order-contract)
3. [Orders Contract](#orders-contract)
4. [JsonConfig Contract](#json-config-contract)

## [Buy Order Contract](./StratisSwap/BuyOrder)

A contract used to secure CRS and release after successful transfers of requested SRC token at a specific price.

### Buy Order Use Case

Johnny wants to buy 10 SRC tokens at .1 CRS each. Johnny creates a new **BuyOrder** contract specifying the token address he wants to buy, at what price (in satoshis) and how many. The contract will hold the CRS tokens until the order is filled by a seller or Johnny cancels the contract. This contract will release CRS tokens to sellers after validations and successful transfers of SRC.

## [Sell Order Contract](./StratisSwap/SellOrder)

A contract used to request a specific amount of an SRC token at a specifed price. Receives CRS tokens after successfully transferring SRC tokens.

### Sell Order Use Case

Johnny wants to sell 10 SRC tokens at .1 CRS each. Johnny creates a new **SellOrder** contract specifying the token address he wants to sell, at what price (in satoshis) and how many. The contract will validate Johnny's inputs and be created.

After the contract is created, Johnny must call the `Approve` method on the token's contract that he wants to sell and _Approve_ the new order's contract address to spend at least the amount that he wants to sell.

This contract will release SRC tokens to buyers after validations. After successful transfers of SRC, the contract will release CRS to the seller.

## [Orders Contract](./StratisSwap/Orders)

A contract used to log orders so users can find orders to fill without direct interaction with each other. Orders should be logged after they have been validated and have a new contract address. This is done manually so there can be different order books.

### Orders Use Case

Johnny just made a **BuyOrder** and now has a contract address where users can sell him SRC tokens. He doesn't want to go search for sellers, so he sends his order details to the **Orders** contract. This will log his input and anyone can search the logs for orders to fill.

#### Example Logged Order

```JSON
{
  "owner": "CNXp26iEE3EbJC9RRLBZ2cYnP7a8L3Z84F",
  "token": "CRWDdNei9teh3ancbEcBPMu4d3q575t7aK",
  "order": "CffkmA9Dy6s1Fsh3GJuBurDEpsVyZ6Loeq",
  "block": 12345
}
```

## [Json Config Contract](./StratisSwap/JsonConfig)

A contract used to log and feed the frontend small, non version specific JSON data. This is helpful when wanting to have a dynamic UI without relying on a centralized API.

### JSON Config Use Case

Tyler is a software developer and is building a DApp. He doesn't want any part of his DApp to talk to a centralized API but he needs to be able to update data without forcing users to upgrade the frontend. Tyler creates a **JsonConfig** contract so he can update small JSON configuration payloads for the frontend to interpret.

_Note:_ Minifiy payload for cheaper gas costs.

#### Example JSON Config

```JSON
{
  "latestUiVersion": "1.0.0",
  "latestUiDownload": "https://github.com/mrtpain/StratisSwapUI/releases",
  "ordersContractAddress": "Cam6mmCcCv4zZN65ppF28tHLhT2DfwuU26",
  "fancyMessage": "December means Christmas!"
}
```
