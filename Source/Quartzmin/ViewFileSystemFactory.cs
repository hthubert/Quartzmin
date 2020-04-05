using System.Collections.Generic;
using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Text;

namespace Quartzmin
{
    public static class ViewFileSystemFactory
    {
        static ViewFileSystemFactory()
        {
            RegisterAssembly(nameof(Quartzmin), Assembly.GetExecutingAssembly());
        }

        public static void RegisterAssembly(string namespaceName, Assembly assembly)
        {
            EmbeddedFileSystem.LoadViews(namespaceName, assembly);
        }

        public static ViewEngineFileSystem Create(QuartzminOptions options)
        {
            ViewEngineFileSystem fs;

            if (string.IsNullOrEmpty(options.ViewsRootDirectory))
            {
                fs = new EmbeddedFileSystem();
            }
            else
            {
                fs = new DiskFileSystem(options.ViewsRootDirectory);
            }

            return fs;
        }

        private class DiskFileSystem : ViewEngineFileSystem
        {
            private readonly string _root;

            public DiskFileSystem(string root)
            {
                this._root = root;
            }

            public override string GetFileContent(string filename)
            {
                return File.ReadAllText(GetFullPath(filename));
            }

            protected override string CombinePath(string dir, string otherFileName)
            {
                return Path.Combine(dir, otherFileName);
            }

            public override bool FileExists(string filePath)
            {
                return File.Exists(GetFullPath(filePath));
            }

            private string GetFullPath(string filePath)
            {
                return Path.Combine(_root, filePath.Replace("partials/", "Partials/").Replace('/', Path.DirectorySeparatorChar));
            }
        }

        private class EmbeddedFileSystem : ViewEngineFileSystem
        {
            private static readonly Dictionary<string, (string flag, Assembly asm)> ViewMap = new Dictionary<string, (string, Assembly)>();
            public static void LoadViews(string namespaceName, Assembly assembly)
            {
                var viewFlag = namespaceName + ".Views.";
                foreach (var name in assembly.GetManifestResourceNames())
                {
                    if (name.StartsWith(viewFlag))
                    {
                        ViewMap[name.Substring(viewFlag.Length)] = (viewFlag, assembly);
                    }
                }
            }

            private static string GetFullPath(string filePath)
            {
                return filePath.Replace("partials/", "Partials/").Replace('/', '.').Replace('\\', '.');
            }

            protected override string CombinePath(string dir, string otherFileName)
            {
                return Path.Combine(dir, otherFileName);
            }

            public override string GetFileContent(string filename)
            {
                var path = GetFullPath(filename);
                if (!ViewMap.TryGetValue(path, out var item)) 
                    return null;

                using (var stream = item.asm.GetManifestResourceStream(item.flag + path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            public override bool FileExists(string filePath)
            {
                return ViewMap.ContainsKey(GetFullPath(filePath));
            }
        }

    }
}
