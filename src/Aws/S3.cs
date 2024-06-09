using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Zhis.Utilities.Aws
{
	/// <summary>
	/// Represents a class for interacting with Amazon S3 (Simple Storage Service).
	/// </summary>
	public class S3 : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets or sets the AWS access key ID used for authentication.
		/// </summary>
		public string AwsAccessKeyId { get; set; }
		/// <summary>
		/// Gets or sets the AWS secret access key used for authentication.
		/// </summary>
		public string AwsSecretAccessKey { get; set; }
		/// <summary>
		/// Gets or sets the AWS region endpoint for the S3 service.
		/// </summary>
		public RegionEndpoint RegionEndpoint { get; set; }
		/// <summary>
		/// Gets or sets the name of the S3 bucket.
		/// </summary>
		public string BucketName { get; set; }
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="S3"/> class with the specified AWS credentials and S3 configuration.
		/// </summary>
		/// <param name="awsAccessKeyId">The AWS access key ID used for authentication.</param>
		/// <param name="awsSecretAccessKey">The AWS secret access key used for authentication.</param>
		/// <param name="regionEndpoint">The AWS region endpoint for the S3 service.</param>
		/// <param name="bucketName">The name of the S3 bucket.</param>
		public S3(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint regionEndpoint, string bucketName)
		{
			AwsAccessKeyId = awsAccessKeyId;
			AwsSecretAccessKey = awsSecretAccessKey;
			RegionEndpoint = regionEndpoint;
			BucketName = bucketName;
		}

		#region Exist File

		/// <summary>
		/// Checks if a file with the specified file name and full path exists in the S3 bucket.
		/// </summary>
		/// <param name="fileNameWithFullPath">The full path and file name of the file to check.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the file exists or not.</returns>
		public async Task<bool> S3FileExists(string fileNameWithFullPath)
		{
			bool result = default;

			using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
			{
				try
				{
					GetObjectMetadataRequest request = new GetObjectMetadataRequest
					{
						BucketName = BucketName,
						Key = fileNameWithFullPath
					};
					var response = await client.GetObjectMetadataAsync(request);

					result = true;
				}
				catch (Amazon.S3.AmazonS3Exception ex)
				{
					if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
						result = false;
					else
						//status wasn't not found, so throw the exception
						throw;
				}
			}

			return result;
		}

		#endregion

		#region Put File

		/// <summary>
		/// Uploads a text file to the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path where the file will be uploaded.</param>
		/// <param name="content">The content of the file.</param>
		/// <param name="cannedACL">The canned access control list (ACL) to apply to the file. Default is the default ACL of the bucket.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the file upload was successful or not.</returns>
		public async Task<bool> S3FilePutText(string path, string content, S3CannedACL cannedACL = default)
		{
			bool result = default;

			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new PutObjectRequest
					{
						BucketName = BucketName,
						Key = path,
						ContentBody = content,
						CannedACL = cannedACL
					};
					var response = await client.PutObjectAsync(request);

					result = true;
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				result = false;
			}

			return result;
		}

		/// <summary>
		/// Uploads a byte array as a file to the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path where the file will be uploaded.</param>
		/// <param name="content">The byte array representing the file content.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the file upload was successful or not.</returns>
		public async Task<bool> S3FilePutBytes(string path, byte[] content)
		{
			bool result = default;

			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new PutObjectRequest
					{
						BucketName = BucketName,
						Key = path,
						InputStream = new MemoryStream(content)
					};
					var response = await client.PutObjectAsync(request);

					result = true;
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				result = false;
			}

			return result;
		}

		#endregion

		#region Get File

		/// <summary>
		/// Retrieves the byte array content of a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a byte array representing the file content. Returns null if the file is not found.</returns>
		public async Task<byte[]> S3FileGetBytes(string path)
		{
			byte[] result = default;

			Stream stream = await S3FileGet(path);

			if (stream != null)
				using (var memoryStream = new MemoryStream())
				{
					stream.CopyTo(memoryStream);
					result = memoryStream.ToArray();
				}

			return result;
		}

		/// <summary>
		/// Retrieves the text content of a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a string representing the file content. Returns null if the file is not found.</returns>
		public async Task<string> S3FileGetText(string path)
		{
			string result = default;

			Stream stream = await S3FileGet(path);

			if (stream != null)
				using (StreamReader reader = new StreamReader(stream))
				{
					result = reader.ReadToEnd();
				}

			return result;
		}

		/// <summary>
		/// Retrieves a stream representing the content of a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path of the file to retrieve.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a stream representing the file content. Returns null if the file is not found.</returns>
		public async Task<Stream> S3FileGet(string path)
		{
			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new GetObjectRequest()
					{
						BucketName = BucketName,
						Key = path
					};
					var response = await client.GetObjectAsync(request);

					return response.ResponseStream;
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				return null;
			}
		}

		#endregion

		#region List File

		/// <summary>
		/// Retrieves a list of file paths in the S3 bucket that match the specified path prefix.
		/// </summary>
		/// <param name="pathPrefix">The path prefix to filter the files.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a list of strings representing the file paths.</returns>
		public async Task<List<string>> S3FileList(string pathPrefix)
		{
			List<string> result = new List<string>();

			using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
			{
				ListObjectsRequest request = new ListObjectsRequest()
				{
					BucketName = BucketName,
					Prefix = pathPrefix
				};

				var response = await client.ListObjectsAsync(request);

				foreach (S3Object obj in response.S3Objects)
				{
					result.Add(obj.Key);
				}
			}

			return result;
		}

		#endregion

		#region File Tagging

		/// <summary>
		/// Converts an Amazon Tag object to a key-value pair.
		/// </summary>
		/// <param name="tag">The Amazon Tag object to convert.</param>
		/// <returns>A key-value pair representing the tag.</returns>
		public static KeyValuePair<string, string> TagObject2KeyValuePair(Tag tag)
		{
			KeyValuePair<string, string> result = default;

			if (tag != null)
			{
				result = new KeyValuePair<string, string>(tag.Key, tag.Value);
			}

			return result;
		}

		/// <summary>
		/// Retrieves the tags of a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="filePath">The path of the file to retrieve the tags for.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a dictionary of string key-value pairs representing the tags of the file. Returns null if the file is not found or has no tags.</returns>
		public async Task<Dictionary<string, string>> S3FileGetTags(string filePath)
		{
			Dictionary<string, string> result = default;

			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new GetObjectTaggingRequest()
					{
						BucketName = BucketName,
						Key = filePath
					};
					var response = await client.GetObjectTaggingAsync(request);

					if (response != null && response.Tagging != null)
					{
						result = new Dictionary<string, string>();
						foreach (var tag in response.Tagging)
						{
							result.Add(tag.Key, tag.Value);
						}
					}
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				result = default;
			}

			return result;
		}

		/// <summary>
		/// Sets the tags of a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="filePath">The path of the file to set the tags for.</param>
		/// <param name="tags">The dictionary of string key-value pairs representing the tags to set for the file.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the tag set operation was successful or not.</returns>
		public async Task<bool> S3FilePutTags(string filePath, Dictionary<string, string> tags)
		{
			bool result = default;

			// Prepare tagging object
			Tagging tagging = new Tagging() { TagSet = new List<Tag>() };
			if (tags != null && tags.Count > 0)
			{
				foreach (var item in tags)
				{
					tagging.TagSet.Add(new Tag() { Key = item.Key, Value = item.Value });
				}
			}
			// Perform the cloud call
			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new PutObjectTaggingRequest
					{
						BucketName = BucketName,
						Key = filePath,
						Tagging = tagging
					};
					var response = await client.PutObjectTaggingAsync(request);

					result = (response.HttpStatusCode == System.Net.HttpStatusCode.OK);
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				result = false;
			}

			return result;
		}

		#endregion

		#region Delete File

		/// <summary>
		/// Deletes a file at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path of the file to delete.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the file deletion was successful or not.</returns>
		public async Task<bool> S3FileDelete(string path)
		{
			bool result = default;

			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
				{
					var request = new DeleteObjectRequest
					{
						BucketName = BucketName,
						Key = path
					};
					var response = await client.DeleteObjectAsync(request);

					result = true;
				}
			}
			catch (Exception ex)
			{
				//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
				result = false;
			}

			return result;
		}

		/// <summary>
		/// Deletes a batch of files specified by their paths in the S3 bucket.
		/// </summary>
		/// <param name="paths">The list of file paths to delete.</param>
		/// <returns>A task representing the asynchronous operation. The task result is an integer representing the number of files deleted.</returns>
		public async Task<int> S3FileDeleteBatch(List<string> paths)
		{
			int result = default;

			if (paths != null && paths.Count > 0)
			{
				List<KeyVersion> objects = new List<KeyVersion>();
				foreach (var item in paths)
				{
					objects.Add(new KeyVersion() { Key = item, VersionId = default });
				}

				try
				{
					using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, RegionEndpoint))
					{
						var request = new DeleteObjectsRequest
						{
							BucketName = BucketName,
							Objects = objects
						};
						var response = await client.DeleteObjectsAsync(request);

						result = response.DeletedObjects.Count;
					}
				}
				catch (Exception ex)
				{
					//context.Logger.LogLine("Exception in PutS3Object:" + ex.Message);
					result = -1;
				}
			}

			return result;
		}

		/// <summary>
		/// Deletes all files under a folder at the specified path in the S3 bucket.
		/// </summary>
		/// <param name="path">The path of the folder to delete.</param>
		/// <returns>A task representing the asynchronous operation. The task result is an integer representing the number of files deleted.</returns>
		public async Task<int> S3FileDeleteFolder(string path)
		{
			int result = default;

			// Correct path to comply with folder presentation criteria
			if (!path.EndsWith("/"))
				path += "/";

			List<string> list = await S3FileList(path);
			result = await S3FileDeleteBatch(list);

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
		/// Finalizes an instance of the <see cref="S3"/> class.
		/// </summary>
		~S3()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
