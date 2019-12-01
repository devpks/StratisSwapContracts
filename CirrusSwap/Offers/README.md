# Offers Contract

This contract is used to log new offers so users can fill trades without direct interaction.

```csharp
public struct Offer
{
  [Index]
  public Address Owner;

  [Index]
  public Address TokenAddress;
  public Address ContractAddress;
  public ulong TokenAmount;
  public ulong TokenPrice;
  public string TradeAction;
  public ulong Block;
}
```
