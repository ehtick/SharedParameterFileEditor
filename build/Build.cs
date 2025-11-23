using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode


    readonly AbsolutePath OutputDirectory = RootDirectory / "output";
    readonly AbsolutePath SourceDirectory = RootDirectory / "source";

    readonly string[] CompiledAssemblies = { "SharedParameterFileEditor.exe", "SharedParametersDefinitionFile.dll" };

    [GitRepository]
    [Required]
    readonly GitRepository GitRepository;

    [Solution(GenerateProjects = true)]
    Solution Solution;

    public static int Main () => Execute<Build>(x => x.Compile);

    //[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    //readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;


}
