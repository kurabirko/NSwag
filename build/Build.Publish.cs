using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

public partial class Build
{
    string NuGetSource => "https://api.nuget.org/v3/index.json";
    [Parameter] [Secret] string NuGetApiKey;

    string MyGetGetSource => "https://www.myget.org/F/nswag/api/v2/package";
    [Parameter] [Secret] string MyGetApiKey;

    string ApiKeyToUse => IsTaggedBuild ? NuGetApiKey : MyGetApiKey;
    string SourceToUse => IsTaggedBuild ? NuGetSource : MyGetGetSource;

    Target Publish => _ => _
        .OnlyWhenDynamic(() => IsRunningOnWindows && (GitRepository.IsOnMainOrMasterBranch() || IsTaggedBuild) && GitRepository.GetGitHubOwner() == "RicoSuter")
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey, () => MyGetApiKey)
        .Executes(() =>
        {
            try
            {
                DotNetNuGetPush(_ => _
                        .Apply(PushSettingsBase)
                        .Apply(PushSettings)
                        .CombineWith(PushPackageFiles, (_, v) => _
                            .SetTargetPath(v))
                        .Apply(PackagePushSettings),
                    PushDegreeOfParallelism,
                    PushCompleteOnFailure);
            }
            catch (Exception e)
            {
                if (IsTaggedBuild)
                {
                    // fatal
                    throw;
                }

                Serilog.Log.Error("Could not push: {Message}", e.Message);
            }

        });

    Configure<DotNetNuGetPushSettings> PushSettingsBase => _ => _
        .SetSource(SourceToUse)
        .SetApiKey(ApiKeyToUse)
        .SetSkipDuplicate(true);

    Configure<DotNetNuGetPushSettings> PushSettings => _ => _;
    Configure<DotNetNuGetPushSettings> PackagePushSettings => _ => _;

    IEnumerable<AbsolutePath> PushPackageFiles =>
        ArtifactsDirectory.GlobFiles("*.nupkg");

    bool PushCompleteOnFailure => true;

    int PushDegreeOfParallelism => 1;
}