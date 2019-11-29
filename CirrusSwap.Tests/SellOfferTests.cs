using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;
using static SellOffer;

namespace CirrusSwap.Tests
{
    public class SellOfferTests
    {
        private readonly Mock<ISmartContractState> MockContractState;
        private readonly Mock<IPersistentState> MockPersistentState;
        private readonly Mock<IContractLogger> MockContractLogger;
        private readonly Mock<IInternalTransactionExecutor> MockInternalExecutor;
        private readonly Address Seller;
        private readonly Address BuyerOne;
        private readonly Address BuyerTwo;
        private readonly Address TokenAddress;
        private readonly Address ContractAddress;
        private readonly ulong TokenAmount;
        private readonly ulong TokenPrice;
        private readonly bool IsActive;

        public SellOfferTests()
        {
            MockContractLogger = new Mock<IContractLogger>();
            MockPersistentState = new Mock<IPersistentState>();
            MockContractState = new Mock<ISmartContractState>();
            MockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            MockContractState.Setup(x => x.PersistentState).Returns(MockPersistentState.Object);
            MockContractState.Setup(x => x.ContractLogger).Returns(MockContractLogger.Object);
            MockContractState.Setup(x => x.InternalTransactionExecutor).Returns(MockInternalExecutor.Object);
            Seller = "0x0000000000000000000000000000000000000001".HexToAddress();
            BuyerOne = "0x0000000000000000000000000000000000000002".HexToAddress();
            BuyerTwo = "0x0000000000000000000000000000000000000003".HexToAddress();
            TokenAddress = "0x0000000000000000000000000000000000000004".HexToAddress();
            ContractAddress = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        private SellOffer NewSellOffer(Address sender, ulong value, ulong amount, ulong price)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, sender, value));
            MockContractState.Setup(x => x.GetBalance).Returns(() => value);
            MockPersistentState.Setup(x => x.GetAddress(nameof(Seller))).Returns(Seller);
            MockPersistentState.Setup(x => x.GetAddress(nameof(TokenAddress))).Returns(TokenAddress);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenPrice))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new SellOffer(MockContractState.Object, TokenAddress, amount, price);
        }

        [Theory]
        [InlineData(0, 10_000_000, 5_000_000_000)]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount)
        {
            var trade = NewSellOffer(Seller, value, amount, price);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Seller), Seller));
            Assert.Equal(Seller, trade.Seller);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(TokenAddress), TokenAddress));
            Assert.Equal(TokenAddress, trade.TokenAddress);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenPrice), price));
            Assert.Equal(price, trade.TokenPrice);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenAmount), amount));
            Assert.Equal(amount, trade.TokenAmount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true));
            Assert.Equal(true, trade.IsActive);
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1000, 0)]
        public void Create_NewTrade_Fails_Invalid_Parameters(ulong price, ulong amount)
        {
            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, Seller, 0));

            Assert.ThrowsAny<SmartContractAssertException>(() => new SellOffer(MockContractState.Object, TokenAddress, amount, price));
        }

        [Fact]
        public void Success_GetTradeDetails()
        {
            ulong value = 0;
            ulong amount = 1;
            ulong price = 1;

            var trade = NewSellOffer(Seller, value, amount, price);

            var actualTradeDetails = trade.GetTradeDetails();
            var expectedTradeDetails = new TradeDetails
            {
                IsActive = true,
                TokenAmount = amount,
                TokenPrice = price,
                TokenAddress = TokenAddress,
                SellerAddress = Seller,
                TradeType = nameof(SellOffer)
            };

            Assert.Equal(expectedTradeDetails, actualTradeDetails);
        }

        #region Close Trade
        [Fact]
        public void CloseTrade_Failure_If_Sender_IsNot_Owner()
        {
            var trade = NewSellOffer(Seller, 0, 1, 1);

            MockContractState.Setup(x => x.Message).Returns(new Message(ContractAddress, BuyerOne, 0));

            Assert.ThrowsAny<SmartContractAssertException>(trade.CloseTrade);

            MockPersistentState.Verify(x => x.GetAddress(nameof(Seller)), Times.AtLeastOnce);
        }

        [Fact]
        public void CloseTrade_Success_Sender_Is_Owner()
        {
            var trade = NewSellOffer(Seller, 0, 1, 1);

            trade.CloseTrade();

            MockPersistentState.Verify(x => x.GetAddress(nameof(Seller)), Times.Once);
            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), false), Times.Once);

            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(false);

            Assert.Equal(false, trade.IsActive);
        }
        #endregion
    }
}
