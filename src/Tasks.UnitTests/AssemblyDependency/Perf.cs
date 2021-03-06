using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.UnitTests.ResolveAssemblyReference_Tests.VersioningAndUnification.AutoUnify;
using Microsoft.Build.Utilities;
using Shouldly;
using Xunit;

namespace Microsoft.Build.UnitTests.ResolveAssemblyReference_Tests
{
    /// <summary>
    /// Test a few perf scenarios.
    /// </summary>
    public sealed class Perf : ResolveAssemblyReferenceTestFixture
    {
        [Theory]
        [Trait("Category", "mono-osx-failing")]
        [Trait("Category", "mono-windows-failing")]
        [InlineData(RARSimulationMode.LoadProject, 1)]
        [InlineData(RARSimulationMode.BuildProject, 2)]
        public void AutoUnifyUsesMinimumIO(RARSimulationMode rarSimulationMode, int ioThreshold)
        {
            // This WriteLine is a hack.  On a slow machine, the Tasks unittest fails because remoting
            // times out the object used for remoting console writes.  Adding a write in the middle of
            // keeps remoting from timing out the object.
            Console.WriteLine("Performing Perf.AutoUnifyUsesMinimumIO() test");

            StronglyNamedDependencyAutoUnify t = new StronglyNamedDependencyAutoUnify();

            try
            {
                // Manually instantiate a test fixture and run it.
                t.StartIOMonitoring();
                t.ExistsImpl(rarSimulationMode);
            }
            finally
            {
                t.StopIOMonitoringAndAssert_Minimal_IOUse(ioThreshold);
            }
        }

        [Fact]
        public void DependeeDirectoryIsProbedForDependency()
        {
            // This WriteLine is a hack.  On a slow machine, the Tasks unittest fails because remoting
            // times out the object used for remoting console writes.  Adding a write in the middle of
            // keeps remoting from timing out the object.
            Console.WriteLine("Performing Perf.DependeeDirectoryIsProbedForDependency() test");

            try
            {
                StartIOMonitoring();

                MockEngine engine = new MockEngine();

                ITaskItem[] assemblyNames =
                {
                    new TaskItem(@"C:\DependsOnNuget\A.dll"), // depends on N, version 1.0.0.0
                    new TaskItem(@"C:\NugetCache\N\lib\N.dll") // version 2.0.0.0
                };

                ResolveAssemblyReference t = new ResolveAssemblyReference();

                t.BuildEngine = engine;
                t.Assemblies = assemblyNames;
                t.SearchPaths = new[] { "{RawFileName}" };
                t.AutoUnify = true;

                bool succeeded = Execute(t);

                Assert.True(succeeded);
                ;
                uniqueFileExists[@"C:\DependsOnNuget\N.winmd"].ShouldBe(1);
                uniqueFileExists[@"C:\DependsOnNuget\N.dll"].ShouldBe(1);
                uniqueFileExists[@"C:\DependsOnNuget\N.exe"].ShouldBe(1);
            }
            finally
            {
                StopIOMonitoring();
            }
        }

        [Fact]
        public void DependeeDirectoryShouldNotBeProbedForDependencyWhenDependencyResolvedExternally()
        {
            // This WriteLine is a hack.  On a slow machine, the Tasks unittest fails because remoting
            // times out the object used for remoting console writes.  Adding a write in the middle of
            // keeps remoting from timing out the object.
            Console.WriteLine("Performing Perf.DependeeDirectoryIsProbedForDependency() test");

            try
            {
                StartIOMonitoring();

                MockEngine engine = new MockEngine();

                ITaskItem[] assemblyNames =
                {
                    new TaskItem(@"C:\DependsOnNuget\A.dll"), // depends on N, version 1.0.0.0
                    new TaskItem(@"C:\NugetCache\N\lib\N.dll", // version 2.0.0.0
                        new Dictionary<string, string>
                        {
                            {"ExternallyResolved", "true"}
                        }) 
                };

                ResolveAssemblyReference t = new ResolveAssemblyReference();

                t.BuildEngine = engine;
                t.Assemblies = assemblyNames;
                t.SearchPaths = new[] { "{RawFileName}" };
                t.AutoUnify = true;

                bool succeeded = Execute(t);

                Assert.True(succeeded);

                uniqueFileExists.ShouldNotContainKey(@"C:\DependsOnNuget\N.winmd");
                uniqueFileExists.ShouldNotContainKey(@"C:\DependsOnNuget\N.dll");
                uniqueFileExists.ShouldNotContainKey(@"C:\DependsOnNuget\N.exe");
            }
            finally
            {
                StopIOMonitoring();
            }
        }
    }
}
