using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static SellOrder;
using Microsoft.CodeAnalysis.Operations;

namespace CirrusSwap.Tests
{
    public class SellOrderTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Mock<IInternalTransactionExecutor> MockInternalExecutor;
        private readonly Mock<ISerializer> MockSerializer;
        private readonly Address Seller;
        private readonly Address BuyerOne;
        private readonly Address BuyerTwo;
        private readonly Address Token;
        private readonly Address ContractAddress;
        private readonly ulong Amount;
        private readonly ulong Price;
        private readonly bool IsActive;
        private const ulong DefaultAmount = 10;
        private const ulong DefaultPrice = 10_000_000;
        private const ulong DefaultZeroValue = 0;
        private const ulong DefaultCostValue = 100_000_000;

        public SellOrderTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            MockSerializer = new Mock<ISerializer>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            MockContractState.Setup(x => x.InternalTransactionExecutor).Returns(MockInternalExecutor.Object);
            MockContractState.Setup(x => x.Serializer).Returns(MockSerializer.Object);
            Seller = "0x0000000000000000000000000000000000000001".HexToAddress();
            BuyerOne = "0x0000000000000000000000000000000000000002".HexToAddress();
            BuyerTwo = "0x0000000000000000000000000000000000000003".HexToAddress();
            Token = "0x0000000000000000000000000000000000000004".HexToAddress();
            ContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private SellOrder NewSellOrder(Address sender, ulong value, ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, sender, value));
            MockContractState.Setup(x => x.GetBalance).Returns(() => value);
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Seller))).Returns(Seller);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Token))).Returns(Token);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Price))).Returns(price);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new SellOrder(MockContractState.Object, Token, price, amount);
        }

        [Fact]
        public void Creates_New_SellOrder()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Seller), Seller));
            Assert.Equal(Seller, order.Seller);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(Token), Token));
            Assert.Equal(Token, order.Token);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Price), DefaultPrice));
            Assert.Equal(DefaultPrice, order.Price);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), DefaultAmount));
            Assert.Equal(DefaultAmount, order.Amount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true));
            Assert.Equal(true, order.IsActive);
        }

        [Theory]
        [InlineData(0, DefaultAmount)]
        [InlineData(DefaultPrice, 0)]
        public void Create_NewOrder_Fails_Invalid_Parameters(ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Seller, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => new SellOrder(MockContractState.Object, Token, amount, price));
        }

        [Fact]
        public void Success_GetOrderDetails()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            var actualOrderDetails = order.GetOrderDetails();

            var expectedOrderDetails = new OrderDetails
            {
                Seller = Seller,
                Token = Token,
                Price = DefaultPrice,
                Amount = DefaultAmount,
                OrderType = nameof(SellOrder),
                IsActive = true
            };

            Assert.Equal(expectedOrderDetails, actualOrderDetails);
        }

        #region Close Order
        [Fact]
        public void CloseOrder_Fails_Sender_IsNot_Owner()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(order.CloseOrder);
        }

        [Fact]
        public void CloseOrder_Success()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            order.CloseOrder();

            MockPersistentState.Verify(x => x.GetAddress(nameof(Seller)), Times.Once);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);
        }
        #endregion

        #region Buy Method
        [Fact]
        public void Buy_Fails_IfContract_IsNotActive()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.Buy(DefaultAmount));

            var expectedCallParams = new object[] { Seller, BuyerOne, DefaultAmount };

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Never);

            MockInternalExecutor.Verify(x => x.Transfer(It.IsAny<ISmartContractState>(), Seller, DefaultAmount), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), 0), Times.Never);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Never);
        }

        [Fact]
        public void Buy_Fails_If_Sender_IsSeller()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Seller, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.Buy(DefaultAmount));

            var expectedCallParams = new object[] { Seller, BuyerOne, DefaultAmount };

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Never);

            MockInternalExecutor.Verify(x => x.Transfer(It.IsAny<ISmartContractState>(), Seller, DefaultAmount), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), 0), Times.Never);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Never);
        }

        [Fact]
        public void Buy_Fails_If_MessageValue_IsLessThan_Cost()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, DefaultZeroValue));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.Buy(DefaultAmount));

            var expectedCallParams = new object[] { Seller, BuyerOne, DefaultAmount };

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Never);

            MockInternalExecutor.Verify(x => x.Transfer(It.IsAny<ISmartContractState>(), Seller, DefaultAmount), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), 0), Times.Never);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Never);
        }

        [Fact]
        public void Buy_Fails_If_SrcTransfer_Fails()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, DefaultCostValue));

            var expectedCallParams = new object[] { Seller, BuyerOne, DefaultAmount };
            MockInternalExecutor.Setup(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0))
                .Returns(TransferResult.Transferred(false));

            Assert.ThrowsAny<SmartContractAssertException>(() => order.Buy(DefaultAmount));

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Once);

            MockInternalExecutor.Verify(x => x.Transfer(It.IsAny<ISmartContractState>(), Seller, DefaultCostValue), Times.Never);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Never);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), 0), Times.Never);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Never);
        }

        [Fact]
        public void Buy_Success_Until_Amount_IsGone()
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            // First Seller
            ulong amountToBuy = DefaultAmount - 5;
            ulong expectedUpdatedAmount = DefaultAmount - amountToBuy;
            ulong orderCost = amountToBuy * DefaultPrice;

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, orderCost));

            var expectedCallParams = new object[] { Seller, BuyerOne, amountToBuy };
            MockInternalExecutor.Setup(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0))
                .Returns(TransferResult.Transferred(true));

            // Transfers CRS to seller, callback to update the contracts balance
            MockInternalExecutor.Setup(x => x.Transfer(It.IsAny<ISmartContractState>(), Seller, orderCost))
                .Callback(() => MockContractState.Setup(x => x.GetBalance).Returns(() => DefaultCostValue - orderCost));

            MockPersistentState.Setup(x => x.SetUInt64(nameof(Amount), expectedUpdatedAmount))
                .Callback(() => MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(expectedUpdatedAmount));

            order.Buy(amountToBuy);

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Once);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Seller, orderCost), Times.Once);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), expectedUpdatedAmount));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);


            // Second Seller
            ulong secondAmountToBuy = expectedUpdatedAmount;
            ulong secondUpdatedAmount = secondAmountToBuy - expectedUpdatedAmount;
            ulong secondOrderCost = secondAmountToBuy * DefaultPrice;

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerTwo, secondOrderCost));

            var secondExpectedCallParams = new object[] { Seller, BuyerTwo, secondAmountToBuy };

            MockInternalExecutor.Setup(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", secondExpectedCallParams, 0))
                .Returns(TransferResult.Transferred(true));

            MockPersistentState.Setup(x => x.SetUInt64(nameof(Amount), secondUpdatedAmount))
                .Callback(() => MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(secondUpdatedAmount));

            order.Buy(secondAmountToBuy);

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", secondExpectedCallParams, 0), Times.Once);

            // Runs twice because in this case we're sending the same amount to the same seller (owner)
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Seller, secondOrderCost), Times.AtLeast(2));

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), secondUpdatedAmount));

            // Shouldn't have enough balance to continue, close contract
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.AtLeast(2));
        }

        [Theory]
        [InlineData(DefaultAmount)]
        [InlineData(DefaultAmount + 1)]
        public void Buy_Success_Remaining_Amount_IsZero_CloseOrder(ulong amountToBuy)
        {
            amountToBuy = DefaultAmount >= amountToBuy ? amountToBuy : DefaultAmount;

            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            ulong orderCost = amountToBuy * DefaultPrice;
            ulong updatedContractBalance = order.Balance - orderCost;

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, orderCost));

            var expectedCallParams = new object[] { Seller, BuyerOne, amountToBuy };

            MockInternalExecutor.Setup(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0))
                .Returns(TransferResult.Transferred(true));

            MockInternalExecutor.Setup(x => x.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()))
                .Callback(() => MockContractState.Setup(x => x.GetBalance).Returns(() => updatedContractBalance));

            MockPersistentState.Setup(x => x.SetUInt64(nameof(Amount), DefaultAmount - amountToBuy))
                .Callback(() => MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(DefaultAmount - amountToBuy));

            order.Buy(amountToBuy);

            MockInternalExecutor.Verify(x =>
                x.Call(It.IsAny<ISmartContractState>(), Token, 0, "TransferFrom", expectedCallParams, 0), Times.Once);

            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), Seller, orderCost), Times.Once);

            // Only runs on the second test if there is a balance to transfer
            MockContractState.Verify(x => x.InternalTransactionExecutor
                .Transfer(It.IsAny<ISmartContractState>(), BuyerOne, order.Balance), Times.AtMostOnce());

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), 0), Times.Once);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Transaction>()), Times.Once);
        }

        [Theory]
        [InlineData("1.0000", 10_000, 100_000_000)]
        [InlineData("1.0000", 100_000, 1_000_000_000)]
        [InlineData("1.0000", 1_000_000, 10_000_000_000)]
        [InlineData("1.0000", 10_000_000, 100_000_000_000)]
        [InlineData("1.0000", 100_000_000, 1_000_000_000_000)]
        [InlineData("1234.5678", 23_450, 289_506_149_100)]
        [InlineData("19484.7657", 1_000, 194_847_657_000)]
        // 0.23 * 14.7656 = 0.3396088
        [InlineData("0.0230", 147_656, 33_960_880)]
        public void Correctly_Calculates_Totals(string amount, ulong price, ulong expectedCost)
        {
            var order = NewSellOrder(Seller, DefaultZeroValue, DefaultPrice, DefaultAmount);

            Assert.Equal(expectedCost, order.CalculateTotals(amount, price));
        }

        [Theory]
        // Minimum price 1 = .0001crs
        [InlineData("1.0000", 10_000, 100_000_000)]
        [InlineData("1.0000", 10_000, 100_000_000)]
        [InlineData("1.0000", 100_000, 1_000_000_000)]
        [InlineData("1.0000", 1_000_000, 10_000_000_000)]
        [InlineData("1.0000", 10_000_000, 100_000_000_000)]
        [InlineData("1.0000", 100_000_000, 1_000_000_000_000)]
        [InlineData("1234.5678", 23_450, 289_506_149_100)]
        [InlineData("19484.7657", 1_000, 194_847_657_000)]
        // 0.23 * 14.7656 = 0.3396088
        [InlineData("0.0230", 147_656, 33_960_880)] 
        public void CanCalculate_Amount_FromString(string amount, ulong price, ulong expectedCost)
        {
            ulong delimiter = 10_000;

            Assert.True(amount.Length >= 6);

            var splitAmount = amount.Split(".");

            ulong.TryParse(splitAmount[0], out ulong integer);
            ulong.TryParse(splitAmount[1], out ulong fractional);

            ulong integerTotal = integer * delimiter * price;
            ulong fractionalTotal = fractional * price;

            var cost = integerTotal + fractionalTotal;

            Assert.Equal(expectedCost, cost);
        }

        [Theory]
        [InlineData("1.00000001", 100_000_001, 100_000_000)]
        [InlineData("1.0000001", 10_000_001, 10_000_000)]
        [InlineData("1.000001", 1_000_001, 1_000_000)]
        [InlineData("1.00001", 100_001, 100_000)]
        [InlineData("1.0001", 10_001, 10_000)]
        [InlineData("1.001", 1_001, 1_000)]
        [InlineData("1.01", 101, 100)]
        [InlineData("1.1", 11, 10)]
        public void ParseDecimalStringToSatoshis(string amount, ulong expectedTotal, ulong delimiter)
        {
            var splitAmount = amount.Split(".");

            ulong.TryParse(splitAmount[0], out ulong integer);
            ulong.TryParse(splitAmount[1], out ulong fractional);

            ulong integerTotal = integer * delimiter;
            ulong fractionalTotal = fractional;

            var total = integerTotal + fractionalTotal;

            Assert.Equal(expectedTotal, total);
        }

        #endregion
    }
}
