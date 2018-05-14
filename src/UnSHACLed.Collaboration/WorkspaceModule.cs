using System.IO;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that allows workspaces to be stored and retrieved.
    /// </summary>
    public class WorkspaceModule : GitHubAccessModule
    {
        public WorkspaceModule()
            : base("workspace")
        {
            RegisterContentTrackerGet(
                "/{token}",
                async (args, user, client) => {
                    var login = await client.GetLogin();
                    return LoadWorkspace(login);
                });

            RegisterContentTrackerPut(
                "/{token}",
                async (args, user, client) => {
                    var login = await client.GetLogin();
                    SaveWorkspace(login, Request.Body.AsString());
                    return HttpStatusCode.OK;
                });
        }

        private const string WorkspaceExtension = ".workspace.json";

        private static DirectoryInfo GetWorkspaceDirectory()
        {
            return Directory.CreateDirectory("UnSHACLed-workspaces");
        }

        private static string GetWorkspacePath(string login)
        {
            var dir = GetWorkspaceDirectory();
            return Path.Combine(dir.FullName, login + WorkspaceExtension);
        }

        private static string LoadWorkspace(string login)
        {
            var path = GetWorkspacePath(login);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            else
            {
                // Return a default workspace.
                return @"{ ""SHACLShapesGraph"": [], ""DataGraph"": [], ""IO"": [] }";
            }
        }

        private static void SaveWorkspace(string login, string workspaceContents)
        {
            var path = GetWorkspacePath(login);
            File.WriteAllText(path, workspaceContents);
        }
    }
}
