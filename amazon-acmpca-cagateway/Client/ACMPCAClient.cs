// Copyright 2022 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
			request.CertificateAuthorityArn = Config.CAArn;
			request.SigningAlgorithm = SigningAlgorithm.SHA256WITHRSA;
			var response = GetPCAClient().IssueCertificate(request);
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return response.CertificateArn;
		}

		public CAConnectorCertificate GetCertificateByRequestID(string requestId)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
			string certArn = $"{Config.CAArn}/certificate/{requestId}";
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return GetCertificateByARN(certArn);
		}

		public CAConnectorCertificate GetCertificateByARN(string certARN)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
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
			catch (RequestInProgressException rip)
			{
				// If request is still in progress, wait a second and try again
				Thread.Sleep(1000);
				return GetCertificateByARN(certARN);
			}
			catch (AmazonACMPCAException aex)
			{
				Logger.Error($"Error retrieving certificate: {aex.Message}");
				throw;
			}
			if (string.IsNullOrEmpty(getCertificateResponse.Certificate))
			{
				Logger.Error($"Certificate with ARN {certARN} not found.");
				throw new Exception($"Certificate with ARN {certARN} not found.");
			}
			X509Certificate2 cert = CertificateConverterFactory.FromPEM(getCertificateResponse.Certificate).ToX509Certificate2();

			bool server = false, client = false;
			string product = "EndEntity";
			// Look at enhanced key usage OIDs to determine if the cert is ServerAuth, ClientAuth, or both
			try
			{
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
			}
			catch (Exception)
			{
				// If ENHANCED_KEY_USAGE_OID is not preset, its a different key usage (most likely a CA cert)
				product = "Unknown";
			}

			if (server && !client)
			{
				product += "ServerAuth";
			}
			else if (client && !server)
			{
				product += "ClientAuth";
			}
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return new CAConnectorCertificate
			{
				CARequestID = certARN.Split('/')[3],
				Certificate = !string.IsNullOrEmpty(getCertificateResponse.Certificate) ? ConfigurationUtils.OnlyBase64CertContent(getCertificateResponse.Certificate) : null,
				Status = 20,
				ProductID = product,
				SubmissionDate = cert.NotBefore.ToUniversalTime()
			};
		}

		public void VerifyCAConnection()
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
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
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
		}

		public int RevokeCertificate(RevokeCertificateRequest request)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
			request.CertificateAuthorityArn = Config.CAArn;
			var _ = GetPCAClient().RevokeCertificate(request);
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return (int)RequestDisposition.REVOKED;
		}

		public List<ACMPCACertificate> GetAuditReport()
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
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
				Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
				return JsonConvert.DeserializeObject<List<ACMPCACertificate>>(respStr);
			}
		}

		private IAmazonACMPCA GetPCAClient()
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
			IAmazonACMPCA client = new AmazonACMPCAClient(Config.AccessKey, Config.AccessSecret, Config.GetRegion());
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return client;
		}

		private IAmazonS3 GetS3Client()
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
			string region = "";
			using (IAmazonS3 tempClient = new AmazonS3Client(Config.AccessKey, Config.AccessSecret, Config.GetRegion()))
			{
				var bucketResponse = tempClient.GetBucketLocation(Config.S3Bucket);
				region = bucketResponse.Location.Value;
			}
			var s3Client = new AmazonS3Client(Config.AccessKey, Config.AccessSecret, RegionEndpoint.GetBySystemName(region));
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
			return s3Client;
		}
	}
}