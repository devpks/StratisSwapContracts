using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;

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
        private readonly ulong Amount;
        private readonly ulong Price;
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
            MockPersistentState.Setup(x => x.GetAddress(nameof(Seller))).Returns(Seller);
            MockPersistentState.Setup(x => x.GetAddress(nameof(TokenAddress))).Returns(TokenAddress);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Amount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(Price))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new SellOffer(MockContractState.Object, TokenAddress, amount, price);
        }

        [Theory]
        [InlineData(0, 10_000_000, 5_000_000_000)]
        [InlineData(500_000_000, 10_000_000, 5_000_000_000)]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount)
        {
            var trade = NewSellOffer(Seller, value, amount, price);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Seller), Seller));
            Assert.Equal(Seller, trade.Seller);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(TokenAddress), TokenAddress));
            Assert.Equal(TokenAddress, trade.TokenAddress);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Price), price));
            Assert.Equal(price, trade.TokenPrice);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(Amount), amount));
            Assert.Equal(amount, trade.TokenAmount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true));
            Assert.Equal(true, trade.IsActive);
        }
    }
}
