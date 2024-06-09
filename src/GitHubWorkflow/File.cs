using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Zhis.Utilities.GitHubWorkflow
{
	/// <summary>
	/// Provides utility methods for working with files in a GitHub workflow.
	/// </summary>
	public class File
	{
		/// <summary>
		/// Determines whether a file was renamed in the most recent commit.
		/// </summary>
		/// <param name="repositoryPath">The path to the Git repository.</param>
		/// <param name="filePath">The relative path to the file within the repository.</param>
		/// <returns>True if the file was renamed in the most recent commit; otherwise, false.</returns>
		/// <exception cref="ArgumentException">Thrown when the repository path is not valid.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the Git process fails to execute.</exception>
		public static bool IsFileRenamed(string repositoryPath, string filePath)
		{
			if (string.IsNullOrEmpty(repositoryPath))
			{
				throw new ArgumentException("Repository path cannot be null or empty", nameof(repositoryPath));
			}

			var processInfo = new ProcessStartInfo("git", $"diff --name-status HEAD~1 HEAD")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = repositoryPath
			};

			var renamedFiles = new List<string>();

			using (var process = new Process { StartInfo = processInfo })
			{
				process.Start();
				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					throw new InvalidOperationException("Failed to execute Git command.");
				}

				var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					var parts = line.Split('\t');
					if (parts.Length == 3 && parts[0] == "R")
					{
						renamedFiles.Add(parts[2]);
					}
				}
			}

			return renamedFiles.Contains(filePath);
		}
	}
}
