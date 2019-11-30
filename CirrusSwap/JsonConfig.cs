using Stratis.SmartContracts;

[Deploy]
public class SoftwareConfig : SmartContract
{
    /// <summary>
    /// Manages versions and small json configurations for software projects.
    /// </summary>
    /// <param name="smartContractState">The execution state for the contract.</param>
    /// <param name="version">Current version of the application</param>
    /// <param name="config">Json payload to store with version</param>
    public SoftwareConfig(ISmartContractState smartContractState, string version, string config)
        : base(smartContractState)
    {
        UpdateAdminExecute(Message.Sender, true);
        Version = version;
        UpdateConfigExecute(version, config);
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
        var serializedKey = GetSerializedKey(version);

        byte[] payload = PersistentState.GetBytes(serializedKey);

        return Serializer.ToString(payload);
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

        UpdateConfigExecute(version, config);
    }

    private void UpdateConfigExecute(string version, string config)
    {
        Version = version;

        var serializedKey = GetSerializedKey(version);
        var serializedConfig = this.Serializer.Serialize(config);

        PersistentState.SetBytes(serializedKey, serializedConfig);

        Log(new UpdateConfigLog
        {
            Version = version,
            Config = config,
            Blame = Message.Sender
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

    private byte[] GetSerializedKey(string version)
    {
        var key = $"{ConfigKey}:{version}";

        return Serializer.Serialize(key);
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
    }
}
