using System.Text;
using System.Xml;

namespace ManifestBuilder;

public static class Builder
{
    private static string FixPath(string p)
    {
        var path = p.Replace("/", "\\");
        if (!path.EndsWith(@"\\")) path = @$"{path}\";
        return path;
    }

    private static IList<string> GetFiles(string path, string extension)
    {
        return Directory.GetFiles(path, $"*.{extension}", SearchOption.TopDirectoryOnly)
            .Select(s => s.Replace(path, string.Empty))
            .ToList();
    }

    private static IList<string> GetProjects(string path)
    {
        return Directory.GetDirectories(path)
            .Select(s => s.Replace(path, String.Empty))
            .ToList();
    }

    private static string Beautify(XmlDocument doc)
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace
        };
        using (var writer = XmlWriter.Create(sb, settings))
        {
            doc.Save(writer);
        }

        return sb.ToString();
    }

    /// <summary>
    /// C:\dev\FourRoads.TelligentCommunity\src\
    /// </summary>
    /// <param name="solutionPath"></param>
    public static void Build(string solutionPath)
    {
        var path = FixPath(solutionPath);
        var libPath = @$"{path}lib\Telligent\";
        var codePath = $@"{path}code\";

        var libs = GetFiles(libPath, "dll");
        var projectDirs = GetProjects(codePath);
        foreach (var projectDir in projectDirs)
        {
            var projectPath = @$"{codePath}{projectDir}\";
            var nuspec = GetFiles(projectPath, "nuspec").FirstOrDefault();
            if (nuspec == null) continue;

            var dir = @$"{projectPath}\bin\";
            var dllDir = string.Empty;
            if (Directory.Exists($"{dir}Debug"))
            {
                dllDir = @$"{dir}Debug\";
            }
            else if (Directory.Exists($"{dir}Release"))
            {
                dllDir = @$"{dir}Release\";
            }

            Console.WriteLine($"Package: {nuspec.Replace(".nuspec", string.Empty)}");
            var xmlDoc = new XmlDocument();
            // var docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            // xmlDoc.AppendChild(docNode);
            var idAttr = xmlDoc.CreateAttribute("id");
            idAttr.Value = projectDir;
            var releaseElement = xmlDoc.CreateElement("release");
            releaseElement.Attributes.Append(idAttr);
            xmlDoc.AppendChild(releaseElement);
            var filesElement = xmlDoc.CreateElement("files");
            var uniqueDlls = GetFiles(dllDir, "dll").Except(libs, new LibsComparer()).ToList();
            foreach (var uniqueDll in uniqueDlls)
            {
                var fileElement = xmlDoc.CreateElement("file");
                var nameAttr = xmlDoc.CreateAttribute("name");
                nameAttr.Value = uniqueDll;
                fileElement.Attributes.Append(nameAttr);
                filesElement.AppendChild(fileElement);
                Console.WriteLine(uniqueDll);
            }

            releaseElement.AppendChild(filesElement);
            var sqlFiles = Directory.GetFiles(projectPath, "*.sql", SearchOption.AllDirectories)
                .Select(s => s.Replace(projectPath, string.Empty))
                .ToList();
            var resourcesElement = xmlDoc.CreateElement("resources");
            foreach (var sqlFile in sqlFiles)
            {
                var resourceElement = xmlDoc.CreateElement("resource");
                var nameAttr = xmlDoc.CreateAttribute("name");
                nameAttr.Value = sqlFile;
                resourceElement.Attributes.Append(nameAttr);
                resourcesElement.AppendChild(resourceElement);
                Console.WriteLine(sqlFile);
            }

            releaseElement.AppendChild(resourcesElement);
            var mdFiles = Directory.GetFiles(projectPath, "*.md", SearchOption.AllDirectories)
                .Select(s => s.Replace(projectPath, string.Empty))
                .ToList();
            var docsElement = xmlDoc.CreateElement("docs");
            foreach (var mdFile in mdFiles)
            {
                var docElement = xmlDoc.CreateElement("doc");
                var nameAttr = xmlDoc.CreateAttribute("name");
                nameAttr.Value = mdFile;
                docElement.Attributes.Append(nameAttr);
                docsElement.AppendChild(docElement);
                Console.WriteLine(mdFile);
            }

            releaseElement.AppendChild(docsElement);
            File.WriteAllText(@$"{projectPath}release.manifest.xml",
                $@"<?xml version=""1.0"" encoding=""utf-8""?>{Environment.NewLine}{Beautify(xmlDoc)}");
            Console.WriteLine("==============================================================");
        }
    }

    public static bool ValidPath(string path)
    {
        if (!Directory.Exists(path)) return false;
        path = FixPath(path);
        if (!Directory.Exists($@"{path}lib/Telligent"))
        {
            Console.WriteLine($"Cannot find 'lib/Telligent' folder in {path}");
            return false;
        }

        if (!Directory.Exists($@"{path}code"))
        {
            Console.WriteLine($"Cannot find 'code' folder in {path}");
            return false;
        }

        return true;
    }

    private class LibsComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            //avoid RSS.Net.dll not matching RSS.NET.dll
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLowerInvariant().GetHashCode();
        }
    }
}