﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibHac;
using LibHac.IO;

namespace XbTool.Xb2.FS
{
    public class Create
    {
        public static IFileSystem CreateFileSystem(string sdPath)
        {
            SwitchFs sdFs = OpenSdCard(sdPath);
            Application xb2App = sdFs.Applications[0x0100E95004038000];

            IFileSystem mainFs = xb2App.Patch.MainNca.OpenSectionFileSystem(1, IntegrityCheckLevel.ErrorOnInvalid);
            IFile mainArh = mainFs.OpenFile("/bf2.arh", OpenMode.Read);
            IFile mainArd = mainFs.OpenFile("/bf2.ard", OpenMode.Read);

            var mainArchiveFs = new ArchiveFileSystem(mainArh, mainArd);

            var fsList = new List<IFileSystem>();
            fsList.Add(xb2App.Patch.MainNca.OpenSectionFileSystem(1, IntegrityCheckLevel.ErrorOnInvalid));
            fsList.Add(mainArchiveFs);

            foreach (Title aoc in xb2App.AddOnContent.OrderBy(x => x.Id))
            {
                IFileSystem aocFs = aoc.MainNca.OpenSectionFileSystem(0, IntegrityCheckLevel.ErrorOnInvalid);
                fsList.Add(aocFs);

                if (aoc.Id == 0x0100E95004039001)
                {
                    IFile aocArh = aocFs.OpenFile("/aoc1.arh", OpenMode.Read);
                    IFile aocArd = aocFs.OpenFile("/aoc1.ard", OpenMode.Read);

                    var aocArchiveFs = new ArchiveFileSystem(aocArh, aocArd);
                    fsList.Add(aocArchiveFs);
                }
            }

            return new LayeredFileSystem(fsList);
        }

        public static SwitchFs OpenSdCard(string path, IProgressReport logger = null)
        {
            SwitchFs switchFs;
            Keyset keyset = OpenKeyset();

            var baseFs = new LocalFileSystem(path);

            if (Directory.Exists(Path.Combine(path, "Nintendo", "Contents", "registered")))
            {
                logger?.LogMessage("Treating path as SD card storage");
                switchFs = SwitchFs.OpenSdCard(keyset, baseFs);
            }
            else if (Directory.Exists(Path.Combine(path, "Contents", "registered")))
            {
                logger?.LogMessage("Treating path as NAND storage");
                switchFs = SwitchFs.OpenNandPartition(keyset, baseFs);
            }
            else
            {
                logger?.LogMessage("Treating path as a directory of loose NCAs");
                switchFs = SwitchFs.OpenNcaDirectory(keyset, baseFs);
            }

            return switchFs;
        }

        private static Keyset OpenKeyset()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string homeKeyFile = Path.Combine(home, ".switch", "prod.keys");
            string homeTitleKeyFile = Path.Combine(home, ".switch", "title.keys");
            string homeConsoleKeyFile = Path.Combine(home, ".switch", "console.keys");

            return ExternalKeys.ReadKeyFile(homeKeyFile, homeTitleKeyFile, homeConsoleKeyFile);
        }
    }
}