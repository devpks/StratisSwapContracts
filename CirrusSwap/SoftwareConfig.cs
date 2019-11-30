using Stratis.SmartContracts;

[Deploy]
public class SoftwareConfig : SmartContract
{
    /// <summary>
    /// Manages version and json configurations for software projects.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="version">Current version of the application</param>
    /// <param name="config">Json payload to store with version</param>
    public SoftwareConfig(ISmartContractState smartContractState, string version, string config)
        : base(smartContractState)
    {
        UpdateAdminExecute(Message.Sender, true);
        Version = version;
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

        UpdateAdminExecute(address, value);
    }

    private void UpdateAdminExecute(Address address, bool value)
    {
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
        [Index]
        public Address Admin;

        [Index]
        public Address UpdatedAddress;

        public string Action;

        public bool UpdatedValue;
    }

    public struct UpdateConfigLog
    {
        [Index]
        public Address Blame;

        [Index]
        public string Version;

        public string Config;

        public string Role;
    }
}
