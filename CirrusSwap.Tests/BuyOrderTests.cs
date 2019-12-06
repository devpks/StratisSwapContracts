using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static BuyOrder;

namespace CirrusSwap.Tests
{
    public class BuyOrderTests
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

        public BuyOrderTests()
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

        private BuyOrder NewBuyOrder(Address sender, ulong value, ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, sender, value));
            MockContractState.Setup(x => x.GetBalance).Returns(() => value);
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Buyer))).Returns(Buyer);
            MockPersistentState.Setup(x => x.GetAddress(nameof(TokenAddress))).Returns(TokenAddress);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenPrice))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new BuyOrder(MockContractState.Object, TokenAddress, price, amount);
        }

        [Theory]
        [InlineData(500_000_000, 10_000_000, 50)]
        [InlineData(50_000_000, 10_000_000, 5)]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount)
        {
            var order =NewBuyOrder(Buyer, value, price, amount);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Buyer), Buyer), Times.Once);
            Assert.Equal(Buyer, order.Buyer);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(TokenAddress), TokenAddress), Times.Once);
            Assert.Equal(TokenAddress, order.TokenAddress);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenPrice), price), Times.Once);
            Assert.Equal(price, order.TokenPrice);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenAmount), amount), Times.Once);
            Assert.Equal(amount, order.TokenAmount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true), Times.Once);
            Assert.Equal(true, order.IsActive);
        }

        [Theory]
        [InlineData(49_999_999, 10_000_000, 5)]
        [InlineData(0, 10_000_000, 50)]
        [InlineData(5_000_000_000, 0, 50)]
        [InlineData(5_000_000_000, 10_000_000, 0)]
        public void Create_NewTrade_Fails_Invalid_Parameters(ulong value, ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Buyer, value));

            Assert.ThrowsAny<SmartContractAssertException>(() => new BuyOrder(MockContractState.Object, TokenAddress, amount, price));
        }

        [Fact]
        public void Success_GetOrderDetails()
        {
            ulong value = 500_000_000;
            ulong price = 10_000_000;
            ulong amount = 50;
            var order =NewBuyOrder(Buyer, value, price, amount);
            var actualOrderDetails = order.GetOrderDetails();
            var expectedOrderDetails = new OrderDetails
            {
                TokenAddress = TokenAddress,
                TokenPrice = price,
                TokenAmount = amount,
                ContractBalance = value,
                OrderType = nameof(BuyOrder),
                IsActive = true
            };

            Assert.Equal(expectedOrderDetails, actualOrderDetails);
        }

        #region Close Trade
        [Fact]
        public void CloseOrder_Failure_If_Sender_IsNot_Owner()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.CloseOrder());
        }

        [Fact]
        public void CloseOrder_Success_Sender_Is_Owner()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);

            order.CloseOrder();

            MockContractState.Verify(x => x.GetBalance, Times.AtLeastOnce);
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);
            MockPersistentState.Verify(x => x.GetAddress(nameof(Buyer)), Times.AtLeastOnce);

            MockContractState.Setup(x => x.GetBalance).Returns(() => 0);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);

            Assert.Equal((ulong)0, order.Balance);
            Assert.Equal(false, order.IsActive);
        }
        #endregion

        #region Sale Method
        [Fact]
        public void SellMethod_Fails_IfContract_IsNotActive()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);

            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 1));

            Assert.Equal(false, order.IsActive);
            Assert.ThrowsAny<SmartContractAssertException>(() => order.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_Sender_IsBuyer()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Buyer, 0));

            Assert.Equal(Buyer, order.Buyer);
            Assert.ThrowsAny<SmartContractAssertException>(() => order.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_TotalPrice_IsLessThan_ContractBalance()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.GetBalance).Returns(() => 0);
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            ulong totalPrice = 1;

            Assert.True(order.Balance < totalPrice);
            Assert.ThrowsAny<SmartContractAssertException>(() => order.Sell(1));
        }

        [Fact]
        public void SellMethod_Fails_If_SrcTransfer_Fails()
        {
            var order =NewBuyOrder(Buyer, 1, 1, 1);
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
                .Returns(TransferResult.Transferred(false));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.Sell(amountToPurchase));

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

        }

        [Fact]
        public void SellMethod_Success_Has_Remaining_SrcTokenAmount()
        {
            var order =NewBuyOrder(Buyer, 45, 10, 4);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));
            ulong amountToPurchase = 2;
            ulong tradeCost = amountToPurchase * order.TokenPrice;
            ulong updatedContractBalance = order.Balance - tradeCost;

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

            order.Sell(amountToPurchase);

            ulong updatedTokenAmount = order.TokenAmount - amountToPurchase;
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(updatedTokenAmount);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerOne, tradeCost), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);

            // Second trade
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerTwo, 0));
            amountToPurchase = 2;
            tradeCost = amountToPurchase * order.TokenPrice;

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

            order.Sell(amountToPurchase);

            // Transfer the 2nd seller the amount of crs
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerTwo, tradeCost), Times.Once);

            // Shouldn't have enough balance to continue, close contract
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            // Return any balance to the buyer/owner
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Buyer, order.Balance), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.AtLeast(2));
        }

        [Theory]
        [InlineData(25, 10, 2, 2)]
        [InlineData(25, 10, 2, 3)]
        public void SellMethod_Success_ClosesTrade_RemainingTokenAmount_IsZero(ulong value, ulong price, ulong amount, ulong amountToPurchase)
        {
            amountToPurchase = amount >= amountToPurchase ? amountToPurchase : amount;

            var order =NewBuyOrder(Buyer, value, price, amount);
            ulong tradeCost = amountToPurchase * price;
            ulong updatedContractBalance = order.Balance - tradeCost;

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

            order.Sell(amountToPurchase);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), SellerOne, tradeCost), Times.Once);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Buyer, order.Balance), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);
        }
        #endregion
    }
}
