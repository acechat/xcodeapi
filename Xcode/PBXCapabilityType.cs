namespace UnityEditor.iOS.Xcode
{
    /// <summary>
    /// List of all the capabilities available.
    /// </summary>
    public sealed class PBXCapabilityType
    {
        public static readonly PBXCapabilityType ApplePay = new PBXCapabilityType ("com.apple.ApplePay", true);
        public static readonly PBXCapabilityType AppGroups = new PBXCapabilityType ("com.apple.ApplicationGroups.iOS", true);
        public static readonly PBXCapabilityType AssociatedDomains = new PBXCapabilityType ("com.apple.SafariKeychain", true);
        public static readonly PBXCapabilityType BackgroundModes = new PBXCapabilityType ("com.apple.BackgroundModes", false);
        public static readonly PBXCapabilityType DataProtection = new PBXCapabilityType ("com.apple.DataProtection", true);
        public static readonly PBXCapabilityType GameCenter = new PBXCapabilityType ("com.apple.GameCenter", false, "GameKit.framework");
        public static readonly PBXCapabilityType HealthKit = new PBXCapabilityType ("com.apple.HealthKit", true, "HealthKit.framework");
        public static readonly PBXCapabilityType HomeKit = new PBXCapabilityType ("com.apple.HomeKit", true, "HomeKit.framework");
        public static readonly PBXCapabilityType iCloud = new PBXCapabilityType("com.apple.iCloud", true, "CloudKit.framework", true);
        public static readonly PBXCapabilityType InAppPurchase = new PBXCapabilityType ("com.apple.InAppPurchase", false);
        public static readonly PBXCapabilityType InterAppAudio = new PBXCapabilityType ("com.apple.InterAppAudio", true, "AudioToolbox.framework");
        public static readonly PBXCapabilityType KeychainSharing = new PBXCapabilityType ("com.apple.KeychainSharing", true);
        public static readonly PBXCapabilityType Maps = new PBXCapabilityType("com.apple.Maps.iOS", false, "MapKit.framework");
        public static readonly PBXCapabilityType PersonalVPN = new PBXCapabilityType("com.apple.VPNLite", true, "NetworkExtension.framework");
        public static readonly PBXCapabilityType PushNotifications = new PBXCapabilityType ("com.apple.Push", true);
        public static readonly PBXCapabilityType Siri = new PBXCapabilityType ("com.apple.Siri", true);
        public static readonly PBXCapabilityType Wallet = new PBXCapabilityType ("com.apple.Wallet", true, "PassKit.framework");
        public static readonly PBXCapabilityType WirelessAccessoryConfiguration = new PBXCapabilityType("com.apple.WAC", true, "ExternalAccessory.framework");

        private readonly string m_ID;
        private readonly bool m_RequiresEntitlements;
        private readonly string m_Framework;
        private readonly bool m_OptionalFramework;

        public bool OptionalFramework
        {
            get { return m_OptionalFramework; }
        }

        public string Framework
        {
            get { return m_Framework; }
        }

        public string Id
        {
            get { return m_ID; }
        }

        public bool RequiresEntitlements
        {
            get { return m_RequiresEntitlements; }
        }

        /// <summary>
        /// This private object represents what a capability changes in the PBXProject file
        /// </summary>
        /// <param name="id">The string used in the PBXProject file to identify the capability and mark it as enabled.</param>
        /// <param name="requiresEntitlements">This capability requires an entitlements file therefore we need to add this entitlements file to the code signing entitlement.</param>
        /// <param name="framework">Specify which framework need to be added to the project for this capability, if "" no framework are added.</param>
        /// <param name="optionalFramework">Some capability (right now only iCloud) adds a framework, not all the time but just when some option are checked
        /// this parameter indicates if one of them is checked.</param>
        private PBXCapabilityType(string id, bool requiresEntitlements, string framework = "", bool optionalFramework = false)
        {
            m_ID = id;
            m_RequiresEntitlements = requiresEntitlements;
            m_Framework = framework;
            m_OptionalFramework = optionalFramework;
        }
    }
}
