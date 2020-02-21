# Stratis Swap

Stratis Swap is a set of four contracts contracts working together with a UI to create a decentralized exchange on the Stratis Sidechain of Stratis.

1. [Simple BuyOrder](#simple-buy-order)
2. [Simple SellOrder](#simple-sell-order)
3. [Orders](#orders-history)
4. [JsonConfig](#json-config)

## [Buy Order](./StratisSwap/SimpleBuyOrder)

A contract used to secure CRS and release after successful transfers of requested SRC token at a specific price.

## [Simple Sell Order](./StratisSwap/SimpleSellOrder)

A contract used to request a specific amount of an SRC token at a specifed price. Receives CRS tokens after successfully transferring SRC tokens.

## [Simple Orders](./StratisSwap/OrdersHistory)

A contract used to log orders so users can find orders to fill without direct interaction with each other. Orders should be logged after they have been validated and have a new contract address. This is done manually so there can be different order books.

## [Json Config](./StratisSwap/JsonConfig)

A contract used to log and feed the frontend small, non version specific JSON data. This is helpful when wanting to have a dynamic UI without relying on a centralized API.
