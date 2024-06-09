//using Octokit;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Zhis.Utilities.GitHub
//{
//	/// <summary>
//	/// Represents a piece of content in the repository. This could be a submodule, a symlink, a directory, or a file.
//	/// Look at the Type property to figure out which one it is.
//	/// </summary>
//	public class RepositoryContent
//	{
//		/// <summary>
//		/// Gets or sets the name of the content.
//		/// </summary>
//		public string Name { get; set; }

//		/// <summary>
//		/// Gets or sets the path of the content in the repository.
//		/// </summary>
//		public string Path { get; set; }

//		/// <summary>
//		/// Gets or sets the SHA-1 hash of the content.
//		/// </summary>
//		public string Sha { get; set; }

//		/// <summary>
//		/// Gets or sets the size of the content in bytes.
//		/// </summary>
//		public int Size { get; set; }

//		/// <summary>
//		/// Gets or sets the URL to the content.
//		/// </summary>
//		public string Url { get; set; }

//		/// <summary>
//		/// Gets or sets the HTML URL to the content on GitHub.
//		/// </summary>
//		public string HtmlUrl { get; set; }

//		/// <summary>
//		/// Gets or sets the Git URL to the content.
//		/// </summary>
//		public string GitUrl { get; set; }

//		/// <summary>
//		/// Gets or sets the download URL to the content (applicable for files).
//		/// </summary>
//		public string DownloadUrl { get; set; }

//		/// <summary>
//		/// Gets or sets the type of the content (file or directory).
//		/// </summary>
//		public ContentType Type { get; set; }

//		/// <summary>
//		/// Gets or sets the encoding of the content if this is a file. Typically "base64". Otherwise, it's null.
//		/// </summary>
//		public string Encoding { get; set; }

//		/// <summary>
//		/// Gets or sets the Base64 encoded content if this is a file. Otherwise, it's null.
//		/// </summary>
//		public string EncodedContent { get; set; }

//		/// <summary>
//		/// Gets the unencoded content as a string. Only access this if the content is expected to be text and not binary content.
//		/// </summary>
//		public string Content
//		{
//			get
//			{
//				return EncodedContent != null ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(EncodedContent)) : null;
//			}
//		}

//		/// <summary>
//		/// Gets or sets the path to the target file in the repository if this is a symlink. Otherwise, it's null.
//		/// </summary>
//		public string Target { get; set; }

//		/// <summary>
//		/// Gets or sets the location of the submodule repository if this is a submodule. Otherwise, it's null.
//		/// </summary>
//		public string SubmoduleGitUrl { get; set; }


//		/// <summary>
//		/// Initializes a new instance of the <see cref="RepositoryContent"/> class based on an <see cref="Octokit.RepositoryContent"/> object.
//		/// </summary>
//		/// <param name="octokitRepositoryContent">The <see cref="Octokit.RepositoryContent"/> object to create the <see cref="RepositoryContent"/> from.</param>
//		public RepositoryContent(Octokit.RepositoryContent octokitRepositoryContent)
//		{
//			this.Name = octokitRepositoryContent.Name;
//			this.Path = octokitRepositoryContent.Path;
//			this.Sha = octokitRepositoryContent.Sha;
//			this.Size = octokitRepositoryContent.Size;
//			this.Url = octokitRepositoryContent.Url;
//			this.HtmlUrl = octokitRepositoryContent.HtmlUrl;
//			this.GitUrl = octokitRepositoryContent.GitUrl;
//			this.DownloadUrl = octokitRepositoryContent.DownloadUrl;
//			this.Type = ConvertType(octokitRepositoryContent.Type);

//			// Properties specific to files
//			this.Encoding = octokitRepositoryContent.Encoding;
//			this.EncodedContent = octokitRepositoryContent.Content;

//			// Properties specific to directories
//			this.Target = octokitRepositoryContent.Target;
//			this.SubmoduleGitUrl = octokitRepositoryContent.SubmoduleGitUrl;
//		}

//		private static ContentType ConvertType(StringEnum<Octokit.ContentType> type)
//		{
//			if (type == Octokit.ContentType.File)
//			{
//				return ContentType.File;
//			}
//			else if (type == Octokit.ContentType.Dir)
//			{
//				return ContentType.Dir;
//			}
//			else
//			{
//				// You may want to handle other cases accordingly or throw an exception if needed.
//				throw new ArgumentException($"Unknown content type: {type}");
//			}
//		}
//	}

//	// <summary>
//	/// Enum representing the type of the content (file or directory).
//	/// </summary>
//	public enum ContentType
//	{
//		/// <summary>
//		/// Indicates that the content is a file.
//		/// </summary>
//		File,

//		/// <summary>
//		/// Indicates that the content is a directory.
//		/// </summary>
//		Dir
//	}
//}
