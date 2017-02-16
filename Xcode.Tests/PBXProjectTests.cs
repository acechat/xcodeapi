using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Unity.PureCSharpTests.iOSExtensions
{
    public class LinearGuidGenerator
    {
        private static int counter = 0;
        
        public static void Reset()
        {
            counter = 0;
        }
        public static string Generate()
        {
            counter = counter + 1;
            return "CCCCCCCC00000000" + counter.ToString("D8");
        }
    }

    /*  It's not possible to test separate components of PBXProject in isolation easily --
        the data stored there has many cross dependencies. We are doing sort-of integration
        testing, thus test data is large. To simplify testing, pbx projects are stored
        in files -- one for source and and one for expected output data. The tests read a
        source file, write a modified version to a temporary test directory and compare to
        the expected output.
        
        Since the changes between source and output files are quite small compared to the
        total quantity of data, it's best to use a diff tool when debugging failures or
        verifying changes.
    */
    [TestFixture]
    public class PBXProjectTests : GenericTester
    {
        public PBXProjectTests() : base("PBXProjectTestFiles", "PBXProjectTestOutput", false /*true for debug*/)
        {
        }
        
        private static void ResetGuidGenerator()
        {
            UnityEditor.iOS.Xcode.PBX.PBXGUID.SetGuidGenerator(LinearGuidGenerator.Generate);
            LinearGuidGenerator.Reset();
        }

        private void TestOutput(PBXProject proj, string testFilename)
        {
            string sourceFile = Path.Combine(GetTestSourcePath(), testFilename);
            string outputFile = Path.Combine(GetTestOutputPath(), testFilename);

            proj.WriteToFile(outputFile);
            Assert.IsTrue(TestUtils.FileContentsEqual(outputFile, sourceFile),
                          "Output not equivalent to the expected output");

            PBXProject other = new PBXProject();
            other.ReadFromFile(outputFile);
            other.WriteToFile(outputFile);
            Assert.IsTrue(TestUtils.FileContentsEqual(outputFile, sourceFile));
            if (!DebugEnabled())
                Directory.Delete(GetTestOutputPath(), recursive:true);
        }
        
        private PBXProject ReadPBXProject()
        {
            return ReadPBXProject("base.pbxproj");
        }

        private PBXProject ReadPBXProject(string project)
        {
            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(Path.Combine(GetTestSourcePath(), project)));
            return proj;
        }

        private static PBXProject Reserialize(PBXProject proj)
        {
            string contents = proj.WriteToString();
            proj = new PBXProject();
            proj.ReadFromString(contents);
            return proj;
        }

        [Test]
        public void BuildOptionsWork1()
        {
            ResetGuidGenerator();

            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            string ptarget = proj.ProjectGuid();

            // check that target selection works when setting options
            proj.SetBuildProperty(ptarget, "TEST_PROJ", "projtest");
            proj.SetBuildProperty(target, "TEST", "testdata1");

            // check quoting in various special cases
            proj.AddBuildProperty(target, "TEST_ADD", "testdata2");
            proj.AddBuildProperty(target, "TEST[quoted]", "testdata3");
            proj.AddBuildProperty(target, "TEST quoted", "testdata4");
            proj.AddBuildProperty(target, "TEST//quoted", "testdata4");
            proj.AddBuildProperty(target, "TEST/*quoted", "testdata4");
            proj.AddBuildProperty(target, "TEST*/quoted", "testdata4");

            // check how LIBRARY_SEARCH_PATHS option is quoted
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "test");
            TestOutput(proj, "conf_append1.pbxproj");
        }

        [Test]
        public void BuildOptionsWork2()
        {
            ResetGuidGenerator();
    
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            // check that we can append multiple options
            proj.AddBuildProperty(target, "TEST_ADD", "test2");
            proj.AddBuildProperty(target, "TEST_ADD", "test2");
            proj.AddBuildProperty(target, "TEST_ADD", "test3");

            // check SetBuildProperty when multiple values already exist
            proj.AddBuildProperty(target, "TEST_SET", "test2");        
            proj.AddBuildProperty(target, "TEST_SET", "test3");
            proj.SetBuildProperty(target, "TEST_SET", "test4");

            // check that option removal works
            proj.AddBuildProperty(target, "TEST_REMOVE", "test1");
            proj.AddBuildProperty(target, "TEST_REMOVE", "test2");
            proj.AddBuildProperty(target, "TEST_REMOVE2", "value");
            proj = Reserialize(proj);
            proj.UpdateBuildProperty(target, "TEST_REMOVE", null, new string[]{"test2"});
            proj.RemoveBuildProperty(target, "TEST_REMOVE2");

            // check quoting for LIBRARY_SEARCH_PATHS
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "test test");
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "\"test test\"");
            TestOutput(proj, "conf_append2.pbxproj");
        }

        [Test]
        public void BuildOptionsWork3()
        {
            ResetGuidGenerator();

            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            // check how various operations work for LIBRARY_SEARCH_PATHS as we have 
            // special logic for this key
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "test test");
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "\"test test\"");
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "test test2");
            proj.AddBuildProperty(target, "LIBRARY_SEARCH_PATHS", "test test3");
            proj.UpdateBuildProperty(target, "LIBRARY_SEARCH_PATHS", null, new string[]{"test test2"});
            proj = Reserialize(proj);
            proj.UpdateBuildProperty(target, "LIBRARY_SEARCH_PATHS", new string[]{"test test3"}, new string[]{"test test2"}); // tests whether "test test2" is correctly removed
            TestOutput(proj, "conf_append3.pbxproj");
        }

        [Test]
        public void DuplicateOptionHandlingWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject("base_dup.pbxproj");
            
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.UpdateBuildProperty(target, "TEST_DUP", new string[]{"test2"}, new string[]{"test3"});
            proj.UpdateBuildProperty(target, "TEST_DUP2", null, new string[]{"test_key"}); // duplicate value removal
            
            TestOutput(proj, "dup1.pbxproj");
        }

        [Test]
        public void AddSingleSourceFileWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddFileToBuild(target, proj.AddFile("relative/path1.cc", "Libraries/path1.cc", PBXSourceTree.Source));
            TestOutput(proj, "add_file1.pbxproj");
        }

        [Test]
        public void AddMultipleSourceFilesWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            // check addition of relative path
            proj.AddFileToBuild(target, proj.AddFile("relative/path1.cc", "Classes/some/path/path1.cc", PBXSourceTree.Source));
            // check addition of absolute path
            proj.AddFileToBuild(target, proj.AddFile("/absolute/path/abs1.cc", "Classes/some/path/abs1.cc", PBXSourceTree.Source));
            proj.AddFileToBuild(target, proj.AddFile("/absolute/path/abs2.cc", "Classes/some/abs2.cc", PBXSourceTree.Source));
            // check addition of files with unknown extensions
            proj.AddFileToBuild(target, proj.AddFile("relative/path1.unknown_ext", "Classes/some/path/path1.unknown_ext", PBXSourceTree.Source));
            // check whether folder references work
            proj.AddFileToBuild(target, proj.AddFolderReference("relative/path2", "Classes/some/path/path2", PBXSourceTree.Source));
            // check whether we correctly add folder references with weird extensions to resources
            proj.AddFileToBuild(target, proj.AddFolderReference("relative/path3.cc", "Classes/some/path/path3.cc", PBXSourceTree.Source));

            Assert.IsTrue(proj.FindFileGuidByRealPath("relative/path1.cc") == "CCCCCCCC0000000000000001");
            Assert.IsTrue(proj.FindFileGuidByRealPath("/absolute/path/abs1.cc") == "CCCCCCCC0000000000000005");
            Assert.IsTrue(proj.FindFileGuidByProjectPath("Classes/some/path/abs1.cc") == "CCCCCCCC0000000000000005");
            Assert.AreEqual(1, proj.GetGroupChildrenFiles("Classes/some").Count);
            TestOutput(proj, "add_file2.pbxproj");
        }

        [Test]
        public void AddSourceFileWithFlagsWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            // check if duplicate add is ignored (we don't lose flags)
            proj.AddFileToBuildWithFlags(target, proj.AddFile("relative/path1.cc", "Classes/path1.cc", PBXSourceTree.Source),
                                         "-Wno-newline");
            proj.AddFileToBuild(target, proj.AddFile("relative/path1.cc", "Classes/path1.cc"));
            
            // check if we can add flags to an existing file and remove them
            proj.AddFileToBuild(target, proj.AddFile("relative/path2.cc", "Classes/path2.cc", PBXSourceTree.Source));
            proj.AddFileToBuildWithFlags(target, proj.AddFile("relative/path3.cc", "Classes/path3.cc", PBXSourceTree.Source), 
                                         "-Wno-newline");
            
            proj = Reserialize(proj);
            target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.SetCompileFlagsForFile(target, proj.FindFileGuidByProjectPath("Classes/path2.cc"),
                                        new List<string>{ "-Wno-newline", "-O3" });
            proj.SetCompileFlagsForFile(target, proj.FindFileGuidByProjectPath("Classes/path3.cc"), null);
            TestOutput(proj, "add_file3.pbxproj");
        }

        [Test]
        public void AddFrameworkWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            // check whether we can add framework reference
            proj.AddFrameworkToProject(target, "Twitter.framework", true);
            proj.AddFrameworkToProject(target, "Foundation.framework", false);
            // check whether we can remove framework reference
            proj.AddFrameworkToProject(target, "GameCenter.framework", false);
            proj = Reserialize(proj);
            proj.RemoveFrameworkFromProject(target, "GameCenter.framework");
            TestOutput(proj, "add_framework1.pbxproj");
        }

        [Test]
        public void RemoveFileWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

            proj.AddFileToBuild(target, proj.AddFile("relative/path1.cc", "Classes/path1.cc", PBXSourceTree.Source));
            proj = Reserialize(proj);
            proj.RemoveFile(proj.FindFileGuidByRealPath("relative/path1.cc"));
            proj.RemoveFile(proj.FindFileGuidByProjectPath("Classes/file"));
            TestOutput(proj, "remove_file1.pbxproj");
        }

        [Test]
        public void AssetTagModificationWorks()
        {
            PBXProject proj;
            string target;

            ResetGuidGenerator();
            proj = ReadPBXProject();
            target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            var f1 = proj.AddFile("Data/data1.dat", "Data/data1.dat", PBXSourceTree.Source);
            var f2 = proj.AddFile("Data/data2.dat", "Data/data2.dat", PBXSourceTree.Source);
            var f3 = proj.AddFile("Data/data3.dat", "Data/data3.dat", PBXSourceTree.Source);
            proj.AddFileToBuild(target, f1);
            proj.AddFileToBuild(target, f2);
            proj.AddFileToBuild(target, f3);
            proj.AddAssetTagForFile(target, f1, "test_tag1");
            proj.AddAssetTagForFile(target, f2, "test_tag1");
            proj.AddAssetTagForFile(target, f2, "test_tag2");
            proj.AddAssetTagForFile(target, f2, "test_tag3");
            proj.AddAssetTagToDefaultInstall(target, "test_tag2");
            proj.AddAssetTagToDefaultInstall(target, "test_tag3");
            proj.AddAssetTagForFile(target, f2, "test_tag_remove1");
            proj.AddAssetTagForFile(target, f3, "test_tag_remove2");
            proj.AddAssetTagToDefaultInstall(target, "test_tag_remove2");
            proj = Reserialize(proj);
            proj.RemoveAssetTagForFile(target, f2, "test_tag_remove1");
            proj.RemoveAssetTag("test_tag_remove2");
            proj.RemoveAssetTagFromDefaultInstall(target, "test_tag3");
            TestOutput(proj, "asset_tags1.pbxproj");
        }

        [Test]
        public void AddExternalReferenceWorks()
        {
            PBXProject proj;
            string target;
            
            ResetGuidGenerator();
            proj = ReadPBXProject();
            target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddExternalProjectDependency("UnityDevProject/UnityDevProject.xcodeproj", "UnityDevProject.xcodeproj",
                                              PBXSourceTree.Source);
            TestOutput(proj, "add_external_ref1.pbxproj");
            
            ResetGuidGenerator();
            proj = ReadPBXProject();
            target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddExternalProjectDependency("UnityDevProject/UnityDevProject.xcodeproj", "UnityDevProject.xcodeproj",
                                              PBXSourceTree.Source);
            proj.AddExternalProjectDependency("UnityDevProject/UnityDevProject2.xcodeproj", "UnityDevProject2.xcodeproj",
                                              PBXSourceTree.Source);
            proj.AddExternalLibraryDependency(target, "UnityDevProject.a", "AA88A7D019101316001E7AB7",
                                              "UnityDevProject/UnityDevProject.xcodeproj", "UnityDevProject");
            TestOutput(proj, "add_external_ref2.pbxproj");
        }

        [Test]
        public void RemoveFilesRecursiveWorks()
        {
            PBXProject proj;
            string target;
            
            ResetGuidGenerator();
            proj = ReadPBXProject();
            target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddFileToBuild(target, proj.AddFile("relative/path1.cc", "Classes/path/path1.cc", PBXSourceTree.Source));
            proj.AddFileToBuild(target, proj.AddFile("/absolute/path/abs1.cc", "Classes/path/path2.cc", PBXSourceTree.Source));
            proj.AddFileToBuild(target, proj.AddFile("/absolute/path/abs2.cc", "Classes/path/path3.cc", PBXSourceTree.Source));
            proj.AddFileToBuild(target, proj.AddFile("/absolute/path/abs3.cc", "Classes/path/path2/path4.cc", PBXSourceTree.Source));
            proj = Reserialize(proj);
            proj.RemoveFilesByProjectPathRecursive("Classes/path");
            TestOutput(proj, "rm_recursive1.pbxproj");
        }

        [Test]
        public void AddTargetWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            string newTarget = proj.AddTarget("TestTarget", ".dylib", "test.type");
            proj.AddBuildConfigForTarget(newTarget, "CustomConfig1");
            proj.AddBuildConfigForTarget(newTarget, "CustomConfig2");
            proj.AddTargetDependency(target, newTarget);
            TestOutput(proj, "add_target1.pbxproj");
        }

        [Test]
        public void AddBuildPhasesWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.AddTarget("TestTarget", ".dylib", "test.type");
            proj.AddSourcesBuildPhase(target);
            proj.AddResourcesBuildPhase(target);
            proj.AddFrameworksBuildPhase(target);
            proj.AddCopyFilesBuildPhase(target, "Copy resources", "$(DST_PATH)", "13");
            TestOutput(proj, "add_build_phases1.pbxproj");
        }

        [Test]
        public void AddWatchExtensionWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddWatchExtension(target, "Watchtest Extension", "com.company.product.watchapp.watchextension", "Watchtest Extension/Info.plist");
            TestOutput(proj, "add_watch_extension.pbxproj");
        }

        [Test]
        public void AddWatchAppAndExtensionWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            string extTargetGuid = proj.AddWatchExtension(target, "watchtest Extension", "com.company.product.watchapp.watchextension", "watchtest Extension/Info.plist");
            proj.AddWatchApp(target, extTargetGuid, "watchtest", "com.company.product.watchapp", "watchtest/Info.plist");
            TestOutput(proj, "add_watch_app_and_extension.pbxproj");
        }

        [Test]
        public void StrippedProjectReadingWorks()
        {
            PBXProject proj = ReadPBXProject("base_stripped.pbxproj");
            TestOutput(proj, "stripped1.pbxproj");
        }
        
        [Test]
        public void UnknownFileTypesWork()
        {
            PBXProject proj = ReadPBXProject("base_unknown.pbxproj");
            TestOutput(proj, "unknown1.pbxproj");
        }
        
        [Test]
        public void InvalidProjectRepairWorks()
        {
            PBXProject proj = ReadPBXProject("base_repair.pbxproj");
            TestOutput(proj, "repair1.pbxproj");
        }

        [Test]
        public void AddCapabilityWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddCapability(target, PBXCapabilityType.GameCenter);
            TestOutput(proj, "add_capability.pbxproj");
        }

        [Test]
        public void AddCapabilityWithEntitlementWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddCapability(target, PBXCapabilityType.iCloud, Path.Combine(GetTestSourcePath(), "test.entitlements"));
            TestOutput(proj, "add_capability_entitlement.pbxproj");
        }

        [Test]
        public void AddMultipleCapabilitiesWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddCapability(target, PBXCapabilityType.GameCenter);
            proj.AddCapability(target, PBXCapabilityType.InAppPurchase);
            proj.AddCapability(target, PBXCapabilityType.Maps);
            TestOutput(proj, "add_multiple_capabilities.pbxproj");
        }

        [Test]
        public void AddMultipleCapabilitiesWithEntitlementWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.AddCapability(target, PBXCapabilityType.iCloud, Path.Combine(GetTestSourcePath(), "test.entitlements"));
            proj.AddCapability(target, PBXCapabilityType.ApplePay, Path.Combine(GetTestSourcePath(), "test.entitlements"));
            proj.AddCapability(target, PBXCapabilityType.Siri, Path.Combine(GetTestSourcePath(), "test.entitlements"));
            TestOutput(proj, "add_multiple_capabilities_entitlement.pbxproj");
        }

        [Test]
        public void SetTeamIdWorks()
        {
            ResetGuidGenerator();
            PBXProject proj = ReadPBXProject();
            string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            proj.SetTeamId(target, "Z6SFPV59E3");
            TestOutput(proj, "set_teamid.pbxproj");
        }
    }
}
