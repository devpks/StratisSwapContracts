using Stratis.SmartContracts;

[Deploy]
public class JsonConfig : SmartContract
{
    /// <summary>
    /// Manages small json configurations for software projects.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="config">Json payload to log</param>
    public JsonConfig(ISmartContractState smartContractState, string config)
        : base(smartContractState)
    {
        UpdateAdminExecute(Message.Sender, true);
        UpdateConfigExecute(config);
    }

    private const string AdminKey = "Admin";
    private const string ContributorKey = "Contributor";

    public bool IsAdmin(Address address)
    {
        return PersistentState.GetBool($"{AdminKey}:{address}");
    }

    public bool IsContributor(Address address)
    {
        return PersistentState.GetBool($"{ContributorKey}:{address}");
    }

    public void UpdateAdmin(Address address, bool value)
    {
        Assert(IsAdmin(Message.Sender));

        UpdateAdminExecute(address, value);
    }

    private void UpdateAdminExecute(Address address, bool value)
    {
        PersistentState.SetBool($"{AdminKey}:{address}", value);

        Log(new RoleLog
        {
            Blame = Message.Sender,
            UpdatedAddress = address,
            UpdatedValue = value,
            Action = nameof(UpdateAdmin),
            Block = Block.Number
        });
    }

    public void UpdateContributor(Address address, bool value)
    {
        Assert(IsAdmin(Message.Sender));

        PersistentState.SetBool($"{ContributorKey}:{address}", value);

        Log(new RoleLog
        {
            Blame = Message.Sender,
            UpdatedAddress = address,
            UpdatedValue = value,
            Action = nameof(UpdateContributor),
            Block = Block.Number
        });
    }

    public void UpdateConfig(string config)
    {

        Assert(IsAdmin(Message.Sender) || IsContributor(Message.Sender));

        UpdateConfigExecute(config);
    }

    private void UpdateConfigExecute(string config)
    {
        Log(new ConfigLog
        {
            Config = config,
            Blame = Message.Sender,
            Block = Block.Number
        });
    }

    public struct RoleLog
    {
        [Index]
        public Address Blame;

        [Index]
        public Address UpdatedAddress;

        public string Action;

        public bool UpdatedValue;

        public ulong Block;
    }

    public struct ConfigLog
    {
        [Index]
        public Address Blame;

        public string Config;

        public ulong Block;
    }
}
