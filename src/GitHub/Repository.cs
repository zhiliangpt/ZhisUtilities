using Octokit;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Zhis.Utilities.GitHub
{

	/// <summary>
	/// Represents a class for interacting with a GitHub repository.
	/// </summary>
	public class Repository : IDisposable
	{
		#region Properties

		/// <summary>
		/// Gets or sets the settings for the repository, including a Github repository's owner, name and the access token.
		/// </summary>
		public RepositorySettings Settings { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class with the specified personal access token, owner, and repository name.
		/// </summary>
		/// <param name="owner">The owner of the repository.</param>
		/// <param name="name">The name of the repository.</param>
		/// <param name="accessToken">The access token used to authenticate with the repository's client.</param>
		public Repository(string owner, string name, string accessToken)
		{
			Settings = new RepositorySettings(owner, name, accessToken);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class with the specified repository settings.
		/// </summary>
		/// <param name="settings">The settings for the GitHub repository.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/> parameter is null.</exception>
		public Repository(RepositorySettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			else
				Settings = new RepositorySettings(settings.Owner, settings.Name, settings.AccessToken);
		}

		#endregion

		private ProductHeaderValue ProductHeaderValue = new ProductHeaderValue(Guid.NewGuid().ToString());

		#region Content

		#region List

		/// <summary>
		/// Retrieves a list of repository contents at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the contents to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the contents. Default is "main".</param>
		/// <param name="includeFile">Determines whether to include files in the retrieved contents. Default is true.</param>
		/// <param name="includeDirectory">Determines whether to include directories in the retrieved contents. Default is true.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="RepositoryContent"/>.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It retrieves all contents by reference for the specified owner, repository, path, and branch.
		/// The retrieved contents are then added to a list of <see cref="RepositoryContent"/> and returned.
		/// </remarks>
		public async Task<List<RepositoryContent>> List(string path, string branch = "main", bool includeFile = true, bool includeDirectory = true)
		{
			List<RepositoryContent> result = new List<RepositoryContent>();

			var client = Client.GetClient(Settings.AccessToken);

			//Remove the first character, if it starts with a "/".
			if (path.StartsWith("/"))
			{
				path.Substring(1);
			}

			IReadOnlyList<Octokit.RepositoryContent> contents = default;
			try
			{
				contents = await client.Repository.Content.GetAllContentsByRef(Settings.Owner, Settings.Name, path, branch);
			}
			catch (Octokit.NotFoundException)
			{
				contents = new List<Octokit.RepositoryContent>();
			}
			catch (Exception) { throw; }

			foreach (var item in contents)
			{
				if (includeFile && item.Type == Octokit.ContentType.File || includeDirectory && item.Type == Octokit.ContentType.Dir)
				{
					//result.Add(new RepositoryContent(item));
					result.Add(item);
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieves a list of repository files at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the files to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the files. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="RepositoryContent"/> representing files.</returns>
		/// <remarks>
		/// This method internally calls the <see cref="ListContents"/> method with the <paramref name="includeFile"/> parameter set to true and the <paramref name="includeDirectory"/> parameter set to false.
		/// It uses a personal access token to authenticate with the repository's client.
		/// The retrieved files are then added to a list of <see cref="RepositoryContent"/> and returned.
		/// </remarks>
		public async Task<List<RepositoryContent>> ListFiles(string path, string branch = "main")
		{
			return await List(path, branch, includeFile: true, includeDirectory: false);
		}

		/// <summary>
		/// Retrieves a list of repository directories at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the directories to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the directories. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="RepositoryContent"/> representing directories.</returns>
		/// <remarks>
		/// This method internally calls the <see cref="ListContents"/> method with the <paramref name="includeFile"/> parameter set to false and the <paramref name="includeDirectory"/> parameter set to true.
		/// It uses a personal access token to authenticate with the repository's client.
		/// The retrieved directories are then added to a list of <see cref="RepositoryContent"/> and returned.
		/// </remarks>
		public async Task<List<RepositoryContent>> ListDirectories(string path, string branch = "main")
		{
			return await List(path, branch, includeFile: false, includeDirectory: true);
		}

		#region Exists

		/// <summary>
		/// Retrieves the metadata of a file at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the file. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains the <see cref="RepositoryContent"/> object representing the file if it exists; otherwise, it returns null.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It retrieves the metadata of the file from the specified owner, repository, path, and branch.
		/// If the file is not found, it returns null.
		/// </remarks>
		public async Task<RepositoryContent> ListFile(string path, string branch = "main")
		{
			RepositoryContent result = default;

			// Remove the first character, if it starts with a "/".
			if (path.StartsWith("/"))
			{
				path = path.Substring(1);
			}

			var contents = await ListFiles(path, branch);

			// If any file is found in the contents, it means the file exists.
			if (contents.Any())
				result = contents.First();

			return result;
		}

		/// <summary>
		/// Checks whether a file exists at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the file to check.</param>
		/// <param name="branch">The branch from which to check the file. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result is true if the file exists; otherwise, false.</returns>
		/// <remarks>
		/// This method internally calls the <see cref="ListFile"/> method to retrieve the metadata of the file at the specified path.
		/// It checks if the file metadata is not null, which indicates the file exists.
		/// </remarks>
		public async Task<bool> FileExists(string path, string branch = "main")
		{
			return (await ListFile(path, branch)) != null;
		}

		#endregion

		#endregion

		#region Get

		/// <summary>
		/// Retrieves the raw content of a file at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the file. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains the raw content of the file as a byte array.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It retrieves the raw content of the file at the specified path and branch.
		/// If the file is not found, it returns null.
		/// </remarks>
		public async Task<byte[]?> GetFileRawContent(string path, string branch = "main")
		{
			byte[]? result = null;

			var client = Client.GetClient(Settings.AccessToken);

			//Remove the first character, if it starts with a "/".
			if (path.StartsWith("/"))
			{
				path = path.Substring(1);
			}

			try
			{
				result = await client.Repository.Content.GetRawContentByRef(Settings.Owner, Settings.Name, path, branch);
			}
			catch (NotFoundException)
			{
				// Handle file not found.
				// return null;
			}

			return result;
		}

		/// <summary>
		/// Retrieves the content of a file at the specified path and branch as a string.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the file. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains the content of the file as a string.</returns>
		/// <remarks>
		/// This method internally calls the <see cref="GetFileRawContent"/> method to retrieve the raw content of the file.
		/// It uses UTF-8 encoding to convert the raw content byte array into a string.
		/// If the file is not found, an empty string is returned.
		/// </remarks>
		public async Task<string> GetFileStringContent(string path, string branch = "main")
		{
			string result = default;

			var fileRawContent = await GetFileRawContent(path, branch);
			if (fileRawContent != null)
			{
				result = Encoding.UTF8.GetString(fileRawContent);
			}

			return result;
		}

		#endregion

		#region Put

		/*
		/// <summary>
		/// Stores raw binary content in a file at the specified path and branch.
		/// </summary>
		/// <param name="content">The raw binary content to store in the file.</param>
		/// <param name="path">The path of the file to store.</param>
		/// <param name="branch">The branch in which to store the file. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It stores the provided raw binary content in the file at the specified path and branch.
		/// If the file does not exist, a new file is created. If the file exists, it is updated with the new content.
		/// </remarks>
		public async Task PutFileContent(byte[] content, string path, string branch = "main")
		{
			var client = Client.GetClient(PersonalAccessToken);

			//Remove the first character, if it starts with a "/".
			if (path.StartsWith("/"))
			{
				path = path.Substring(1);
			}

			//check if file exists
			var meta = GetFileMeta(path, branch);
			if (meta == null)
			{
				// If the file does not exist, create a new file
				CreateFileRequest request = new CreateFileRequest("Create " + Path.GetFileName(path) + " in base64.", content);
				await client.Repository.Content.CreateFile(Owner, Repo, path, new CreateFileRequest("Creating a new file", content, branch));
			}
			else
			{
				// If the file does not exist, create a new file
				await client.Repository.Content.CreateFile(Owner, Repo, path, new CreateFileRequest("Creating a new file", content, branch));
			}

			// First, check if the file already exists
			try
			{
				await client.Repository.Content.GetContents(Owner, Repo, path, branch);
				// If the file exists, update it
				await client.Repository.Content.UpdateFile(Owner, Repo, path, new UpdateFileRequest("Updating the file", content, branch));
			}
			catch (NotFoundException)
			{
			}
		}
		*/

		/// <summary>
		/// Stores UTF-8 text content in a file at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the file to store.</param>
		/// <param name="branch">The branch in which to store the file. Default is "main".</param>
		/// <param name="content">The UTF-8 text content to store in the file.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It stores the provided UTF-8 text content in the file at the specified path and branch.
		/// If the file does not exist, a new file is created. If the file exists, it is updated with the new content.
		/// </remarks>
		public async Task PutFileContent(string content, string path, string branch = "main")
		{
			var client = Client.GetClient(Settings.AccessToken);

			//Remove the first character, if it starts with a "/".
			if (path.StartsWith("/"))
			{
				path = path.Substring(1);
			}

			//check if file exists
			var meta = await ListFile(path, branch);
			if (meta == null)
			{
				// If the file does not exist, create a new file
				CreateFileRequest request = new CreateFileRequest("Create " + Path.GetFileName(path), content, branch);
				await client.Repository.Content.CreateFile(Settings.Owner, Settings.Name, path, request);
			}
			else
			{
				// If the file exists, update it
				UpdateFileRequest request = new UpdateFileRequest("Update " + Path.GetFileName(path), content, meta.Sha);
				await client.Repository.Content.UpdateFile(Settings.Owner, Settings.Name, path, request);
			}
		}

		#endregion


		#endregion

		#region Commit

		/// <summary>
		/// Retrieves a GitHub commit based on its reference (SHA).
		/// </summary>
		/// <param name="reference">The reference (SHA) of the commit to retrieve.</param>
		/// <returns>The retrieved GitHub commit, or null if not found.</returns>
		public async Task<Octokit.GitHubCommit> GetCommit(string reference)
		{
			Octokit.GitHubCommit result = null;

			var client = Client.GetClient(Settings.AccessToken);

			try
			{
				result = await client.Repository.Commit.Get(Settings.Owner, Settings.Name, reference);
			}
			catch (NotFoundException)
			{
				// Handle file not found.
				// return null;
			}

			return result;
		}

		/// <summary>
		/// Retrieves a list of all GitHub commits in the repository, optionally filtered by file path.
		/// </summary>
		/// <param name="path">Optional. The file path to filter commits by.</param>
		/// <param name="branch">Optional. The branch name. Default is "main".</param>
		/// <returns>The list of retrieved GitHub commits.</returns>
		public async Task<List<Octokit.GitHubCommit>> GetCommitsAll(string path, string branch = "main")
		{
			List<Octokit.GitHubCommit> result = null;

			var client = Client.GetClient(Settings.AccessToken);

			try
			{
				CommitRequest request = new CommitRequest()
				{
					Path = path
				};
				var list = await client.Repository.Commit.GetAll(Settings.Owner, Settings.Name, request);
				if (list != null)
				{
					result = new List<GitHubCommit>();
					foreach (var item in list)
					{
						result.Add(item);
					}
				}
			}
			catch (NotFoundException)
			{
				// Handle file not found.
				// return null;
			}

			return result;
		}

		/// <summary>
		/// Retrieves a list of GitHub commits since a specified date and optional file path.
		/// </summary>
		/// <param name="since">The date and time after which commits should be retrieved.</param>
		/// <param name="path">Optional. The file path to filter commits by.</param>
		/// <param name="branch">Optional. The branch name. Default is "main".</param>
		/// <returns>The list of retrieved GitHub commits.</returns>
		public async Task<List<Octokit.GitHubCommit>> GetCommitsSince(DateTimeOffset since, bool include, string path, string branch = "main")
		{
			List<Octokit.GitHubCommit> result = null;

			var client = Client.GetClient(Settings.AccessToken);

			try
			{
				CommitRequest request = new CommitRequest()
				{
					Path = path,
					Since = since
				};
				var list = await client.Repository.Commit.GetAll(Settings.Owner, Settings.Name, request);
				if (list != null)
				{
					result = new List<GitHubCommit>();
					foreach (var item in list)
					{
						if (include || item.Commit.Committer.Date != since)
						{
							result.Add(item);
						}
					}
				}
			}
			catch (NotFoundException)
			{
				// Handle file not found.
				// return null;
			}

			return result;
		}

		/// <summary>
		/// Retrieves a list of GitHub commits from a specified SHA, optionally including the commit itself, and an optional file path.
		/// </summary>
		/// <param name="sha">The SHA of the commit to start retrieving from.</param>
		/// <param name="include">True to include the specified SHA commit, false otherwise.</param>
		/// <param name="path">Optional. The file path to filter commits by.</param>
		/// <param name="branch">Optional. The branch name. Default is "main".</param>
		/// <returns>The list of retrieved GitHub commits.</returns>
		public async Task<List<Octokit.GitHubCommit>> GetCommitsFromSha(string sha, bool include, string path, string branch = "main")
		{
			List<Octokit.GitHubCommit> result = null;

			var client = Client.GetClient(Settings.AccessToken);

			try
			{
				CommitRequest request = new CommitRequest()
				{
					//Path = path,
					Sha = sha
				};
				var list = await client.Repository.Commit.GetAll(Settings.Owner, Settings.Name, request);
				if (list != null)
				{
					result = new List<GitHubCommit>();
					foreach (var item in list)
					{
						if (include || item.Sha != sha)
						{
							result.Add(item);
						}
					}
				}
			}
			catch (NotFoundException)
			{
				// Handle file not found.
				// return null;
			}

			return result;
		}

		#endregion

		#region Disposing
		// Has Dispose() already been called?
		Boolean isDisposed = false;

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged and managed resources.
		/// </summary>
		public void Dispose()
		{
			ReleaseResources(true); // cleans both unmanaged and managed resources
			GC.SuppressFinalize(this); // supress finalization
		}

		/// <summary>
		/// Releases the allocated resources.
		/// </summary>
		/// <param name="isFromDispose">Indicates whether the resources are being released from the <see cref="Dispose"/> method.</param>

		protected void ReleaseResources(bool isFromDispose)
		{
			// Try to release resources only if they have not been previously released.
			if (!isDisposed)
			{
				if (isFromDispose)
				{
					// TODO: Release managed resources here
					// GC will automatically release Managed resources by calling the destructor,
					// but Dispose() need to release managed resources manually
				}
				//TODO: Release unmanaged resources here
				//...
			}
			isDisposed = true; // Dispose() can be called numerous times
		}
		// Use C# destructor syntax for finalization code, invoked by GC only.
		/// <summary>
		/// Finalizes an instance of the <see cref="Repository"/> class.
		/// </summary>
		~Repository()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
