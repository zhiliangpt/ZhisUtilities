using System;
using System.Collections.Generic;
using System.Text;

namespace Zhis.Utilities.GitHub
{
	/// <summary>
	/// Represents the settings for a GitHub repository.
	/// </summary>
	public class RepositorySettings
	{
		/// <summary>
		/// Gets or sets the owner of the repository.
		/// </summary>
		public string Owner { get; set; }

		/// <summary>
		/// Gets or sets the name of the repository.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the access token used to authenticate with the repository.
		/// This token can be a Personal Access Token (PAT), Organization Access Token, or other appropriate token type.
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositorySettings"/> class with the specified owner, repository name, and access token.
		/// </summary>
		/// <param name="owner">The owner of the GitHub repository.</param>
		/// <param name="name">The name of the GitHub repository.</param>
		/// <param name="accessToken">The access token used to authenticate with the GitHub repository.</param>
		public RepositorySettings(string owner, string name, string accessToken)
		{
			Owner = owner;
			Name = name;
			AccessToken = accessToken;
		}
	}
}
