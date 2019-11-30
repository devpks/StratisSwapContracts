using Stratis.SmartContracts;

[Deploy]
public class SoftwareConfig : SmartContract
{
    public SoftwareConfig(ISmartContractState smartContractState, string version, string config)
        : base(smartContractState)
    {
        UpdateAdmin(Message.Sender, true);
        // Should we set this, is defaut string.Empty?
        Version = version ?? "0.0.0";
        UpdateConfig(version, config);
    }

    private const string AdminKey = "Admin";
    private const string ContributorKey = "Contributor";
    private const string ConfigKey = "Config";

    public string Version
    {
        get => PersistentState.GetString(nameof(Version));
        private set => PersistentState.SetString(nameof(Version), value);
    }

    public string GetCurrentConfig()
    {
        return GetConfigByVersion(Version);
    }

    public string GetConfigByVersion(string version)
    {
        return PersistentState.GetString($"{ConfigKey}:{version}");
    }

    public bool IsAdmin(Address address)
    {
        return PersistentState.GetBool($"{AdminKey}:{address}");
    }

    public bool IsContributor(Address address)
    {
        return PersistentState.GetBool($"{ContributorKey}:{address}");
    }

    public void UpdateConfig(string version, string config)
    {
        var isAdmin = IsAdmin(Message.Sender);
        var isContributor = IsContributor(Message.Sender);

        Assert(isAdmin || isContributor);

        Version = version;
        PersistentState.SetString($"{ConfigKey}:{version}", config);

        Log(new UpdateConfigLog
        {
            Version = version,
            Config = config,
            Blame = Message.Sender,
            Role = isAdmin ? AdminKey : ContributorKey
        });
    }

    public void UpdateAdmin(Address address, bool value)
    {
        Assert(IsAdmin(Message.Sender));

        PersistentState.SetBool($"{AdminKey}:{address}", value);

        Log(new UpdateRoleLog
        {
            Admin = Message.Sender,
            UpdatedAddress = address,
            UpdatedValue = value,
            Action = nameof(UpdateAdmin)
        });
    }

    public void UpdateContributor(Address address, bool value)
    {
        Assert(IsAdmin(Message.Sender));

        PersistentState.SetBool($"{AdminKey}:{address}", value);

        Log(new UpdateRoleLog
        {
            Admin = Message.Sender,
            UpdatedAddress = address,
            UpdatedValue = value,
            Action = nameof(UpdateContributor)
        });
    }

    public struct UpdateRoleLog
    {
        public Address Admin;
        public Address UpdatedAddress;
        public bool UpdatedValue;
        public string Action;
    }

    public struct UpdateConfigLog
    {
        public string Version;
        public string Config;
        public Address Blame;
        public string Role;
    }
}
