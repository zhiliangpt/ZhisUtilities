
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
	public class S3Copy : IDisposable
	{
		#region Properties
		public string AwsAccessKeyId { get; set; }
		public string AwsSecretAccessKey { get; set; }
		public RegionEndpoint SourceRegionEndpoint { get; set; }
		public string SourceBucketName { get; set; }
		public RegionEndpoint DestinationRegionEndpoint { get; set; }
		public string DestinationBucketName { get; set; }
		#endregion

		public S3Copy(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint sourceRegionEndpoint, string sourceBucketName, RegionEndpoint destinationRegionEndpoint, string destinationBucketName)
		{
			AwsAccessKeyId = awsAccessKeyId;
			AwsSecretAccessKey = awsSecretAccessKey;
			SourceRegionEndpoint = sourceRegionEndpoint;
			SourceBucketName = sourceBucketName;
			DestinationRegionEndpoint = destinationRegionEndpoint;
			DestinationBucketName = destinationBucketName;
		}

		#region Copy File

		public async Task<bool> S3CopyFile(string sourceFilePath, string destinationFilePath, S3CannedACL cannedACL = default)
		{
			bool result = default;

			try
			{
				using (IAmazonS3 client = new AmazonS3Client(AwsAccessKeyId, AwsSecretAccessKey, DestinationRegionEndpoint))
				{
					var request = new CopyObjectRequest
					{
						SourceBucket = SourceBucketName,
						SourceKey = sourceFilePath,
						DestinationBucket = DestinationBucketName,
						DestinationKey = destinationFilePath
					};

					if (cannedACL != default)
					{
						request.CannedACL = cannedACL;
					}

					var response = await client.CopyObjectAsync(request);

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

		public async Task<int> S3CopyFolder(string sourceFolderPath, string destinationFolderPath, S3CannedACL cannedACL = default)
		{
			int result = default;

			// Correct path to comply with folder presentation criteria
			if (!sourceFolderPath.EndsWith("/"))
				sourceFolderPath += "/";
			if (!destinationFolderPath.EndsWith("/"))
				destinationFolderPath += "/";

			List<string> sourceList = default;
			using (var s3 = new S3(AwsAccessKeyId, AwsSecretAccessKey, SourceRegionEndpoint, SourceBucketName))
			{
				sourceList = await s3.S3FileList(sourceFolderPath);
			}

			if (sourceList != null)
				foreach (var sourceItem in sourceList)
				{
					string destinationItem = destinationFolderPath + sourceItem.Substring(sourceFolderPath.Length);
					if (await S3CopyFile(sourceItem, destinationItem, cannedACL))
					{
						result++;
					}
				}

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
		~S3Copy()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
