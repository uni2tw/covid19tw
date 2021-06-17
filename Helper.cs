using System;
using System.Diagnostics;
using System.IO;


namespace Covid19TW
{
    public class Helper
    {
        private static string _rootPath;

        public static string MapPath(string relPath)
        {            
            if (_rootPath == null)
            {
                _rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) +
                    Path.DirectorySeparatorChar;
            }
            string result = Path.Combine(_rootPath, relPath);

            return result;
        }

    }
}