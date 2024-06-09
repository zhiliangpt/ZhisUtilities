using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace Zhis.Utilities.Aws
{
	/// <summary>
	/// Represents a class for interacting with Amazon Simple Email Service (SES).
	/// </summary>
	public class Ses : IDisposable
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
		/// Gets or sets the AWS region endpoint for the SES service.
		/// </summary>
		public RegionEndpoint RegionEndpoint { get; set; }

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Ses"/> class with the specified AWS credentials and SES configuration.
		/// </summary>
		/// <param name="awsAccessKeyId">The AWS access key ID used for authentication.</param>
		/// <param name="awsSecretAccessKey">The AWS secret access key used for authentication.</param>
		/// <param name="regionEndpoint">The AWS region endpoint for the SES service.</param>
		public Ses(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint regionEndpoint)
		{
			AwsAccessKeyId = awsAccessKeyId;
			AwsSecretAccessKey = awsSecretAccessKey;
			RegionEndpoint = regionEndpoint;
		}

		#region SES

		/// <summary>
		/// Sends an email using Amazon SES.
		/// </summary>
		/// <param name="senderAddress">The email address of the sender.</param>
		/// <param name="receiverAddress">The email address of the receiver.</param>
		/// <param name="subject">The subject of the email.</param>
		/// <param name="htmlBody">The HTML body of the email.</param>
		/// <param name="textBody">The plain text body of the email.</param>
		/// <returns>A boolean indicating whether the email was sent successfully.</returns>
		public async Task<SesSendResult> SesSend(string senderAddress, string receiverAddress, string subject, string htmlBody, string textBody)
		{
			SesSendResult result = new SesSendResult();

			using (var client = new AmazonSimpleEmailServiceClient(this.AwsAccessKeyId, this.AwsSecretAccessKey, this.RegionEndpoint))
			{
				var sendRequest = new SendEmailRequest
				{
					Source = senderAddress,
					Destination = new Destination
					{
						ToAddresses =
						new List<string> { receiverAddress }
					},
					Message = new Message
					{
						Subject = new Content(subject),
						Body = new Body
						{
							Html = new Content
							{
								Charset = "UTF-8",
								Data = htmlBody
							},
							Text = new Content
							{
								Charset = "UTF-8",
								Data = textBody
							}
						}
					},
				};
				try
				{
					var response = await client.SendEmailAsync(sendRequest);
					result.IsSuccessful = true;
				}
				catch (Exception ex)
				{
					result.Exception = ex.ToString();
				}
			}

			return result;
		}

		#endregion

		#region Disposing

		// Has Dispose() already been called?
		bool isDisposed = false;

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged and managed resources.
		/// </summary>
		public void Dispose()
		{
			ReleaseResources(true); // cleans both unmanaged and managed resources
			GC.SuppressFinalize(this); // suppress finalization
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
				// TODO: Release unmanaged resources here
				// ...
			}
			isDisposed = true; // Dispose() can be called numerous times
		}

		// Use C# destructor syntax for finalization code, invoked by GC only.
		/// <summary>
		/// Finalizes an instance of the <see cref="Ses"/> class.
		/// </summary>
		~Ses()
		{
			// cleans only unmanaged stuff
			ReleaseResources(false);
		}

		#endregion
	}

	public struct SesSendResult
	{
		public bool IsSuccessful { get; set; }
		public string Exception { get; set; }
	}
}
