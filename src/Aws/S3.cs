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
	public class S3 : IDisposable
	{
		#region Properties
		public string AwsAccessKeyId { get; set; }
		public string AwsSecretAccessKey { get; set; }
		public RegionEndpoint RegionEndpoint { get; set; }
		public string BucketName { get; set; }
		#endregion

		public S3(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint regionEndpoint, string bucketName)
		{
			AwsAccessKeyId = awsAccessKeyId;
			AwsSecretAccessKey = awsSecretAccessKey;
			RegionEndpoint = regionEndpoint;
			BucketName = bucketName;
		}

		#region Exist File

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
		/// Converts a single Amazon Tag object to a key-value pair.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public static KeyValuePair<string, string> TagObject2KeyValuePair(Tag tag)
		{
			KeyValuePair<string, string> result = default;

			if (tag != null)
			{
				result = new KeyValuePair<string, string>(tag.Key, tag.Value);
			}

			return result;
		}

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
		// Implement IDisposable.
		public void Dispose()
		{
			ReleaseResources(true); // cleans both unmanaged and managed resources
			GC.SuppressFinalize(this); // supress finalization
		}

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
		~S3()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
