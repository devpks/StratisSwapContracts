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

        private Orders CreateNewOrdersContract()
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(OrdersContractAddress, Sender, 0));
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            var orders = new Orders(MockContractState.Object);

            return orders;
        }

        [Fact]
        public void Success_Logs_New_Order()
        {
            var orders = CreateNewOrdersContract();

            orders.AddOrder(TokenAddress, OrderContractAddress);

            var expectedLog = new Order
            {
                Owner = Sender,
                TokenAddress = TokenAddress,
                OrderAddress = OrderContractAddress,
                Block = orders.Block.Number
            };

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), expectedLog), Times.Once);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_TokenAddress()
        {
            var tokenAddress = Address.Zero;
            var orders = CreateNewOrdersContract();


            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.AddOrder(tokenAddress, OrderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_OrderContractAddress()
        {
            var orderContractAddress = Address.Zero;
            var orders = CreateNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.AddOrder(TokenAddress, orderContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Order>()), Times.Never);
        }
    }
}
