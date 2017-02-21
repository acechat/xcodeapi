/* \mainpage XCodeAPI
 * \section intro Introduction
 * This package allow you to modify the Xcode project, especially to add capabilities.
 * It's a modification and an extentions of the package provided by Unity.
 *
 * \section usage Usage
 * To use it create a ProjectCapabilityManager and add capabilities :
 * \code{.csharp}
 * var capabilityManager = new ProjectCapabilityManager(buildPath, PlayerSettings.bundleIdentifier);
 * PBXProject project = capabilityManager.AddPushNotifications().AddGameCenter().AddHomeKit().AddHealthKit().PBXProject;
 * capabilityManager.AddMaps(MapsOptions.Airplane | MapsOptions.Bike | MapsOptions.RideSharing);
 * project.SetTeamId(YourTeamIdFromAppleDeveloperConsole);
 * capabilityManager.Close();
 * \endcode
 *
 */

using System;
using System.IO;

namespace UnityEditor.iOS.Xcode
{

    /// <summary>
    /// This class is here to help you add capabilities to your xcode project.
    /// Because capabilities modify the PBXProject, the entitlements file and/or the Info.plist and not consistently,
    /// it can be detious.
    /// Therefore this class open the PBXProject that is always modify by capabilities and open Entitlement and info.plist only when needed.
    /// For optimisation reason, we write the file only in the close method.
    /// If you don't call it the file will not be written.
    ///
    /// For convignience and to keep the optimisation you can access the open PBXProject directly.
    /// </summary>
    public class ProjectCapabilityManager : IDisposable
    {
        private readonly string _buildPath;
        private readonly string _targetName;
        private readonly string _appName;
        private readonly string pbxProjectPath;
        private readonly string _entitlementFileName;
        private readonly string _entitlementFilePath;
        private PlistDocument entitlements;
        private PlistDocument infoPlist;

        private bool _closed = false;

        /// <summary>
        /// convignence property to allow you to add more changes to the PBXProject while it's still open.
        /// </summary>
        public readonly PBXProject PBXProject;


        /// <summary>
        /// Create the manager with the required paramter to open files and set the properties in the write place.
        /// Then open the PBXProject.
        /// </summary>
        /// <param name="buildPath"> the build path passed by the PostProcessBuild method</param>
        /// <param name="bundleId">The bundle Id of your application, can be returned by PlayerSettings.bundleIdentifier</param>
        /// <param name="targetName">The name of the target project by default unity produce a project named : Unity-iPhone</param>
        public ProjectCapabilityManager(string buildPath, string bundleId, string targetName = "Unity-iPhone" )
        {
            _buildPath = buildPath;
            _targetName = targetName;
            _appName = bundleId.Substring(bundleId.LastIndexOf(".") + 1);
            _entitlementFileName = _appName + ".entitlements";
            _entitlementFilePath = _buildPath + "/" + _targetName + "/" + _entitlementFileName;
            pbxProjectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject = new PBXProject();
            PBXProject.ReadFromString(File.ReadAllText(pbxProjectPath));
        }

        /// <summary>
        /// Write the actual file to the disk.
        /// If you don't call this method nothing will change.
        /// </summary>
        public void Close()
        {
            File.WriteAllText(pbxProjectPath, PBXProject.WriteToString());
            if (entitlements != null)entitlements.WriteToFile(_entitlementFilePath);
            if (infoPlist != null) infoPlist.WriteToFile(_buildPath + "/"+"Info.plist");
            _closed = true;

        }

