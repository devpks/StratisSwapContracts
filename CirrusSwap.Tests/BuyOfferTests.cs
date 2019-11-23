using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static BuyOffer;
using Castle.DynamicProxy.Contributors;

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

        [Theory]
        [InlineData(500_000_000, 50, 10_000_000)]
        public void Success_GetTradeDetails(ulong value, ulong price, ulong amount)
        {
            var trade = NewBuyOffer(Buyer, value, amount, price);
            var actualTradeDetails = trade.GetTradeDetails();
            var expectedTradeDetails = new TradeDetails
            {
                IsActive = true,
                TokenAmount = amount,
                TokenPrice = price,
                TokenAddress = TokenAddress,
                ContractBalance = value
            };


            Assert.Equal(expectedTradeDetails, actualTradeDetails);
        }

        [Fact]
        public void CloseTrade_Failure_If_Sender_IsNot_Owner()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, SellerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => trade.CloseTrade());
        }

        [Fact]
        public void CloseTrade_Success_If_Sender_IsNot_Owner()
        {
            var trade = NewBuyOffer(Buyer, 1, 1, 1);

            trade.CloseTrade();

            MockContractState.Verify(x => x.GetBalance, Times.AtLeastOnce);
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockContractState.Setup(x => x.GetBalance).Returns(() => 0);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);

            Assert.Equal((ulong)0, trade.Balance);
            Assert.Equal(false, trade.IsActive);
        }
    }
}
