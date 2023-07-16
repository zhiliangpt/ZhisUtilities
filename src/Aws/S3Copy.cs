
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
	/// Represents a utility class for copying files between S3 buckets.
	/// </summary>
	public class S3Copy : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets or sets the AWS access key ID.
		/// </summary>
		public string AwsAccessKeyId { get; set; }

		/// <summary>
		/// Gets or sets the AWS secret access key.
		/// </summary>
		public string AwsSecretAccessKey { get; set; }

		/// <summary>
		/// Gets or sets the source region endpoint.
		/// </summary>
		public RegionEndpoint SourceRegionEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the source bucket name.
		/// </summary>
		public string SourceBucketName { get; set; }

		/// <summary>
		/// Gets or sets the destination region endpoint.
		/// </summary>
		public RegionEndpoint DestinationRegionEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the destination bucket name.
		/// </summary>
		public string DestinationBucketName { get; set; }
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="S3Copy"/> class with the specified AWS access key ID, AWS secret access key, source region endpoint, source bucket name, destination region endpoint, and destination bucket name.
		/// </summary>
		/// <param name="awsAccessKeyId">The AWS access key ID.</param>
		/// <param name="awsSecretAccessKey">The AWS secret access key.</param>
		/// <param name="sourceRegionEndpoint">The source region endpoint.</param>
		/// <param name="sourceBucketName">The source bucket name.</param>
		/// <param name="destinationRegionEndpoint">The destination region endpoint.</param>
		/// <param name="destinationBucketName">The destination bucket name.</param>
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

		/// <summary>
		/// Copies a file from the source S3 bucket to the destination S3 bucket.
		/// </summary>
		/// <param name="sourceFilePath">The path of the file to copy from the source bucket.</param>
		/// <param name="destinationFilePath">The path of the file to copy to in the destination bucket.</param>
		/// <param name="cannedACL">The canned access control list (ACL) to apply to the copied file. Default is the default ACL of the destination bucket.</param>
		/// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the file copy was successful or not.</returns>
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

		/// <summary>
		/// Copies a folder and its files from the source S3 bucket to the destination S3 bucket.
		/// </summary>
		/// <param name="sourceFolderPath">The path of the folder to copy from the source bucket.</param>
		/// <param name="destinationFolderPath">The path of the folder to copy to in the destination bucket.</param>
		/// <param name="cannedACL">The canned access control list (ACL) to apply to the copied files. Default is the default ACL of the destination bucket.</param>
		/// <returns>A task representing the asynchronous operation. The task result is an integer representing the number of files copied.</returns>
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
		/// Finalizes an instance of the <see cref="S3Copy"/> class.
		/// </summary>
		~S3Copy()
		{
			// cleans only unmanaged stuffs
			ReleaseResources(false);
		}
		#endregion
	}
}
