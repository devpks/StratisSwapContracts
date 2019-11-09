# Cirrus Swap
Cirrus Decentralized Exchange

## Four Contracts

1. [OrderBooks](#1-orderbooks)
2. [OrderBook](#2-orderbook)
3. [Trade](#3-trade)
4. [Swap Wallet](#4-swap-wallet)

## 1. OrderBooks
Keeps record of all orderbooks (tokens) being traded. Easy single entry point to find any given orderbook's address.

- Users query contract for a specified tokens orderbook
- Users can add new orderbook's by specifying a token
  - If an orderbook for that token doesn't exist, creates new one
  - Else, fails

---

## 2. OrderBook

Individual order book for each token being traded. It's job is to monitor all open orders and return open orders available to fulfill when queried.
When there are no open orders at the users specified price, this contract creates and returns a new Trade contract.

### Created By - OrderBooks Contract

Can be created by anyone through the orderbooks contract. Check if supplied token address already has an orderbook, if not add a new one.

- Tracks sell/buy prices
  - Query buy/sell prices to return available contracts to fulfill
  - If none, creates new Trade contract

### Wallet to OrderBook Flow

Todo: Insert steps on exactly when and where orderbook open buy/sell orders are updated.
Should it be from the wallet that the call is made to add the valid trade?
Should it be updated when the orderbook creates the new token contract prior to funding?

**Basic Flow**

- Send Request (price, amount)
- If orders to fulfill based on price
  - return orders to wallet contract to fulfill
- If No orders to fulfill
  - Create new token contract
  - Return new token contract
  - Fund new token contract (from wallet)

**Buy Orders to be Fulfilled**

- Request to orderbook (price, amount)
- Return orders to be fulfilled to wallet
- foreach (orderToFulfill) - call token contract
  - satisfy all or part of the contract
  - if all - continue;
  - if partial - break loop
- Remaining amounts - call orderbook
  - Check for new orders to fulfill
    - No orders to fulfill - create new contract
    - Else, create new contract

**Sell Orders to be Fulfilled**

- Request to orderbook (price, amount)
- Return orders to be fulfilled to wallet
- foreach (orderToFulfill) - call token contract
  - satisfy all or part of the contract
  - if all - continue;
  - if partial - break loop
- Remaining amounts - call orderbook
  - Check for new orders to fulfill
    - No orders to fulfill - create new contract
    - Else, create new contract

**No Orders to be Fulfilled**

- Request to orderbook (price, amount)
- Create new contract
- Return new contract
- Wallet funds new contract

---

## 3. Trade
Executes trades between buyer(s) and seller(s)

**On Creation**

- Orderbook calls to create this contract when there are no orders to fulfill
- Sets owner as users wallet address
- Returns contract address

---

## 4. Swap Wallet
Holds balances of CRS and any SRC token used for all trades.

**Place Order**

- Call order book with price and amount
- If orders to fulfill
  - Enter open orders
    - fulfill all or partial of the order
      - If all, continue
      - If partial, end
    - Remaining amounts, create order on orderbook
- Else no orders to fulfill
  - Create new Trade contract
  - Save open trade address

---

## Auth Roles and Flow (Outdated)

- User creates a new swap wallet by creating a new smart contract and supplying the approved bytecode.
- User sends funds to the new smart contract address (in theory)
- User calls CreateTrade from the swap wallet contract
  - As a Buyer
    - Sends CRS tokens
    - Sends token address of desired SRC-20
    - Sends amount of tokens to purchase
  - As a Seller
    - Approves contract address for amount to sell or directly
    - todo
- Trade is executed
  - Trade contract calls token contract to "TransferFrom"
    - Supplies the address of the seller
    - Seller would have the trade contract address approved for the amount they want to sell
  - On Failure - No CRS is transferred
  - On success - CRS is released from the trade contract to the seller
- On Success of Trade Contract creation - If Selling - Approve trade contract address for the amount being sold

---

## Gas Key

Token / Satoshi

- .00000001 = 1
- .0000001 = 10
- .000001 = 100
- .00001 = 1_000
- .0001 = 10_000
- .001 = 100_000
- .01 = 1_000_000
- .1 = 10_000_000
- 1 = 100_000_000
- 10 = 1_000_000_000
- 100 = 10_000_000_000
- 1_000 = 100_000_000_000
- 10_000 = 100_000_000_000_000
- 1_000_00 = 100_000_000_000_000_000
- 1_000_000 = 100_000_000_000_000_000_000
