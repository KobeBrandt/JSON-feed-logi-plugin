namespace Loupedeck.JsonFeedPlugin.Helpers
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class PluginResources
    {
        private static Assembly _assembly;

        public static void Init(Assembly assembly) => _assembly = assembly;

        public static String ReadTextResource(String resourceName)
        {
            using (var stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(resourceName);
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static Byte[] ReadBinaryFile(String fileName)
        {
            var resourceName = fileName.Replace("-", "_").Replace("/", ".");
            using (var stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(fileName);
                }

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
