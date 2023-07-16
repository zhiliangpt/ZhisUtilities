using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Zhis.Utilities.GitHub
{

	/// <summary>
	/// Represents a repository with properties for personal access token, owner, and repository name.
	/// </summary>
	public class Repository : IDisposable
	{
		#region Properties

		/// <summary>
		/// Gets or sets the personal access token used to authenticate with the repository's client.
		/// </summary>
		public string PersonalAccessToken { get; set; }

		/// <summary>
		/// Gets or sets the owner of the repository.
		/// </summary>
		public string Owner { get; set; }

		/// <summary>
		/// Gets or sets the name of the repository.
		/// </summary>
		public string Repo { get; set; }

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class with the specified personal access token, owner, and repository name.
		/// </summary>
		/// <param name="personalAccessToken">The personal access token used to authenticate with the repository's client.</param>
		/// <param name="owner">The owner of the repository.</param>
		/// <param name="repo">The name of the repository.</param>
		public Repository(string personalAccessToken, string owner, string repo)
		{
			PersonalAccessToken = personalAccessToken;
			Owner = owner;
			Repo = repo;
		}

		private ProductHeaderValue ProductHeaderValue = new ProductHeaderValue(Guid.NewGuid().ToString());

		#region Content

		/// <summary>
		/// Retrieves a list of repository contents at the specified path and branch.
		/// </summary>
		/// <param name="path">The path of the contents to retrieve.</param>
		/// <param name="branch">The branch from which to retrieve the contents. Default is "main".</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="RepositoryContent"/>.</returns>
		/// <remarks>
		/// This method uses a personal access token to authenticate with the repository's client.
		/// It retrieves all contents by reference for the specified owner, repository, path, and branch.
		/// The retrieved contents are then added to a list of <see cref="RepositoryContent"/> and returned.
		/// </remarks>
		public async Task<List<RepositoryContent>> ListContents(string path, string branch = "main")
		{
			List<RepositoryContent> result = new List<RepositoryContent>();

			var client = Client.GetClient(PersonalAccessToken);

			var contents = await client.Repository.Content.GetAllContentsByRef(Owner, Repo, path, branch);
			foreach (var item in contents)
			{
				result.Add(item);
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
