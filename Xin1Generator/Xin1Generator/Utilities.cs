using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Xin1Generator {
    public class Utilities {
        public static void CheckDependencies() {
            foreach (string dependencyFileName in new[] { Eac3toWrapper.processFileName, XportWrapper.processFileName }) {
                try {
                    new Process {
                        StartInfo = {
                            FileName = dependencyFileName,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    }.Start();
                } catch (Win32Exception e) {
                    throw new InvalidOperationException(e.Message + ": " + dependencyFileName);
                }
            }
        }

        public static bool IsBluray(string path) {
            return Directory.Exists(Path.Combine(path, "BDMV"));
        }
    }
}
