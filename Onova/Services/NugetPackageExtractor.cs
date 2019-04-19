﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onova.Internal;

namespace Onova.Services
{
    /// <summary>
    /// Extracts files from NuGet packages.
    /// </summary>
    public class NugetPackageExtractor : IPackageExtractor
    {
        private readonly string _rootDirPath;

        /// <summary>
        /// Initializes an instance of <see cref="NugetPackageExtractor"/>.
        /// </summary>
        public NugetPackageExtractor(string rootDirPath)
        {
            _rootDirPath = rootDirPath.GuardNotNull(nameof(rootDirPath));
        }

        /// <inheritdoc />
        public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
            IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            sourceFilePath.GuardNotNull(nameof(sourceFilePath));
            destDirPath.GuardNotNull(nameof(destDirPath));

            // Read the zip
            using (var archive = ZipFile.OpenRead(sourceFilePath))
            {
                // Get entries in the content directory
                var entries = archive.Entries
                    .Where(e => e.FullName.StartsWith(_rootDirPath, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // For progress reporting
                var totalBytes = entries.Sum(e => e.Length);
                var totalBytesCopied = 0L;

                // Loop through entries
                foreach (var entry in entries)
                {
                    // Get relative entry path
                    var relativeEntryPath = entry.FullName.Substring(_rootDirPath.Length).TrimStart('/', '\\');

                    // Get destination paths
                    var entryDestFilePath = Path.Combine(destDirPath, relativeEntryPath);
                    var entryDestDirPath = Path.GetDirectoryName(entryDestFilePath);

                    // Create directory
                    if (!entryDestDirPath.IsNullOrWhiteSpace())
                        Directory.CreateDirectory(entryDestDirPath);

                    // Extract entry
                    using (var input = entry.Open())
                    using (var output = File.Create(entryDestFilePath))
                    {
                        var buffer = new byte[81920];
                        int bytesCopied;
                        do
                        {
                            // Copy
                            bytesCopied = await input.CopyChunkToAsync(output, buffer, cancellationToken);

                            // Report progress
                            totalBytesCopied += bytesCopied;
                            progress?.Report(1.0 * totalBytesCopied / totalBytes);
                        } while (bytesCopied > 0);
                    }
                }
            }
        }
    }
}