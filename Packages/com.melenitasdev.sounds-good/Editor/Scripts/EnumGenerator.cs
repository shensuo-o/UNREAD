/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */
#if UNITY_EDITOR
using System;
using System.IO;

namespace MelenitasDev.SoundsGood.Editor
{
    internal class EnumGenerator : IDisposable
    {
        /// <summary>
        /// Defined by the SoundsGood.Application assembly (see its .asmdef versionDefines), so it is
        /// present exactly while the package is installed.
        /// </summary>
        internal const string PACKAGE_DEFINE = "SOUNDS_GOOD";

        internal void GeneratePseudoEnum (string enumName, string[] tags, string locationPath)
        {
            string ident = "\t";
            using (StreamWriter streamWriter = new StreamWriter(locationPath))
            {
                // These files live in the user's Assets (so a package update cannot wipe them) but
                // declare one half of a partial struct whose other half - the constructor - lives in
                // the package. Uninstall the package and they would be left declaring a type they
                // cannot build: a compile error in a project that no longer even has Sounds Good.
                // SOUNDS_GOOD is defined by SoundsGood.Application, the assembly these files join
                // through the .asmref beside them, so removing the package empties them out instead
                // of breaking the project.
                streamWriter.WriteLine("#if " + PACKAGE_DEFINE);
                streamWriter.WriteLine("namespace MelenitasDev.SoundsGood");
                streamWriter.WriteLine("{");
                streamWriter.WriteLine(ident + "public partial struct " + enumName);
                streamWriter.WriteLine(ident + "{");

                if (tags is { Length: > 0 })
                {
                    foreach (var tag in tags)
                    {
                        streamWriter.WriteLine(
                            ident + ident +
                            "public static readonly " + enumName + " " + tag +
                            " = new " + enumName + "(\"" + tag + "\");"
                        );
                    }
                }

                streamWriter.WriteLine(ident + "}");
                streamWriter.WriteLine("}");
                streamWriter.WriteLine("#endif");
            }
        }

        public void Dispose ()
        {
            
        }
    }
}
#endif