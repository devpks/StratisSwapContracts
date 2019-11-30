using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static BuyOffer;

namespace CirrusSwap.Tests
{
    public class BuyOfferTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Mock<IInternalTransactionExecutor> MockInternalExecutor;
        private readonly Address Buyer;
        private readonly Address SellerOne;
        private readonly Address SellerTwo;
        private readonly Address TokenAddress;
        private readonly Address ContractAddress;
        private readonly ulong TokenAmount;
        private readonly ulong TokenPrice;
        private readonly bool IsActive;

        public BuyOfferTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            MockContractState.Setup(x => x.InternalTransactionExecutor).Returns(MockInternalExecutor.Object);
            Buyer = "0x0000000000000000000000000000000000000001".HexToAddress();
            SellerOne = "0x0000000000000000000000000000000000000002".HexToAddress();
            SellerTwo = "0x0000000000000000000000000000000000000003".HexToAddress();
            TokenAddress = "0x0000000000000000000000000000000000000004".HexToAddress();
            ContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private BuyOffer NewBuyOffer(Address sender, ulong value, ulong amount, ulong price)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, sender, value));
            MockContractState.Setup(x => x.GetBalance).Returns(() => value);
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Buyer))).Returns(Buyer);
            MockPersistentState.Setup(x => x.GetAddress(nameof(TokenAddress))).Returns(TokenAddress);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenPrice))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new BuyOffer(MockContractState.Object, TokenAddress, amount, price);
        }

        [Theory]
        [InlineData(500_000_000, 10_000_000, 50)]
        [InlineData(50_000_000, 10_000_000, 5)]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount)
        {
            var trade = NewBuyOffer(Buyer, value, amount, price);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Buyer), Buyer), Times.Once);
            Assert.Equal(Buyer, trade.Buyer);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(TokenAddress), TokenAddress), Times.Once);
            Assert.Equal(TokenAddress, trade.TokenAddress);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenPrice), price), Times.Once);
            Assert.Equal(price, trade.TokenPrice);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenAmount), amount), Times.Once);
            Assert.Equal(amount, trade.TokenAmount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true), Times.Once);
            Assert.Equal(true, trade.IsActive);
        }

        [Theory]
        [InlineData(49_999_999, 10_000_000, 5)]
        [InlineData(0, 10_000_000, 50)]
        [InlineData(5_000_000_000, 0, 50)]
        [InlineData(5_000_000_000, 10_000_000, 0)]
        public void Create_NewTrade_Fails_Invalid_Parameters(ulong value, ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Buyer, value));

            Assert.ThrowsAny<SmartContractAssertException>(() => new BuyOffer(MockContractState.Object, TokenAddress, amount, price));
        }

        [Fact]
        public void Success_GetTradeDetails()
        {
            ulong value = 500_000_000;
            ulong price = 10_000_000;
            ulong amount = 50;
            var trade = NewBuyOffer(Buyer, value, amount, price);
            var actualTradeDetails = trade.GetTradeDetails();
            var expectedTradeDetails = new TradeDetails
            {
                IsActive = true,
                TokenAmount = amount,
                TokenPrice = price,
                TokenAddress = TokenAddress,
                ContractBalance = value,
                TradeType = nameof(BuyOffer)
            };

            Assert.Equal(expectedTradeDetails, actualTradeDetails);
        }

        #region Close Trade
        [Fact]
        public void CloseTrade_Failure_If_Sender_IsNot_Owner()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => trade.CloseTrade());
        }

        [Fact]
        public void CloseTrade_Success_Sender_Is_Owner()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            trade.CloseTrade();

            MockContractState.Verify(x => x.GetBalance, Times.AtLeastOnce);
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);
            MockPersistentState.Verify(x => x.GetAddress(nameof(Buyer)), Times.AtLeastOnce);

            MockContractState.Setup(x => x.GetBalance).Returns(() => 0);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);

            Assert.Equal((ulong)0, trade.Balance);
            Assert.Equal(false, trade.IsActive);
        }
        #endregion

        #region Sale Method
        [Fact]
        public void SellMethod_Fails_IfContract_IsNotActive()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 1));

            Assert.Equal(false, trade.IsActive);
            Assert.ThrowsAny<SmartContractAssertException>(() => trade.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_Sender_IsBuyer()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Buyer, 0));

            Assert.Equal(Buyer, trade.Buyer);
            Assert.ThrowsAny<SmartContractAssertException>(() => trade.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_TokenAmount_IsLessThan_AmountToPurchase()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            ulong amountToPurchase = 2;

            Assert.True(amountToPurchase > trade.TokenAmount);
            Assert.ThrowsAny<SmartContractAssertException>(() => trade.Sell(amountToPurchase));
        }

        [Fact]
        public void SellMethod_Fails_If_TotalPrice_IsLessThan_ContractBalance()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.GetBalance).Returns(() => 0);
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            ulong totalPrice = 1;

            Assert.True(trade.Balance < totalPrice);
            Assert.ThrowsAny<SmartContractAssertException>(() => trade.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_SrcTransfer_Fails()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);
            ulong amountToPurchase = 1;

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            // Mock contract call
            MockInternalExecutor.Setup(s =>
                s.Call(
                    It.IsAny<ISmartContractState>(),
                    It.IsAny<Address>(),
                    It.IsAny<ulong>(),
                    "TransferFrom",
                    It.IsAny<object[]>(),
                    It.IsAny<ulong>()))
                .Returns(TransferResult.Failed);

            Assert.ThrowsAny<SmartContractAssertException>(() => trade.Sell(amountToPurchase));

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

        }

        [Fact]
        public void SellMethod_Success_Has_Remaining_SrcTokenAmount()
        {
            var trade = NewBuyOffer(Buyer, 45, 4, 10);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));
            ulong amountToPurchase = 2;
            ulong tradeCost = amountToPurchase * trade.TokenPrice;
            ulong updatedContractBalance = trade.Balance - tradeCost;

            // Mock contract call
            MockInternalExecutor.Setup(s =>
                s.Call(
                    It.IsAny<ISmartContractState>(),
                    It.IsAny<Address>(),
                    It.IsAny<ulong>(),
                    "TransferFrom",
                    It.IsAny<object[]>(),
                    It.IsAny<ulong>()))
                .Returns(TransferResult.Transferred(true));

            MockInternalExecutor.Setup(x => x.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()))
                .Callback(() => MockContractState.Setup(x => x.GetBalance).Returns(() => updatedContractBalance));

            trade.Sell(amountToPurchase);

            ulong updatedTokenAmount = trade.TokenAmount - amountToPurchase;
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(updatedTokenAmount);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerOne, tradeCost), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);

            // Second trade
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerTwo, 0));
            amountToPurchase = 2;
            tradeCost = amountToPurchase * trade.TokenPrice;

            // Mock contract call
            MockInternalExecutor.Setup(s =>
                s.Call(
                    It.IsAny<ISmartContractState>(),
                    It.IsAny<Address>(),
                    It.IsAny<ulong>(),
                    "TransferFrom",
                    It.IsAny<object[]>(),
                    It.IsAny<ulong>()))
                .Returns(TransferResult.Transferred(true));

            trade.Sell(amountToPurchase);

            // Transfer the 2nd seller the amount of crs
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerTwo, tradeCost), Times.Once);

            // Shouldn't have enough balance to continue, close contract
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            // Return any balance to the buyer/owner
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Buyer, trade.Balance), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.AtLeast(2));
        }

        [Fact]
        public void SellMethod_Success_ClosesTrade_Because_TokenAmount_Is_Zero()
        {
            var trade = NewBuyOffer(Buyer, 25, 2, 10);
            ulong amountToPurchase = 2;
            ulong tradeCost = amountToPurchase * trade.TokenPrice;
            ulong updatedContractBalance = trade.Balance - tradeCost;

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            // Mock contract call
            MockInternalExecutor.Setup(s =>
                s.Call(
                    It.IsAny<ISmartContractState>(),
                    It.IsAny<Address>(),
                    It.IsAny<ulong>(),
                    "TransferFrom",
                    It.IsAny<object[]>(),
                    It.IsAny<ulong>()))
                .Returns(TransferResult.Transferred(true));

            MockInternalExecutor.Setup(x => x.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()))
                .Callback(() => MockContractState.Setup(x => x.GetBalance).Returns(() => updatedContractBalance));

            trade.Sell(amountToPurchase);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerOne, tradeCost), Times.Once);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Buyer, trade.Balance), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);
        }
        #endregion
    }
}
