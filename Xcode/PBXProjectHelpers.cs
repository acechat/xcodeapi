using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System;
using UnityEditor.iOS.Xcode.PBX;

namespace UnityEditor.iOS.Xcode
{
    /*  This class implements a number of static methods for performing common tasks
        on xcode projects. 
        TODO: Make sure enough stuff is exposed so that it's possible to perform the tasks
        without using internal APIs
    */
    public class PBXProjectHelpers
    {
        // Create a wrapper class so that collection initializers work and we can have a 
        // compact notation. Note that we can't use Dictionary because the keys may be duplicate
        internal class FlagList : List<KeyValuePair<string, string>>
        {
            public void Add(string flag, string value)
            {
                Add(new KeyValuePair<string, string>(flag, value));
            }
        }

        internal static FlagList appExtensionReleaseBuildFlags = new FlagList
        {
            { "ALWAYS_SEARCH_USER_PATHS", "NO" },
            { "CLANG_CXX_LANGUAGE_STANDARD", "gnu++0x" },
            { "CLANG_CXX_LIBRARY", "libc++" },
            { "CLANG_ENABLE_MODULES", "YES" },
            { "CLANG_ENABLE_OBJC_ARC", "YES" },
            { "CLANG_WARN_BOOL_CONVERSION", "YES" },
            { "CLANG_WARN_CONSTANT_CONVERSION", "YES" },
            { "CLANG_WARN_DIRECT_OBJC_ISA_USAGE", "YES_ERROR" },
            { "CLANG_WARN_EMPTY_BODY", "YES" },
            { "CLANG_WARN_ENUM_CONVERSION", "YES" },
            { "CLANG_WARN_INT_CONVERSION", "YES" },
            { "CLANG_WARN_OBJC_ROOT_CLASS", "YES_ERROR" },
            { "CLANG_WARN_UNREACHABLE_CODE", "YES" },
            { "CLANG_WARN__DUPLICATE_METHOD_MATCH", "YES" },
            { "COPY_PHASE_STRIP", "YES" },
            { "ENABLE_NS_ASSERTIONS", "NO" },
            { "ENABLE_STRICT_OBJC_MSGSEND", "YES" },
            { "GCC_C_LANGUAGE_STANDARD", "gnu99" },
            { "GCC_WARN_64_TO_32_BIT_CONVERSION", "YES" },
            { "GCC_WARN_ABOUT_RETURN_TYPE", "YES_ERROR" },
            { "GCC_WARN_UNDECLARED_SELECTOR", "YES" },
            { "GCC_WARN_UNINITIALIZED_AUTOS", "YES_AGGRESSIVE" },
            { "GCC_WARN_UNUSED_FUNCTION", "YES" },
            //{ "INFOPLIST_FILE", <path/to/info.plist> },
            { "IPHONEOS_DEPLOYMENT_TARGET", "8.0" },
            { "LD_RUNPATH_SEARCH_PATHS", "$(inherited)" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/../../Frameworks" },
            { "MTL_ENABLE_DEBUG_INFO", "NO" },
            { "PRODUCT_NAME", "$(TARGET_NAME)" },
            { "SKIP_INSTALL", "YES" },
            { "VALIDATE_PRODUCT", "YES" }
        };

        internal static FlagList appExtensionDebugBuildFlags = new FlagList
        {
            { "ALWAYS_SEARCH_USER_PATHS", "NO" },
            { "CLANG_CXX_LANGUAGE_STANDARD", "gnu++0x" },
            { "CLANG_CXX_LIBRARY", "libc++" },
            { "CLANG_ENABLE_MODULES", "YES" },
            { "CLANG_ENABLE_OBJC_ARC", "YES" },
            { "CLANG_WARN_BOOL_CONVERSION", "YES" },
            { "CLANG_WARN_CONSTANT_CONVERSION", "YES" },
            { "CLANG_WARN_DIRECT_OBJC_ISA_USAGE", "YES_ERROR" },
            { "CLANG_WARN_EMPTY_BODY", "YES" },
            { "CLANG_WARN_ENUM_CONVERSION", "YES" },
            { "CLANG_WARN_INT_CONVERSION", "YES" },
            { "CLANG_WARN_OBJC_ROOT_CLASS", "YES_ERROR" },
            { "CLANG_WARN_UNREACHABLE_CODE", "YES" },
            { "CLANG_WARN__DUPLICATE_METHOD_MATCH", "YES" },
            { "COPY_PHASE_STRIP", "NO" },
            { "ENABLE_STRICT_OBJC_MSGSEND", "YES" },
            { "GCC_C_LANGUAGE_STANDARD", "gnu99" },
            { "GCC_DYNAMIC_NO_PIC", "NO" },
            { "GCC_OPTIMIZATION_LEVEL", "0" },
            { "GCC_PREPROCESSOR_DEFINITIONS", "DEBUG=1" },
            { "GCC_PREPROCESSOR_DEFINITIONS", "$(inherited)" },
            { "GCC_SYMBOLS_PRIVATE_EXTERN", "NO" },
            { "GCC_WARN_64_TO_32_BIT_CONVERSION", "YES" },
            { "GCC_WARN_ABOUT_RETURN_TYPE", "YES_ERROR" },
            { "GCC_WARN_UNDECLARED_SELECTOR", "YES" },
            { "GCC_WARN_UNINITIALIZED_AUTOS", "YES_AGGRESSIVE" },
            { "GCC_WARN_UNUSED_FUNCTION", "YES" },
            // { "INFOPLIST_FILE", <path/to/info.plist> },
            { "IPHONEOS_DEPLOYMENT_TARGET", "8.0" },
            { "LD_RUNPATH_SEARCH_PATHS", "$(inherited)" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/../../Frameworks" },
            { "MTL_ENABLE_DEBUG_INFO", "YES" },
            { "ONLY_ACTIVE_ARCH", "YES" },
            { "PRODUCT_NAME", "$(TARGET_NAME)" },
            { "SKIP_INSTALL", "YES" },
        };

        internal static FlagList watchExtensionReleaseBuildFlags = new FlagList
        {
            { "ASSETCATALOG_COMPILER_COMPLICATION_NAME", "Complication" },
            { "CLANG_ANALYZER_NONNULL", "YES" },
            { "CLANG_WARN_DOCUMENTATION_COMMENTS", "YES" },
            { "CLANG_WARN_INFINITE_RECURSION", "YES" },
            { "CLANG_WARN_SUSPICIOUS_MOVE", "YES" },
            { "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" },
            { "GCC_NO_COMMON_BLOCKS", "YES" },
            //{ "INFOPLIST_FILE", "<path/to/Info.plist>" },
            { "LD_RUNPATH_SEARCH_PATHS", "$(inherited)" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/../../Frameworks" },
            { "PRODUCT_BUNDLE_IDENTIFIER", "com.unity3d.ios.watchkitapp.watchkitextension" },
            { "PRODUCT_NAME", "${TARGET_NAME}" },
            { "SDKROOT", "watchos" },
            { "SKIP_INSTALL", "YES" },
            { "TARGETED_DEVICE_FAMILY", "4" },
            { "WATCHOS_DEPLOYMENT_TARGET", "3.1" },
        };

        internal static FlagList watchExtensionDebugBuildFlags = new FlagList
        {
            { "ASSETCATALOG_COMPILER_COMPLICATION_NAME", "Complication" },
            { "CLANG_ANALYZER_NONNULL", "YES" },
            { "CLANG_WARN_DOCUMENTATION_COMMENTS", "YES" },
            { "CLANG_WARN_INFINITE_RECURSION", "YES" },
            { "CLANG_WARN_SUSPICIOUS_MOVE", "YES" },
            { "DEBUG_INFORMATION_FORMAT", "dwarf" },
            { "ENABLE_TESTABILITY", "YES" },
            { "GCC_NO_COMMON_BLOCKS", "YES" },
            // { "INFOPLIST_FILE", "<path/to/Info.plist>" },
            { "LD_RUNPATH_SEARCH_PATHS", "$(inherited)" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks" },
            { "LD_RUNPATH_SEARCH_PATHS", "@executable_path/../../Frameworks" },
            { "PRODUCT_BUNDLE_IDENTIFIER", "com.unity3d.ios.watchkitapp.watchkitextension" },
            { "PRODUCT_NAME", "${TARGET_NAME}" },
            { "SDKROOT", "watchos" },
            { "SKIP_INSTALL", "YES" },
            { "TARGETED_DEVICE_FAMILY", "4" },
            { "WATCHOS_DEPLOYMENT_TARGET", "3.1" },
        };

        internal static FlagList watchAppReleaseBuildFlags = new FlagList
        {
            { "ASSETCATALOG_COMPILER_APPICON_NAME", "AppIcon" },
            { "CLANG_ANALYZER_NONNULL", "YES" },
            { "CLANG_WARN_DOCUMENTATION_COMMENTS", "YES" },
            { "CLANG_WARN_INFINITE_RECURSION", "YES" },
            { "CLANG_WARN_SUSPICIOUS_MOVE", "YES" },
            { "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" },
            { "GCC_NO_COMMON_BLOCKS", "YES" },
            //{ "IBSC_MODULE", "the extension target name with ' ' replaced with '_'" },
            //{ "INFOPLIST_FILE", "<path/to/Info.plist>" },
            //{ "PRODUCT_BUNDLE_IDENTIFIER", "<bundle id>" },
            { "PRODUCT_NAME", "$(TARGET_NAME)" },
            { "SDKROOT", "watchos" },
            { "SKIP_INSTALL", "YES" },
            { "TARGETED_DEVICE_FAMILY", "4" },
            { "WATCHOS_DEPLOYMENT_TARGET", "3.1" },
        };

        internal static FlagList watchAppDebugBuildFlags = new FlagList
        {
            { "ASSETCATALOG_COMPILER_APPICON_NAME", "AppIcon" },
            { "CLANG_ANALYZER_NONNULL", "YES" },
            { "CLANG_WARN_DOCUMENTATION_COMMENTS", "YES" },
            { "CLANG_WARN_INFINITE_RECURSION", "YES" },
            { "CLANG_WARN_SUSPICIOUS_MOVE", "YES" },
            { "DEBUG_INFORMATION_FORMAT", "dwarf" },
            { "ENABLE_TESTABILITY", "YES" },
            { "GCC_NO_COMMON_BLOCKS", "YES" },
            //{ "IBSC_MODULE", "the extension target name with ' ' replaced with '_'" },
            //{ "INFOPLIST_FILE", "<path/to/Info.plist>" },
            //{ "PRODUCT_BUNDLE_IDENTIFIER", "<bundle id>" },
            { "PRODUCT_NAME", "$(TARGET_NAME)" },
            { "SDKROOT", "watchos" },
            { "SKIP_INSTALL", "YES" },
            { "TARGETED_DEVICE_FAMILY", "4" },
            { "WATCHOS_DEPLOYMENT_TARGET", "3.1" },
        };

        static void SetBuildFlagsFromDict(PBXProject proj, string configGuid, IEnumerable<KeyValuePair<string, string>> data)
        {
            foreach (var kv in data)
                proj.AddBuildPropertyForConfig(configGuid, kv.Key, kv.Value);
        }

        public static void SetDefaultAppExtensionReleaseBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, appExtensionReleaseBuildFlags);
        }

        public static void SetDefaultAppExtensionDebugBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, appExtensionDebugBuildFlags);
        }

        public static void SetDefaultWatchExtensionReleaseBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, watchExtensionReleaseBuildFlags);
        }

        public static void SetDefaultWatchExtensionDebugBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, watchExtensionDebugBuildFlags);
        }

        public static void SetDefaultWatchAppReleaseBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, watchAppReleaseBuildFlags);
        }

        public static void SetDefaultWatchAppDebugBuildFlags(PBXProject proj, string configGuid)
        {
            SetBuildFlagsFromDict(proj, configGuid, watchAppDebugBuildFlags);
        }

        // Returns the guid of the new target
        public static string AddAppExtension(PBXProject proj, string mainTarget, string name, string infoPlistPath)
        {
            string ext = ".appex";
            var newTargetGuid = proj.AddTarget(name, ext, "com.apple.product-type.app-extension");

            var debugConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Debug");
            var releaseConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Release");

            SetDefaultAppExtensionDebugBuildFlags(proj, debugConfigGuid);
            SetDefaultAppExtensionReleaseBuildFlags(proj, releaseConfigGuid);

            var configs = new string[] { debugConfigGuid, releaseConfigGuid };
            proj.SetBuildPropertyForConfig(configs, "INFOPLIST_FILE", infoPlistPath);

            proj.AddSourcesBuildPhase(newTargetGuid);
            proj.AddResourcesBuildPhase(newTargetGuid);
            proj.AddFrameworksBuildPhase(newTargetGuid);
            string copyFilesPhaseGuid = proj.AddCopyFilesBuildPhase(mainTarget, "Embed App Extensions", "", "13");
            proj.AddFileToBuildSection(mainTarget, copyFilesPhaseGuid, proj.GetTargetProductFileRef(newTargetGuid));

            proj.AddTargetDependency(mainTarget, newTargetGuid);

            return newTargetGuid;
        }

        // Returns the guid of the new target
        public static string AddWatchApp(PBXProject proj, string mainTargetGuid, string watchExtensionTargetGuid, 
                                         string name, string bundleId, string infoPlistPath)
        {
            var newTargetGuid = proj.AddTarget(name, ".app", "com.apple.product-type.application.watchapp2");

            var debugConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Debug");
            var releaseConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Release");

            SetDefaultWatchAppDebugBuildFlags(proj, debugConfigGuid);
            SetDefaultWatchAppReleaseBuildFlags(proj, releaseConfigGuid);

            var isbcModuleName = proj.nativeTargets[watchExtensionTargetGuid].name.Replace(" ", "_");

            var configs = new string[] { debugConfigGuid, releaseConfigGuid };
            proj.SetBuildPropertyForConfig(configs, "PRODUCT_BUNDLE_IDENTIFIER", bundleId);
            proj.SetBuildPropertyForConfig(configs, "INFOPLIST_FILE", infoPlistPath);
            proj.SetBuildPropertyForConfig(configs, "IBSC_MODULE", isbcModuleName);

            proj.AddResourcesBuildPhase(newTargetGuid);
            string copyFilesGuid = proj.AddCopyFilesBuildPhase(newTargetGuid, "Embed App Extensions", "", "13");
            proj.AddFileToBuildSection(newTargetGuid, copyFilesGuid, proj.GetTargetProductFileRef(watchExtensionTargetGuid));

            string copyWatchFilesGuid = proj.AddCopyFilesBuildPhase(mainTargetGuid, "Embed Watch Content", "$(CONTENTS_FOLDER_PATH)/Watch", "16");
            proj.AddFileToBuildSection(mainTargetGuid, copyWatchFilesGuid, proj.GetTargetProductFileRef(newTargetGuid));

            proj.AddTargetDependency(newTargetGuid, watchExtensionTargetGuid);
            proj.AddTargetDependency(mainTargetGuid, newTargetGuid);

            return newTargetGuid;
        }

        public static string AddWatchExtension(PBXProject proj, string mainTarget, string name, string infoPlistPath)
        {
            var newTargetGuid = proj.AddTarget(name, ".appex", "com.apple.product-type.watchkit2-extension");

            var debugConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Debug");
            var releaseConfigGuid = proj.AddBuildConfigForTarget(newTargetGuid, "Release");

            SetDefaultWatchExtensionDebugBuildFlags(proj, debugConfigGuid);
            SetDefaultWatchExtensionReleaseBuildFlags(proj, releaseConfigGuid);

            var configs = new string[] { debugConfigGuid, releaseConfigGuid };
            proj.SetBuildPropertyForConfig(configs, "INFOPLIST_FILE", infoPlistPath);

            proj.AddSourcesBuildPhase(newTargetGuid);
            proj.AddResourcesBuildPhase(newTargetGuid);
            proj.AddFrameworksBuildPhase(newTargetGuid);

            return newTargetGuid;
        }

        
    }
} // namespace UnityEditor.iOS.Xcode
