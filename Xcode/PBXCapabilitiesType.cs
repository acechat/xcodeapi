namespace UnityEditor.iOS.Xcode
{
    /// <summary>
    /// List of all the capabilities available.
    /// </summary>
    public sealed class PBXCapabilitiesType
    {
        public static readonly PBXCapabilitiesType ApplePay = new PBXCapabilitiesType ("com.apple.ApplePay", true);
        public static readonly PBXCapabilitiesType AppGroups = new PBXCapabilitiesType ("com.apple.ApplicationGroups.iOS", true);
        public static readonly PBXCapabilitiesType AssociatedDomains = new PBXCapabilitiesType ("com.apple.SafariKeychain", true);
        public static readonly PBXCapabilitiesType BackgroundModes = new PBXCapabilitiesType ("com.apple.BackgroundModes", false);
        public static readonly PBXCapabilitiesType DataProtection = new PBXCapabilitiesType ("com.apple.DataProtection", true);
        public static readonly PBXCapabilitiesType GameCenter = new PBXCapabilitiesType ("com.apple.GameCenter", false, "GameKit.framework");
        public static readonly PBXCapabilitiesType HealthKit = new PBXCapabilitiesType ("com.apple.HealthKit", true, "HealthKit.framework");
        public static readonly PBXCapabilitiesType HomeKit = new PBXCapabilitiesType ("com.apple.HomeKit", true, "HomeKit.framework");
        public static readonly PBXCapabilitiesType iCloud = new PBXCapabilitiesType("com.apple.iCloud", true, "CloudKit.framework", true);
        public static readonly PBXCapabilitiesType InAppPurchase = new PBXCapabilitiesType ("com.apple.InAppPurchase", false);
        public static readonly PBXCapabilitiesType InterAppAudio = new PBXCapabilitiesType ("com.apple.InterAppAudio", true, "AudioToolbox.framework");
        public static readonly PBXCapabilitiesType KeychainSharing = new PBXCapabilitiesType ("com.apple.KeychainSharing", true);
        public static readonly PBXCapabilitiesType Maps = new PBXCapabilitiesType("com.apple.Maps.iOS", false, "MapKit.framework");
        public static readonly PBXCapabilitiesType PersonalVPN = new PBXCapabilitiesType("com.apple.VPNLite", true, "NetworkExtension.framework");
        public static readonly PBXCapabilitiesType PushNotifications = new PBXCapabilitiesType ("com.apple.Push", true);
        public static readonly PBXCapabilitiesType Siri = new PBXCapabilitiesType ("com.apple.Siri", true);
        public static readonly PBXCapabilitiesType Wallet = new PBXCapabilitiesType ("com.apple.Wallet", true, "PassKit.framework");
        public static readonly PBXCapabilitiesType WirelessAccessoryConfiguration = new PBXCapabilitiesType("com.apple.WAC", true, "ExternalAccessory.framework");

        private readonly string _id;
        private readonly bool _requiresEntitlements;
        private readonly string _framework;
        private readonly bool _optionalFramework;

        public bool OptionalFramework
        {
            get { return _optionalFramework; }
        }

        public string Framework
        {
            get { return _framework; }
        }

        public string Id
        {
            get { return _id; }
        }

        public bool RequiresEntitlements
        {
            get { return _requiresEntitlements; }
        }

        /// <summary>
        /// This private object represents what a capability changes in the PBXProject file
        /// </summary>
        /// <param name="id">The string used in the PBXProject file to identify the capability and mark it as enabled.</param>
        /// <param name="requiresEntitlements">This capability requires an entitlements file therefore we need to add this entitlements file to the code signing entitlement.</param>
        /// <param name="framework">Specify which framework need to be added to the project for this capability, if "" no framework are added.</param>
        /// <param name="optionalFramework">Some capability (right now only iCloud) adds a framework, not all the time but just when some option are checked
        /// this parameter indicates if one of them is checked.</param>
        private PBXCapabilitiesType(string id, bool requiresEntitlements, string framework = "", bool optionalFramework = false)
        {
            _id = id;
            _requiresEntitlements = requiresEntitlements;
            _framework = framework;
            _optionalFramework = optionalFramework;
        }
    }
}