        public void Dispose()
        {
            if (!_closed) Close();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Add the iCloud capability with the desired options.
        /// </summary>
        /// <param name="keyValueStorage">A boolean representing the Key-value storage checkbox in the project</param>
        /// <param name="iCloudDocument">A boolean representing the iCloud document storage checkbox in the project, you can then use the last paramter to add document to the capability</param>
        /// <param name="cloudKit">A boolean representing the cloudKit storage checkbox in the project, that add the framework to the project.</param>
        /// <param name="customContainer">The list of container where you want to store the domcuments on iCloud</param>
        public ProjectCapabilityManager AddICloud(bool keyValueStorage = true, bool iCloudDocument = false, bool cloudKit = false, string[] customContainer = null)
        {
            var ent = getOrCreateEntitlementDoc();
            var val = (ent.root[ICloudEnt.ContIdValue] = new PlistElementArray()) as PlistElementArray;
            if (iCloudDocument || cloudKit)
            {
                val.values.Add(new PlistElementString(ICloudEnt.ContIdValue));
                var ser = (ent.root[ICloudEnt.ServicesKey] = new PlistElementArray()) as PlistElementArray;
                if (cloudKit)
                {
                    ser.values.Add(new PlistElementString(ICloudEnt.ServicesKitValue));
                }
                if (iCloudDocument)
                {
                    ser.values.Add(new PlistElementString(ICloudEnt.ServicesDocValue));
                    var ubiquity = (ent.root[ICloudEnt.UbiContIdKey] = new PlistElementArray()) as PlistElementArray;
                    ubiquity.values.Add(new PlistElementString(ICloudEnt.UbiContIdValue));
                }
            }

            if (keyValueStorage)
            {
                ent.root[ICloudEnt.KvStoreKey] = new PlistElementString(ICloudEnt.KvStoreValue);
            }

            PBXProject.EnableCapability(PBXCapabilitiesType.ICloud, _entitlementFileName, cloudKit);
            return this;
        }

        /// <summary>
        /// Add Push (or remote) Notifications capability to your project
        /// </summary>
        /// <param name="development">Indicate if this is a development build with a development provisoning profile</param>
        public ProjectCapabilityManager AddPushNotifications(bool development = true)
        {
            getOrCreateEntitlementDoc().root[PNEnt.Key] = new PlistElementString(development ? PNEnt.DevValue : PNEnt.ProdValue);
            PBXProject.EnableCapability(PBXCapabilitiesType.PushNotifications, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add GameCenter capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddGameCenter()
        {
            var arr = (getOrCreateInfoDoc().root[GameCenterInfo.Key] ?? (getOrCreateInfoDoc().root[GameCenterInfo.Key] = new PlistElementArray())) as PlistElementArray;
            arr.values.Add(new PlistElementString(GameCenterInfo.Value));
            PBXProject.EnableCapability(PBXCapabilitiesType.GameCenter);
            return this;
        }

        /// <summary>
        /// Add Wallet capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddWallet(string[] passSubset)
        {
            var arr = (getOrCreateEntitlementDoc().root[WalletEnt.Key] = new PlistElementArray()) as PlistElementArray;
            if ((passSubset == null || passSubset.Length == 0) && arr != null)
                arr.values.Add(new PlistElementString(WalletEnt.BaseValue + WalletEnt.BaseValue));
            else
            {
                for (var i = 0; i < passSubset.Length; i++)
                {
                    if (arr != null) arr.values.Add(new PlistElementString(WalletEnt.BaseValue+passSubset[i]));
                }
            }

            PBXProject.EnableCapability(PBXCapabilitiesType.Wallet, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Siri capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddSiri()
        {
            getOrCreateEntitlementDoc().root[SiriEnt.Key] = new PlistElementBoolean(true);

            PBXProject.EnableCapability(PBXCapabilitiesType.Siri, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Apple Pay capability to the project.
        /// </summary>
        /// <param name="merchants">The list of marchant you want for apple pay</param>
        public ProjectCapabilityManager AddApplePay(string[] merchants)
        {
            var arr = (getOrCreateEntitlementDoc().root[AppPayEnt.Key] = new PlistElementArray()) as PlistElementArray;
            for (var i = 0; i < merchants.Length; i++)
            {
                arr.values.Add(new PlistElementString(merchants[i]));
            }


            PBXProject.EnableCapability(PBXCapabilitiesType.ApplePay, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add In App Purchase capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddInAppPurchase()
        {
            PBXProject.EnableCapability(PBXCapabilitiesType.InAppPurchase);
            return this;
        }


        /// BETA : not thoughly tested.
        /// <summary>
        /// Add Maps capability to the project.
        /// </summary>
        /// <param name="options">A flag reprensting which kind of transportation you want to provide information for.</param>
        public ProjectCapabilityManager AddMaps(MapsOptions options = MapsOptions.None)
        {
            var bundleArr = (getOrCreateInfoDoc().root[MapsInfo.BundleKey] ?? (getOrCreateInfoDoc().root[MapsInfo.BundleKey] = new PlistElementArray())) as PlistElementArray;
            bundleArr.values.Add(new PlistElementDict());
            PlistElementDict bundleDic = GetOrCreateUnicDictElementInArray(bundleArr);
            bundleDic[MapsInfo.BundleNameKey] = new PlistElementString(MapsInfo.BundleNameValue);
            var bundleTypeArr = (bundleDic[MapsInfo.BundleTypeKey] ?? (bundleDic[MapsInfo.BundleTypeKey]  = new PlistElementArray())) as PlistElementArray;
            GetOrCreateStringElementInArray(bundleTypeArr, MapsInfo.BundleTypeValue);


            var optionArr = (getOrCreateInfoDoc().root[MapsInfo.ModeKey] ??
                            (getOrCreateInfoDoc().root[MapsInfo.ModeKey] = new PlistElementArray())) as PlistElementArray;
            if ( (options & MapsOptions.Airplane) == MapsOptions.Airplane)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModePlaneValue);
            }
            if ((options & MapsOptions.Bike) == MapsOptions.Bike)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeBikeValue);
            }
            if ((options & MapsOptions.Bus) == MapsOptions.Bus)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeBusValue);
            }
            if ((options & MapsOptions.Car) == MapsOptions.Car)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeCarValue);
            }
            if ((options & MapsOptions.Ferry) == MapsOptions.Ferry)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeFerryValue);
            }
            if ((options & MapsOptions.Other) == MapsOptions.Other)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeOtherValue);
            }
            if ((options & MapsOptions.Pedestrian) == MapsOptions.Pedestrian)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModePedestrianValue);
            }
            if ((options & MapsOptions.RideSharing) == MapsOptions.RideSharing)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeRideShareValue);
            }
            if ((options & MapsOptions.StreetCar) == MapsOptions.StreetCar)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeStreetCarValue);
            }
            if ((options & MapsOptions.Subway) == MapsOptions.Subway)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeSubwayValue);
            }
            if ((options & MapsOptions.Taxi) == MapsOptions.Taxi)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeTaxiValue);
            }
            if ((options & MapsOptions.Train) == MapsOptions.Train)
            {
                GetOrCreateStringElementInArray(optionArr, MapsInfo.ModeTrainValue);
            }

            PBXProject.EnableCapability(PBXCapabilitiesType.Maps);
            return this;
        }

        /// <summary>
        /// Add Personal VPN capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddPersonalVPN()
        {
            var arr = (getOrCreateEntitlementDoc().root[VPNEnt.Key] = new PlistElementArray()) as PlistElementArray;
            arr.values.Add(new PlistElementString(VPNEnt.Value));

            PBXProject.EnableCapability(PBXCapabilitiesType.PersonalVPN, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Background capability to the project with the options wanted.
        /// </summary>
        /// <param name="options">A flag representing the options you want to check in this capability</param>
        public ProjectCapabilityManager AddBackgroundModes(BackgroundModesOptions options = BackgroundModesOptions.None)
        {
            var optionArr = (getOrCreateInfoDoc().root[BGInfo.Key] ??
                            (getOrCreateInfoDoc().root[BGInfo.Key] = new PlistElementArray())) as PlistElementArray;

            if ((options & BackgroundModesOptions.ActsAsABluetoothLEAccessory) == BackgroundModesOptions.ActsAsABluetoothLEAccessory)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeActsBluetoothValue);
            }
            if ((options & BackgroundModesOptions.AudioAirplayPiP) == BackgroundModesOptions.AudioAirplayPiP)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeAudioValue);
            }
            if ((options & BackgroundModesOptions.BackgroundFetch) == BackgroundModesOptions.BackgroundFetch)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeFetchValue);
            }
            if ((options & BackgroundModesOptions.ExternalAccessoryCommunication) == BackgroundModesOptions.ExternalAccessoryCommunication)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeExtAccessoryValue);
            }
            if ((options & BackgroundModesOptions.LocationUpdates) == BackgroundModesOptions.LocationUpdates)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeLocationValue);
            }
            if ((options & BackgroundModesOptions.NewsstandDownloads) == BackgroundModesOptions.NewsstandDownloads)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeNewsstandValue);
            }
            if ((options & BackgroundModesOptions.RemoteNotifications) == BackgroundModesOptions.RemoteNotifications)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModePushValue);
            }
            if ((options & BackgroundModesOptions.VoiceOverIp) == BackgroundModesOptions.VoiceOverIp)
            {
                GetOrCreateStringElementInArray(optionArr, BGInfo.ModeVOIPValue);
            }
            PBXProject.EnableCapability(PBXCapabilitiesType.BackgroundModes);
            return this;
        }

        /// <summary>
        /// Add Keychain Sharing capability to the project with a list of groups.
        /// </summary>
        /// <param name="accessGroups">The list of access groups you want to access.</param>
        public ProjectCapabilityManager AddKeychainSharing(string[] accessGroups = null)
        {
            var arr = (getOrCreateEntitlementDoc().root[KeyChainEnt.Key] = new PlistElementArray()) as PlistElementArray;
            if(accessGroups != null){
                for (var i = 0; i < accessGroups.Length; i++)
                {
                    arr.values.Add(new PlistElementString(accessGroups[i]));
                }
            }
            else
            {
                arr.values.Add(new PlistElementString(KeyChainEnt.DefaultValue));
            }

            PBXProject.EnableCapability(PBXCapabilitiesType.KeychainSharing, _entitlementFileName);
            return this;
        }


        /// <summary>
        /// Add Inter App Audio capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddInterAppAudio()
        {
            getOrCreateEntitlementDoc().root[AudioEnt.Key] = new PlistElementBoolean(true);
            PBXProject.EnableCapability(PBXCapabilitiesType.InterAppAudio, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Associated Domains capability to the project.
        /// </summary>
        /// <param name="domains">The list of domains you want to be able to access.</param>
        public ProjectCapabilityManager AddAssociatedDomains(string[] domains)
        {
            var arr = (getOrCreateEntitlementDoc().root[AssDomEnt.Key] = new PlistElementArray()) as PlistElementArray;
            for (var i = 0; i < domains.Length; i++)
            {
                arr.values.Add(new PlistElementString(domains[i]));
            }


            PBXProject.EnableCapability(PBXCapabilitiesType.AssociatedDomains, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add App Groups capability to the project.
        /// </summary>
        /// <param name="groups">The list of groups this application belong into.</param>
        public ProjectCapabilityManager AddAppGroups(string[] groups)
        {
            var arr = (getOrCreateEntitlementDoc().root[GroupsEnt.Key] = new PlistElementArray()) as PlistElementArray;
            for (var i = 0; i < groups.Length; i++)
            {
                arr.values.Add(new PlistElementString(groups[i]));
            }

            PBXProject.EnableCapability(PBXCapabilitiesType.AppGroups, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add HomeKit capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddHomeKit()
        {
            getOrCreateEntitlementDoc().root[HomeEnt.Key] = new PlistElementBoolean(true);
            PBXProject.EnableCapability(PBXCapabilitiesType.HomeKit, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Data Protection capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddDataProtection()
        {
            getOrCreateEntitlementDoc().root[ProtEnt.Key] = new PlistElementString(ProtEnt.Value);
            PBXProject.EnableCapability(PBXCapabilitiesType.DataProtection, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add HealthKit capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddHealthKit()
        {
            var capabilityArr =(getOrCreateInfoDoc().root[HealthInfo.Key] ??
                            (getOrCreateInfoDoc().root[HealthInfo.Key] = new PlistElementArray())) as PlistElementArray;
            GetOrCreateStringElementInArray(capabilityArr, HealthInfo.Value);
            getOrCreateEntitlementDoc().root[HealthEnt.Key] = new PlistElementBoolean(true);
            PBXProject.EnableCapability(PBXCapabilitiesType.HealthKit, _entitlementFileName);
            return this;
        }

        /// <summary>
        /// Add Wireless Accessory Configuration capability to the project.
        /// </summary>
        public ProjectCapabilityManager AddWirelessAccessoryConfiguration()
        {
            getOrCreateEntitlementDoc().root[WACEnt.Key] = new PlistElementBoolean(true);
            PBXProject.EnableCapability(PBXCapabilitiesType.WirelessAccessoryConfiguration, _entitlementFileName);
            return this;
        }

        private PlistDocument getOrCreateEntitlementDoc()
        {
            if (entitlements == null)
            {
                entitlements = new PlistDocument();
                string[] entitlementsFiles = Directory.GetFiles(_buildPath + "/" + _targetName + "/", _entitlementFileName);
                if (entitlementsFiles.Length > 0)
                {
                    entitlements.ReadFromFile(entitlementsFiles[0]);
                }
                else
                {

                    entitlements.create();
                }

            }

            return entitlements;
        }

        private PlistDocument getOrCreateInfoDoc()
        {
            if (infoPlist == null)
            {
                infoPlist = new PlistDocument();
                string[] infoFiles = Directory.GetFiles(_buildPath + "/", "Info.plist");
                if (infoFiles.Length > 0)
                {
                    infoPlist.ReadFromFile(infoFiles[0]);
                }
                else
                {

                    infoPlist.create();
                }

            }

            return infoPlist;
        }

        private PlistElementString GetOrCreateStringElementInArray(PlistElementArray root, string value)
        {
            PlistElementString r = null;
            var c = root.values.Count;
            var exist = false;
            for (var i = 0; i < c; i++)
            {
                if (root.values[i] is PlistElementString && (root.values[i] as PlistElementString).value == value)
                {
                    r = root.values[i] as PlistElementString;
                    exist = true;
                }
            }
            if (!exist)
            {
                r = new PlistElementString(value);
                root.values.Add(r);
            }
            return r;
        }

        private PlistElementDict GetOrCreateUnicDictElementInArray(PlistElementArray root)
        {
            PlistElementDict r;
            if (root.values.Count == 0) r = root.values[0] as PlistElementDict;
            else
            {
                r = new PlistElementDict();
                root.values.Add(r);
            }
            return r;
        }


    }

    /// <summary>
    /// The list of options available for Background Mode.
    /// </summary>
    [Flags]
    [Serializable]
    public enum BackgroundModesOptions
    {
        None                           = 0,
        AudioAirplayPiP                = 1<<0,
        LocationUpdates                = 1<<1,
        VoiceOverIp                    = 1<<2,
        NewsstandDownloads             = 1<<3,
        ExternalAccessoryCommunication = 1<<4,
        UsesBluetoothLEAccessory       = 1<<5,
        ActsAsABluetoothLEAccessory    = 1<<6,
        BackgroundFetch                = 1<<7,
        RemoteNotifications            = 1<<8
    }

    /// <summary>
    /// The list of options available for Maps.
    /// </summary>

    [Serializable]
    [Flags]
    public enum MapsOptions
    {
        None          = 0,
        Airplane      = 1<<0,
        Bike          = 1<<1,
        Bus           = 1<<2,
        Car           = 1<<3,
        Ferry         = 1<<4,
        Pedestrian    = 1<<5,
        RideSharing   = 1<<6,
        StreetCar     = 1<<7,
        Subway        = 1<<8,
        Taxi          = 1<<9,
        Train         = 1<<10,
        Other         = 1<<11
    }


    /** Follows the large quantity of string used as key and value all over the place in the info.plist or entitlements file. **/


    internal class GameCenterInfo
    {
        internal static readonly string Key = "UIRequiredDeviceCapabilities";
        internal static readonly string Value = "gamekit";
    }

    internal class MapsInfo
    {
        internal static readonly string BundleKey = "CFBundleDocumentTypes";
        internal static readonly string BundleNameKey = "CFBundleTypeName";
        internal static readonly string BundleNameValue = "MKDirectionsRequest";
        internal static readonly string BundleTypeKey = "LSItemContentTypes";
        internal static readonly string BundleTypeValue = "com.apple.maps.directionsrequest";
        internal static readonly string ModeKey = "MKDirectionsApplicationSupportedModes";
        internal static readonly string ModePlaneValue = "MKDirectionsModePlane";
        internal static readonly string ModeBikeValue = "MKDirectionsModeBike";
        internal static readonly string ModeBusValue = "MKDirectionsModeBus";
        internal static readonly string ModeCarValue = "MKDirectionsModeCar";
        internal static readonly string ModeFerryValue = "MKDirectionsModeFerry";
        internal static readonly string ModeOtherValue = "MKDirectionsModeOther";
        internal static readonly string ModePedestrianValue = "MKDirectionsModePedestrian";
        internal static readonly string ModeRideShareValue = "MKDirectionsModeRideShare";
        internal static readonly string ModeStreetCarValue = "MKDirectionsModeStreetCar";
        internal static readonly string ModeSubwayValue = "MKDirectionsModeSubway";
        internal static readonly string ModeTaxiValue = "MKDirectionsModeTaxi";
        internal static readonly string ModeTrainValue = "MKDirectionsModeTrain";

    }

    internal class BGInfo
    {
        internal static readonly string Key = "UIBackgroundModes";
        internal static readonly string ModeAudioValue = "audio";
        internal static readonly string ModeBluetoothValue = "bluetooth-central";
        internal static readonly string ModeActsBluetoothValue = "bluetooth-peripheral";
        internal static readonly string ModeExtAccessoryValue = "external-accessory";
        internal static readonly string ModeFetchValue = "fetch";
        internal static readonly string ModeLocationValue = "location";
        internal static readonly string ModeNewsstandValue = "newsstand-content";
        internal static readonly string ModePushValue = "remote-notification";
        internal static readonly string ModeVOIPValue = "voip";
    }

    internal class HealthInfo
    {
        internal static readonly string Key = "UIRequiredDeviceCapabilities";
        internal static readonly string Value = "healthkit";
    }

    internal class ICloudEnt
    {
        internal static readonly string ContIdKey = "com.apple.developer.icloud-container-identifiers";
        internal static readonly string UbiContIdKey = "com.apple.developer.ubiquity-container-identifiers";
        internal static readonly string ContIdValue = "iCloud.$(CFBundleIdentifier)";
        internal static readonly string UbiContIdValue = "iCloud.$(CFBundleIdentifier)";
        internal static readonly string ServicesKey = "com.apple.developer.icloud-services";
        internal static readonly string ServicesDocValue = "CloudDocuments";
        internal static readonly string ServicesKitValue = "CloudKit";
        internal static readonly string KvStoreKey = "com.apple.developer.ubiquity-kvstore-identifier";
        internal static readonly string KvStoreValue = "$(TeamIdentifierPrefix)$(CFBundleIdentifier)";
    }

    internal class PNEnt
    {
        internal static readonly string Key = "aps-environment";
        internal static readonly string DevValue = "development";
        internal static readonly string ProdValue = "production";
    }

    internal class WalletEnt
    {
        internal static readonly string Key = "com.apple.developer.pass-type-identifiers";
        internal static readonly string BaseValue = "$(TeamIdentifierPrefix)";
        internal static readonly string DefaultValue = "*";
    }

    internal class SiriEnt
    {
        internal static readonly string Key = "com.apple.developer.siri";
    }

    internal class AppPayEnt
    {
        internal static readonly string Key = "com.apple.developer.in-app-payments";
    }

    internal class VPNEnt
    {
        internal static readonly string Key = "com.apple.developer.networking.vpn.api";
        internal static readonly string Value = "allow-vpn";
    }

    internal class KeyChainEnt
    {
        internal static readonly string Key = "keychain-access-groups";
        internal static readonly string DefaultValue = "$(AppIdentifierPrefix)$(CFBundleIdentifier)";
    }

    internal class AudioEnt
    {
        internal static readonly string Key = "inter-app-audio";
    }

    internal class AssDomEnt
    {
        // value is an array of string of domains
        internal static readonly string Key = "com.apple.developer.associated-domains";
    }

    internal class GroupsEnt
    {
        // value is an array of string of groups
        internal static readonly string Key = "com.apple.security.application-groups";
    }

    internal class HomeEnt
    {
        // value is bool true.
        internal static readonly string Key = "com.apple.developer.homekit";
    }

    internal class ProtEnt
    {
        internal static readonly string Key = "com.apple.developer.default-data-protection";
        internal static readonly string Value = "NSFileProtectionComplete";
    }

    internal class HealthEnt
    {
        // value is bool true.
        internal static readonly string Key = "com.apple.developer.healthkit";
    }

    internal class WACEnt
    {
        // value is bool true.
        internal static readonly string Key = "com.apple.external-accessory.wireless-configuration";
    }

}