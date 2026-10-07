using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

public partial class Build
{
    // only the code generator libraries are published as NuGet packages
    static readonly string[] PackableProjects =
    [
        "NSwag.Core",
        "NSwag.CodeGeneration",
        "NSwag.CodeGeneration.CSharp",
        "NSwag.CodeGeneration.TypeScript",
    ];

    Target Pack => _ => _
        .DependsOn(Compile)
        .After(Test)
        // packages are identical on every platform, so only produce them once
        .OnlyWhenDynamic(() => IsRunningOnWindows)
        .Executes(() =>
        {
            if (Configuration != Configuration.Release)
            {
                throw new InvalidOperationException("Cannot pack if compilation hasn't been done in Release mode, use --configuration Release");
            }

            var nugetVersion = VersionPrefix;
            if (!string.IsNullOrWhiteSpace(VersionSuffix))
            {
                nugetVersion += "-" + VersionSuffix;
            }

            foreach (var project in PackableProjects.Select(GetProject))
            {
                DotNetPack(s => s
                    .SetProcessWorkingDirectory(SourceDirectory)
                    .SetProject(project)
                    .SetAssemblyVersion(VersionPrefix)
                    .SetFileVersion(VersionPrefix)
                    .SetInformationalVersion(VersionPrefix)
                    .SetVersion(nugetVersion)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoRestore()
                    .EnableNoBuild()
                );
            }
        });
}
