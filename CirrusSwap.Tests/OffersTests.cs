using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static Offers;

namespace CirrusSwap.Tests
{
    public class OffersTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Address Sender;
        private readonly Address TokenAddress;
        private readonly Address TradeContractAddress;
        private readonly Address OffersContractAddress;

        public OffersTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            Sender = "0x0000000000000000000000000000000000000001".HexToAddress();
            TokenAddress = "0x0000000000000000000000000000000000000002".HexToAddress();
            TradeContractAddress = "0x0000000000000000000000000000000000000003".HexToAddress();
            OffersContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private Offers createNewOffersContract()
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(OffersContractAddress, Sender, 0));
            MockContractState.Setup(x => x.Block.Number).Returns(12345);
            var contract = new Offers(MockContractState.Object);

            return contract;
        }

        [Theory]
        [InlineData(1, 2, "Buy")]
        [InlineData(3, 4, "Sell")]
        public void Success_Logs_New_Offer(ulong tokenAmount, ulong tokenPrice, string tradeAction)
        {
            var contract = createNewOffersContract();

            contract.AddOffer(tradeAction, tokenAmount, tokenPrice, TokenAddress, TradeContractAddress);

            var expectedLog = new Offer
            {
                Owner = Sender,
                TokenAddress = TokenAddress,
                ContractAddress = TradeContractAddress,
                TokenAmount = tokenAmount,
                TokenPrice = tokenPrice,
                TradeAction = tradeAction,
                Block = contract.Block.Number
            };

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), expectedLog), Times.Once);
        }

        [Fact]
        public void Failure_Log_New_Offer_Invalid_TradeType()
        {
            var incorrectAction = "IncorrectTradeType";
            var contract = createNewOffersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOffer(incorrectAction, 1, 2, TokenAddress, TradeContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Offer>()), Times.Never);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        public void Failure_Log_New_Offer_Invalid_TokenAmount_Or_TokenPrice(ulong tokenAmount, ulong tokenPrice)
        {
            var contract = createNewOffersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOffer("Buy", tokenAmount, tokenPrice, TokenAddress, TradeContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Offer>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Offer_Invalid_TokenAddress()
        {
            var tokenAddress = Address.Zero;
            var contract = createNewOffersContract();


            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOffer("Buy", 1, 1, tokenAddress, TradeContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Offer>()), Times.Never);
        }

        [Fact]
        public void Failure_Log_New_Offer_Invalid_TradeContractAddress()
        {
            var tradeContractAddress = Address.Zero;
            var contract = createNewOffersContract();

            Assert.ThrowsAny<SmartContractAssertException>(()
                => contract.AddOffer("Buy", 1, 1, TokenAddress, tradeContractAddress));

            MockContractLogger.Verify(x => x.Log(It.IsAny<ISmartContractState>(), It.IsAny<Offer>()), Times.Never);
        }
    }
}
