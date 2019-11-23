using Moq;
using Stratis.SmartContracts.CLR;
using Stratis.SmartContracts;
using Xunit;

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
            MockPersistentState.Setup(x => x.GetAddress(nameof(Buyer))).Returns(Buyer);
            MockPersistentState.Setup(x => x.GetAddress(nameof(TokenAddress))).Returns(TokenAddress);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenAmount))).Returns(amount);
            MockPersistentState.Setup(x => x.GetUInt64(nameof(TokenPrice))).Returns(price);
            MockPersistentState.Setup(x => x.GetBool(nameof(IsActive))).Returns(true);

            return new BuyOffer(MockContractState.Object, TokenAddress, amount, price);
        }

        [Theory]
        [InlineData(0, 10_000_000, 5_000_000_000)]
        [InlineData(500_000_000, 10_000_000, 5_000_000_000)]
        public void Creates_New_Trade(ulong value, ulong price, ulong amount)
        {
            var trade = NewBuyOffer(Buyer, value, amount, price);

            MockPersistentState.Verify(x => x.SetAddress(nameof(Buyer), Buyer));
            Assert.Equal(Buyer, trade.Buyer);
            
            MockPersistentState.Verify(x => x.SetAddress(nameof(TokenAddress), TokenAddress));
            Assert.Equal(TokenAddress, trade.TokenAddress);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenPrice), price));
            Assert.Equal(price, trade.TokenPrice);

            MockPersistentState.Verify(x => x.SetUInt64(nameof(TokenAmount), amount));
            Assert.Equal(amount, trade.TokenAmount);

            MockPersistentState.Verify(x => x.SetBool(nameof(IsActive), true));
            Assert.Equal(true, trade.IsActive);
        }
    }
}
