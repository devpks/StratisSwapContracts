using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static Orders;

namespace CirrusSwap.Tests
{
    public class OrdersTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Address Sender;
        private readonly Address TokenAddress;
        private readonly Address OrderContractAddress;
        private readonly Address OrdersContractAddress;

        public OrdersTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            Sender = "0x0000000000000000000000000000000000000001".HexToAddress();
            TokenAddress = "0x0000000000000000000000000000000000000002".HexToAddress();
            OrderContractAddress = "0x0000000000000000000000000000000000000003".HexToAddress();
            OrdersContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private Orders createNewOrdersContract()
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(OrdersContractAddress, Sender, 0));
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            var contract = new Orders(MockContractState.Object);

            return contract;
        }

        [Theory]
        [InlineData(1, 2, "Buy")]
        [InlineData(3, 4, "Sell")]
        public void Success_Logs_New_Order(ulong tokenAmount, ulong tokenPrice, string orderAction)
        {
            var contract = createNewOrdersContract();

            contract.AddOrder(orderAction, tokenAmount, tokenPrice, TokenAddress, OrderContractAddress);

            var expectedLog = new Order
            {
                Owner = Sender,
                TokenAddress = TokenAddress,
                ContractAddress = OrderContractAddress,
                TokenAmount = tokenAmount,
                TokenPrice = tokenPrice,
                OrderType = orderAction,
                Block = contract.Block.Number
            };

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), expectedLog), Times.Once);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_OrderType()
        {
            var incorrectAction = "IncorrectOrderType";
            var contract = createNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOrder(incorrectAction, 1, 2, TokenAddress, OrderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        public void Failure_Log_New_Order_Invalid_TokenAmount_Or_TokenPrice(ulong tokenAmount, ulong tokenPrice)
        {
            var contract = createNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOrder("Buy", tokenAmount, tokenPrice, TokenAddress, OrderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_TokenAddress()
        {
            var tokenAddress = Address.Zero;
            var contract = createNewOrdersContract();


            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOrder("Buy", 1, 1, tokenAddress, OrderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_OrderContractAddress()
        {
            var orderContractAddress = Address.Zero;
            var contract = createNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOrder("Buy", 1, 1, TokenAddress, orderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }
    }
}
