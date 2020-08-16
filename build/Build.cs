using System;
using System.IO;
using System.Linq;
//using BIT.Data;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);
    //HACK this is the path that will be use if execute the scripts build.X from the src director
    private const string BuildProps = "../Directory.Build.props";

    //HACK this is the path that will be use if build using the _build project 
    //private const string BuildProps = "../../../Directory.Build.props";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Configuration to build NugetVersion - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string NugetVersion = IsLocalBuild ? "20.1.3.1" : "20.1.3.1";

    [Parameter("Configuration to build XpoVersion - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string XpoVersion = IsLocalBuild ? "20.1.3" : "20.1.3";

    [Parameter("Configuration to build XafVersion - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string XafVersion = IsLocalBuild ? "20.1.3" : "20.1.3";

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });


    Target CreateBuildProps => _ => _
      .Executes(() =>
      {
          ulong v = DotNetTime.DotNetPico();
          Console.WriteLine($"DotNetSmall:{NugetVersion}.{v}");


          if (File.Exists("Directory.Build.props_backup"))
              File.Delete("Directory.Build.props_backup");

          if (!File.Exists(BuildProps))
          {
              Console.WriteLine("Missing prop file");
              throw new Exception("Missing prop file");
          }
          File.Copy(BuildProps, "Directory.Build.props_backup");

          var props = File.ReadAllText("Directory.Build.props_backup");

          //Read the template and replace the values with the parameters






          var Template = File.ReadAllText(BuildProps)
                .Replace("$XpoVersion", XpoVersion)
                .Replace("$XafVersion", XafVersion)
                .Replace("$NugetVersion", $"{NugetVersion}.{v}");//$".{new DateTimeOffset(DateTime.Now).ToUnixTime()}");

          //Write the new props, later we will restore the origi
          File.WriteAllText(BuildProps, Template);
      });


    Target Restore => _ => _
        //.DependsOn(CreateBuildProps)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Restore"));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {

            try
            {
                MSBuild(s => s
               .SetTargetPath(Solution)
               .SetTargets("Rebuild")
               .SetConfiguration(Configuration)
               .SetAssemblyVersion(GitVersion.AssemblySemVer)
               .SetFileVersion(GitVersion.AssemblySemFileVer)
               .SetInformationalVersion(GitVersion.InformationalVersion)
               .SetMaxCpuCount(Environment.ProcessorCount)

               //here is too late to set this values, that is why we are rewriting the props

               //.AddProperty("XpoVersion", XpoVersion)
               //.AddProperty("XafVersion", XafVersion)

               //Everything else is ok

               //.AddProperty("NugetVersion", NugetVersion)
               .SetNodeReuse(IsLocalBuild));

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                //Always restore the file

                if (File.Exists("Directory.Build.props"))
                    File.Delete("Directory.Build.props");

                if (File.Exists("Directory.Build.props_backup"))
                {
                    File.Copy("Directory.Build.props_backup", "Directory.Build.props");
                    File.Delete("Directory.Build.props_backup");
                }
            }



        });
    public static class DotNetTime
    {
        public static UInt64 ToDotNetTime(DateTime Date)
        {
            double totalMilliseconds = (Date - new DateTime(2002, 2, 13)).TotalMilliseconds;
            double value = Math.Round(totalMilliseconds, 0);
            return Convert.ToUInt64(value);
        }
        public static UInt64 DotNetNow()
        {
            return ToDotNetTime(DateTime.UtcNow);
        }
        public static UInt64 DotNetNow(DateTime Date)
        {
            return ToDotNetTime(Date);
        }
        public static UInt64 DotNetNowSmall(DateTime Date)
        {
            return Convert.ToUInt64(DotNetNow(Date) * 0.5);
        }
        public static UInt64 DotNetPico(DateTime Date)
        {
            return Convert.ToUInt64(DotNetNow(Date) * 0.001);
        }
        public static UInt64 DotNetPico()
        {
            return Convert.ToUInt64(DotNetNow(DateTime.UtcNow) * 0.001);
        }
        public static UInt64 DotNetNowSmall()
        {
            return Convert.ToUInt64(DotNetNow());
        }
    }
}
