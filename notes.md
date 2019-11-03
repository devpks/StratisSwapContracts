# Cirrus Swap
Cirrus Decentralized Exchange

## Four Contracts

1. [OrderBooks](#1.-orderbooks)
2. [OrderBook](#2.-orderbook)
3. [Trade](#3.-trade)
4. [Swap Wallet](#4.-swap-wallet)

## 1. OrderBooks
Keeps record of all orderbooks (tokens) being traded. Easy single entry point to find any given orderbook's address.

### Properties

### Methods

**Add OrderBook**

**Get OrderBook**

**Get OrderBooks**

---

## 2. OrderBook
Individual order book for each token being traded.

### Properties

### Methods

**Add Trade**

**Partial Trade**

**FulFill Trade**

**Cancel Trade**

**Get Trades**

---

## 3. Trade
Executes trades between buyer(s) and seller(s)

### Properties

### Methods

**Cancel**

// buyer(s) info
// seller(s) info

// .00001 = 1_000
// .0001 = 10_000
// .001 = 100_000
// .01 = 1_000_000
// .1 = 10_000_000
// 1 = 100_000_000
// 10 = 1_000_000_000
// 100 = 10_000_000_000
// 1000 = 100_000_000_000

// Buy
// Amount of SRC to buy
// Cost of each one
// Results in total cost to buyer

// Scenario:
// Tyler creates a new contract. He wants to buy 100adt at .1crs per.
// So his total cost would be 10crs.

// Creates new contract
    // -- Supplys the token address, trade action, the price per token and the crs willing to spend. 
    // -- (address, "buy", .1, 10)
// Seller will bid on the contract through their wallet
    // Wallet will set an allowance for the trade contract
    // Call the trade contract, which will verify it's allowance
    // Calculate total allowance and price per token
    // Call the trade contract, transfer tokens
        // On failure - Stop here
        // On Success - withdraw crs

// Sell
// Amount of SRC to sell
// Cost of each one
// Results in total cost to buyer

// Scenario
// Tyler creates a new contract to sell. He wants to sell 100adt at .1crs per.
// The buyer would need to pay Tyler 10crs for all of them.

---

## 4. Swap Wallet
Holds balances of CRS and any SRC token used for all trades.

### Properties

**Owner**

Single owner of the wallet and funds. Can request a new owner, new owner must accept.

### Methods

**ChangeOwner** - `Address newOwner`

Request to change owner to new address

**AcceptOwnership**

Accepts ownership from sending address

**Withdraw** - `string token`, `ulong amount`

Withdraws the specified token and amount to the owners address

---

## Auth Roles and Flow

1. User creates a new swap wallet by creating a new smart contract and supplying the approved bytecode.
2. User sends funds to the new smart contract address (in theory)
3. User calls CreateTrade from the swap wallet contract
  - As a Buyer
    -  Sends CRS tokens
    - Sends token address of desired SRC-20
    - Sends amount of tokens to purchase
  - As a Seller
    - Approves contract address for amount to sell or directly
    - todo
4. Trade is executed
  - Trade contract calls token contract to "TransferFrom"
    - Supplies the address of the seller
    - Seller would have the trade contract address approved for the amount they want to sell
  - On Failure - No CRS is transferred
  - On success - CRS is released from the trade contract to the seller
5. On Success of Trade Contract creation - If Selling - Approve trade contract address for the amount being sold
