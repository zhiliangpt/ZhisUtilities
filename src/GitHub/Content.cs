using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Octokit;
using Octokit.Models.Request.Enterprise;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zhis.Utilities.GitHub;

namespace Zhis.Utilities.GitHub
{
	public class ImageInfo
	{
		public RepositorySettings RepositorySettings { get; set; }
		public string Url { get; set; }
		public string AltText { get; set; }
		public string Title { get; set; }
		public bool IsAsset { get; set; }


		#region Methods
		public async Task<(byte[] imageBinary, string fileName)> GetImageBinaryAsync()
		{
			using (HttpClient httpClient = new HttpClient())
			{
				if (this.RepositorySettings != null)
				{
					httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.RepositorySettings.AccessToken);
				}
				try
				{
					var response = await httpClient.GetAsync(this.Url);
					response.EnsureSuccessStatusCode();
					string fileName = null;

					// 从响应头中获取文件名
					if (response.Content.Headers.ContentDisposition != null)
					{
						fileName = response.Content.Headers.ContentDisposition.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName;
					}
					if (fileName == null)
					{
						// 如果没有Content-Disposition头，尝试使用Content-Type头推断文件扩展名
						var contentType = response.Content.Headers.ContentType.MediaType;
						var extension = contentType switch
						{
							"image/jpeg" => ".jpg",
							"image/png" => ".png",
							_ => ".bin" // 默认扩展名
						};
						var assetIdMatch = Regex.Match(this.Url, @"[^/]+$");
						fileName = assetIdMatch.Success ? assetIdMatch.Value + extension : "downloaded_image" + extension;
					}

					var imageBinary = await response.Content.ReadAsByteArrayAsync();
					return (imageBinary, fileName);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error downloading image: {ex.Message}");
					return (null, null);
				}
			}
		}
		#endregion
	}

	public class ImageAsset : ImageInfo
	{
		public string Account { get; set; }
		public string Repo { get; set; }
		public string AuthorId { get; set; }
		public string AssetId { get; set; }
	}

	public class Content : IDisposable
	{
		#region Properties

		/// <summary>
		/// Gets or sets the settings for the repository, including a Github repository's owner, name and the access token.
		/// </summary>
		public RepositorySettings RepositorySettings { get; set; }
		public string Branch { get; set; }
		public string FilePath { get; set; }

		public string ContentString { get; set; }
		public bool IsContentStringLoaded => this.ContentString != default;

		public byte[] ContentBinary { get; set; }
		public bool IsContentBinaryLoaded => this.ContentBinary != default;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class with the specified repository settings.
		/// </summary>
		/// <param name="settings">The settings for the GitHub repository.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/> parameter is null.</exception>
		public Content(RepositorySettings repo, string branch, string filePath)
		{
			RepositorySettings = new RepositorySettings(repo.Owner, repo.Name, repo.AccessToken);
			Branch = branch;
			FilePath = filePath;
		}

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
		public async Task<byte[]?> GetRawContent()
		{
			byte[]? result = null;

			using (var repo = new GitHub.Repository(RepositorySettings))
			{
				result = await repo.GetFileRawContent(FilePath, Branch);
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
		public async Task<string> GetStringContent()
		{
			string result = default;

			using (var repo = new GitHub.Repository(RepositorySettings))
			{
				result = await repo.GetFileStringContent(FilePath, Branch);
			}

			return result;
		}

		public async Task<bool> LoadContentString()
		{
			this.ContentString = await GetStringContent();
			return this.IsContentStringLoaded;
		}

		#endregion

		#region Put

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
		public async Task<RepositoryContentChangeSet> PutStringContent(string content)
		{
			RepositoryContentChangeSet result = default;

			using (var repo = new GitHub.Repository(RepositorySettings))
			{
				result = await repo.PutFileContent(content, FilePath, Branch);
			}

			return result;

		}

		#endregion

		#region Markdown Related

		/* Here polymorphism is used to return and the consumer has to use the following code to determine and output.
if (info.IsAsset && info is ImageAsset asset)
{
    Console.WriteLine($"Owner: {asset.Owner}, User: {asset.User}, Repo: {asset.Repo}, AssetId: {asset.AssetId}");
}
		*/

		public async Task<List<ImageInfo>> GetImagesFromMarkdown(bool attachRepoSettings = false)
		{
			List<ImageInfo> result = default;

			if (this.IsContentStringLoaded || await LoadContentString())
			{
				result = ExtractImageInfos(this.ContentString);
			}
			if (attachRepoSettings && result != default)
			{
				foreach (var image in result)
				{
					image.RepositorySettings = this.RepositorySettings;
				}
			}

			return result;
		}

		#region Private Static Methods

		private static List<ImageInfo> ExtractImageInfos(string markdownContent)
		{
			var document = Markdown.Parse(markdownContent);
			var imageInfos = new List<ImageInfo>();

			foreach (var node in document.Descendants())
			{
				if (node is LinkInline linkInline && linkInline.IsImage)
				{
					var altText = linkInline.FirstChild != null ? ((LiteralInline)linkInline.FirstChild).Content.ToString() : "";
					var isAsset = IsAssetUrl(linkInline.Url);

					ImageInfo imageInfo = isAsset ? new ImageAsset() : new ImageInfo();
					imageInfo.Url = linkInline.Url;
					imageInfo.AltText = altText;
					imageInfo.Title = linkInline.Title;
					imageInfo.IsAsset = isAsset;

					if (isAsset && imageInfo is ImageAsset asset)
					{
						ExtractAssetInfo(asset);
					}

					imageInfos.Add(imageInfo);
				}
			}

			return imageInfos;
		}

		private static bool IsAssetUrl(string url)
		{
			// 判断URL是否符合GitHub assets的格式
			return Regex.IsMatch(url, @"^https://github.com/[^/]+/[^/]+/assets/[^/]+/[^/]+$");
		}

		private static void ExtractAssetInfo(ImageAsset asset)
		{
			// 使用正则表达式提取用户、仓库和Asset ID信息
			var match = Regex.Match(asset.Url, @"^https://github.com/([^/]+)/([^/]+)/assets/([^/]+)/([^/]+)$");
			if (match.Success)
			{
				asset.Account = match.Groups[1].Value;
				asset.Repo = match.Groups[2].Value;
				// 第 3 项 Assets路径忽略
				asset.AuthorId = match.Groups[4].Value;
				asset.AssetId = match.Groups[5].Value;
			}
		}

		#endregion

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
		/// Finalizes an instance of the <see cref="Content"/> class.
		/// </summary>
		~Content()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
