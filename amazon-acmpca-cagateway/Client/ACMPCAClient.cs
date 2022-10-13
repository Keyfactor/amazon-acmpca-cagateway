using Amazon;
using Amazon.ACMPCA;
using Amazon.ACMPCA.Model;
using Amazon.S3;
using Amazon.S3.Model;

using CAProxy.AnyGateway.Models;
using CAProxy.Common.Config;

using CSS.Common.Logging;

using Keyfactor.PKI.X509;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static Keyfactor.PKI.PKIConstants.Microsoft;

namespace Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA.Client
{
	public class ACMPCAClient : LoggingClientBase
	{
		private const string ENHANCED_KEY_USAGE_OID = "2.5.29.37";
		private const string SERVER_AUTH_OID = "1.3.6.1.5.5.7.3.1";
		private const string CLIENT_AUTH_OID = "1.3.6.1.5.5.7.3.2";

		public ACMPCAClient(ACMPCAConfig config)
		{
			Config = config;
		}

		private ACMPCAConfig Config { get; set; }

		public string RequestCertificate(IssueCertificateRequest request)
		{
			request.CertificateAuthorityArn = Config.CAArn;
			request.SigningAlgorithm = SigningAlgorithm.SHA256WITHRSA;
			var response = GetPCAClient().IssueCertificate(request);
			// Amazon API will give an error if attempting to retrieve the issued certificate before it has completed
			Thread.Sleep(100);
			return response.CertificateArn;
		}

		public CAConnectorCertificate GetCertificateByARN(string certARN)
		{
			GetCertificateRequest getCertificateRequest = new GetCertificateRequest()
			{
				CertificateArn = certARN,
				CertificateAuthorityArn = Config.CAArn
			};
			GetCertificateResponse getCertificateResponse;
			try
			{
				getCertificateResponse = GetPCAClient().GetCertificate(getCertificateRequest);
			}
			catch (AmazonACMPCAException aex)
			{
				Logger.Error($"Error retrieving certificate: {aex.Message}");
				throw;
			}
			if (string.IsNullOrEmpty(getCertificateResponse.Certificate))
			{
				Logger.Error($"Certificate with ARN {certARN} not found.");
			}
			X509Certificate2 cert = CertificateConverterFactory.FromPEM(getCertificateResponse.Certificate).ToX509Certificate2();

			bool server = false, client = false;

			// Look at enhanced key usage OIDs to determine if the cert is ServerAuth, ClientAuth, or both
			var enhKeyUsage = cert.Extensions[ENHANCED_KEY_USAGE_OID] as X509EnhancedKeyUsageExtension;
			foreach (var usage in enhKeyUsage.EnhancedKeyUsages)
			{
				if (usage.Value.Equals(SERVER_AUTH_OID))
				{
					server = true;
				}
				else if (usage.Value.Equals(CLIENT_AUTH_OID))
				{
					client = true;
				}
			}
			string product = "EndEntity";
			if (server && !client)
			{
				product += "ServerAuth";
			}
			else if (client && !server)
			{
				product += "ClientAuth";
			}

			return new CAConnectorCertificate
			{
				CARequestID = certARN,
				Certificate = !string.IsNullOrEmpty(getCertificateResponse.Certificate) ? ConfigurationUtils.OnlyBase64CertContent(getCertificateResponse.Certificate) : null,
				Status = 20,
				ProductID = product,
				SubmissionDate = cert.NotBefore.ToUniversalTime()
			};
		}

		public void VerifyCAConnection()
		{
			try
			{
				var request = new DescribeCertificateAuthorityRequest()
				{
					CertificateAuthorityArn = Config.CAArn
				};
				var ca = GetPCAClient().DescribeCertificateAuthority(request);
				if (ca.CertificateAuthority.Status != CertificateAuthorityStatus.ACTIVE)
				{
					Logger.Error("Amazon PCA is not in the ACTIVE state");
					throw new Exception("Cannot communicate with given CA, CA is not in the ACTIVE state");
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Unable to communicate with Amazon CA: {ex.Message}");
				throw;
			}
		}

		public int RevokeCertificate(RevokeCertificateRequest request)
		{
			request.CertificateAuthorityArn = Config.CAArn;
			var _ = GetPCAClient().RevokeCertificate(request);
			return (int)RequestDisposition.REVOKED;
		}

		public List<ACMPCACertificate> GetAuditReport()
		{
			CreateCertificateAuthorityAuditReportRequest request = new CreateCertificateAuthorityAuditReportRequest()
			{
				CertificateAuthorityArn = Config.CAArn,
				S3BucketName = Config.S3Bucket,
				AuditReportResponseFormat = AuditReportResponseFormat.JSON
			};
			var response = GetPCAClient().CreateCertificateAuthorityAuditReport(request);

			GetObjectRequest reportRequest = new GetObjectRequest()
			{
				BucketName = Config.S3Bucket,
				Key = response.S3Key
			};
			using (var s3Client = GetS3Client())
			using (var reportResponse = s3Client.GetObject(reportRequest))
			using (Stream responseStream = reportResponse.ResponseStream)
			using (StreamReader reader = new StreamReader(responseStream))
			{
				string respStr = reader.ReadToEnd();
				return JsonConvert.DeserializeObject<List<ACMPCACertificate>>(respStr);
			}
		}

		private IAmazonACMPCA GetPCAClient()
		{
			IAmazonACMPCA client = new AmazonACMPCAClient(Config.AccessKey, Config.AccessSecret, Config.GetRegion());
			return client;
		}

		private IAmazonS3 GetS3Client()
		{
			string region = "";
			using (IAmazonS3 tempClient = new AmazonS3Client(Config.AccessKey, Config.AccessSecret, Config.GetRegion()))
			{
				var bucketResponse = tempClient.GetBucketLocation(Config.S3Bucket);
				region = bucketResponse.Location.Value;
			}
			var s3Client = new AmazonS3Client(Config.AccessKey, Config.AccessSecret, RegionEndpoint.GetBySystemName(region));
			return s3Client;
		}
	}
}