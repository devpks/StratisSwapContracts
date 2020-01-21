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
        private readonly Address Token;
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
            Token = "0x0000000000000000000000000000000000000002".HexToAddress();
            OrderContractAddress = "0x0000000000000000000000000000000000000003".HexToAddress();
            OrdersContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private Orders CreateNewOrdersContract()
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(OrdersContractAddress, Sender, 0));
            MockContractState.Setup(x => x.Block.Number).Returns(12345);

            return new Orders(MockContractState.Object);
        }

        [Fact]
        public void Success_Logs_New_Order()
        {
            var orders = CreateNewOrdersContract();

            orders.AddOrder(OrderContractAddress, Token);

            var expectedLog = new OrderLog
            {
                Owner = Sender,
                Token = Token,
                Order = OrderContractAddress,
                Block = 12345
            };

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), expectedLog), Times.Once);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_Token()
        {
            var token = Address.Zero;
            var orders = CreateNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.AddOrder(OrderContractAddress, token));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<OrderLog>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Order_Invalid_OrderContractAddress()
        {
            var orderContractAddress = Address.Zero;
            var orders = CreateNewOrdersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.AddOrder(orderContractAddress, Token));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<OrderLog>()), Times.Never);
        }

        [Fact]
        public void Success_Logs_Updated_Order()
        {
            var orders = CreateNewOrdersContract();
            var txHash = "ee345c8b55558760e49fe8706528c8f50a56a022280675094b6654c0abec4463";

            orders.UpdateOrder(OrderContractAddress, Token, txHash);

            var expectedLog = new UpdatedOrderLog
            {
                OrderTxHash = txHash,
                Token = Token,
                Order = OrderContractAddress,
                Block = 12345
            };

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), expectedLog), Times.Once);
        }

        [Fact]
        public void Failure_Log_Updated_Order_Invalid_Token()
        {
            var token = Address.Zero;
            var orders = CreateNewOrdersContract();
            var txHash = "ee345c8b55558760e49fe8706528c8f50a56a022280675094b6654c0abec4463";

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.UpdateOrder(OrderContractAddress, token, txHash));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<UpdatedOrderLog>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_Updated_Order_Invalid_OrderContractAddress()
        {
            var orderContractAddress = Address.Zero;
            var orders = CreateNewOrdersContract();
            var txHash = "ee345c8b55558760e49fe8706528c8f50a56a022280675094b6654c0abec4463";

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.UpdateOrder(orderContractAddress, Token, txHash));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<UpdatedOrderLog>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_Updated_Order_Invalid_OrderTxHash()
        {
            var orderContractAddress = Address.Zero;
            var orders = CreateNewOrdersContract();
            var txHash = "";

            Assert.ThrowsAny<SmartContractAssertException>(()
                => orders.UpdateOrder(orderContractAddress, Token, txHash));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<UpdatedOrderLog>()), Times.Never);
        }
    }
}
