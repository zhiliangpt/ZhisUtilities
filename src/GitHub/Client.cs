
using Octokit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zhis.Utilities.GitHub
{
	public class Client
	{
		public static Octokit.GitHubClient GetClient(string personalAccessToken = null, ProductHeaderValue productInformation = null)
		{
			if (productInformation == null)
			{
				productInformation = new ProductHeaderValue(Guid.NewGuid().ToString());
			}

			Octokit.GitHubClient result = new GitHubClient(productInformation: productInformation);

			if (!string.IsNullOrEmpty(personalAccessToken))
			{
				result.Credentials = new Credentials(personalAccessToken);
			}

			return result;
		}



	}
}
