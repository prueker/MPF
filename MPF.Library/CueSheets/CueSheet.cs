﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <remarks>
/// Information sourced from http://web.archive.org/web/20070221154246/http://www.goldenhawk.com/download/cdrwin.pdf
/// </remarks>
namespace MPF.CueSheets
{
    /// <summary>
    /// Represents a single cuesheet
    /// </summary>
    public class CueSheet
    {
        /// <summary>
        /// CATALOG
        /// </summary>
        public string Catalog { get; set; }

        /// <summary>
        /// CDTEXTFILE
        /// </summary>
        public string CdTextFile { get; set; }

        /// <summary>
        /// PERFORMER
        /// </summary>
        public string Performer { get; set; }

        /// <summary>
        /// SONGWRITER
        /// </summary>
        public string Songwriter { get; set; }

        /// <summary>
        /// TITLE
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// List of FILE in cuesheet
        /// </summary>
        public List<CueFile> Files { get; set; }

        /// <summary>
        /// Create an empty cuesheet
        /// </summary>
        public CueSheet()
        {
        }

        /// <summary>
        /// Create a cuesheet from a file, if possible
        /// </summary>
        /// <param name="filename"></param>
        public CueSheet(string filename)
        {
            // Check that the file exists
            if (!File.Exists(filename))
                return;

            // Check the extension
            string ext = Path.GetExtension(filename).TrimStart('.');
            if (!string.Equals(ext, "cue", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ext, "txt", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Open the file and begin reading
            string[] cueLines = File.ReadAllLines(filename);
            for (int i = 0; i < cueLines.Length; i++)
            {
                string line = cueLines[i].Trim();

                // http://stackoverflow.com/questions/554013/regular-expression-to-split-on-spaces-unless-in-quotes
                string[] splitLine = Regex
                    .Matches(line, @"[^\s""]+|""[^""]*""")
                    .Cast<Match>()
                    .Select(m => m.Groups[0].Value)
                    .ToArray();

                // If we have an empty line, we skip
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                switch (splitLine[0])
                {
                    // Read comments
                    case "REM":
                        // We ignore all comments for now
                        break;

                    // Read MCN
                    case "CATALOG":
                        if (splitLine.Length < 2)
                            continue; // TODO: Make this throw an exception

                        this.Catalog = splitLine[1];
                        break;

                    // Read external CD-Text file path
                    case "CDTEXTFILE":
                        if (splitLine.Length < 2)
                            continue; // TODO: Make this throw an exception

                        this.CdTextFile = splitLine[1];
                        break;

                    // Read CD-Text enhanced performer
                    case "PERFORMER":
                        if (splitLine.Length < 2)
                            continue; // TODO: Make this throw an exception

                        this.Performer = splitLine[1];
                        break;

                    // Read CD-Text enhanced songwriter
                    case "SONGWRITER":
                        if (splitLine.Length < 2)
                            continue; // TODO: Make this throw an exception

                        this.Songwriter = splitLine[1];
                        break;

                    // Read CD-Text enhanced title
                    case "TITLE":
                        if (splitLine.Length < 2)
                            continue; // TODO: Make this throw an exception

                        this.Title = splitLine[1];
                        break;

                    // Read file information
                    case "FILE":
                        if (splitLine.Length < 3)
                            continue; // TODO: Make this throw an exception

                        if (this.Files == null)
                            this.Files = new List<CueFile>();

                        var file = new CueFile(splitLine[1], splitLine[2], cueLines, ref i);
                        if (file == default)
                            continue; // TODO: Make this throw an exception

                        this.Files.Add(file);
                        break;
                }
            }
        }

        /// <summary>
        /// Write the cuesheet out to a file
        /// </summary>
        /// <param name="filename">File path to write to</param>
        public void Write(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                Write(fs);
            }
        }

        /// <summary>
        /// Write the cuesheet out to a stream
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        public void Write(Stream stream)
        {
            // If we don't have any files, it's invalid
            if (this.Files == null || this.Files.Count == 0)
                return; // TODO: Make this throw an exception

            using (var sw = new StreamWriter(stream, Encoding.ASCII, 1024, true))
            {
                if (!string.IsNullOrEmpty(this.Catalog))
                    sw.WriteLine($"CATALOG {this.Catalog}");

                if (!string.IsNullOrEmpty(this.CdTextFile))
                    sw.WriteLine($"CDTEXTFILE {this.CdTextFile}");

                if (!string.IsNullOrEmpty(this.Performer))
                    sw.WriteLine($"PERFORMER {this.Performer}");

                if (!string.IsNullOrEmpty(this.Songwriter))
                    sw.WriteLine($"SONGWRITER {this.Songwriter}");

                if (!string.IsNullOrEmpty(this.Title))
                    sw.WriteLine($"TITLE {this.Title}");

                foreach (var file in Files)
                {
                    file.Write(sw);
                }
            }
        }
    }
}
