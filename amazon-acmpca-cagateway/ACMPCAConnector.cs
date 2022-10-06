// Copyright 2022 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using Amazon;
using Amazon.ACMPCA;
using Amazon.ACMPCA.Model;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;

using CAProxy.AnyGateway;
using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CAProxy.AnyGateway.Models.Configuration;
using CAProxy.Common;

using CSS.PKI;

using Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA.Client;

using Newtonsoft.Json;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static Keyfactor.PKI.PKIConstants.Microsoft;

using RevocationReason = Amazon.ACMPCA.RevocationReason;

namespace Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA
{
	public class ACMPCAConnector : BaseCAConnector, ICAConnectorConfigInfoProvider
	{
		#region Fields and Constructors

		private ACMPCAClient Client { get; set; }

		#endregion Fields and Constructors

		#region ICAConnector Methods

		public override void Initialize(ICAConnectorConfigProvider configProvider)
		{
			string rawconfig = JsonConvert.SerializeObject(configProvider.CAConnectionData);
			ACMPCAConfig config = JsonConvert.DeserializeObject<ACMPCAConfig>(rawconfig);

			Client = new ACMPCAClient(config);
		}

		/// <summary>
		/// Enrolls for a certificate through the ACM PCA API.
		/// </summary>
		/// <param name="certificateDataReader">Reads certificate data from the database.</param>
		/// <param name="csr">The certificate CSR in PEM format.</param>
		/// <param name="subject">The subject of the certificate request.</param>
		/// <param name="san">Any sans added to the request.</param>
		/// <param name="productInfo">Information about the CA product type.</param>
		/// <param name="requestFormat">The format of the request.</param>
		/// <param name="enrollmentType">The type of the enrollment, i.e. new, renew, or reissue.</param>
		/// <returns></returns>
		public override EnrollmentResult Enroll(ICertificateDataReader certificateDataReader, string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
		{
			IssueCertificateRequest issueRequest = new IssueCertificateRequest
			{
				Csr = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(csr))
			};

			var days = (productInfo.ProductParameters.ContainsKey("LifetimeDays")) ? int.Parse(productInfo.ProductParameters["LifetimeDays"]) : 365;
			issueRequest.Validity = new Validity()
			{
				Type = ValidityPeriodType.DAYS,
				Value = days
			};

			issueRequest.TemplateArn = ACMPCAConstants.TemplateARNs[productInfo.ProductID.ToLower()];

			string certArn = Client.RequestCertificate(issueRequest);

			CAConnectorCertificate cert = Client.GetCertificateByARN(certArn);

			return new EnrollmentResult()
			{
				CARequestID = certArn,
				Certificate = cert.Certificate,
				Status = cert.Status,
				StatusMessage = "Certificate Issued"
			};
		}

		/// <summary>
		/// Returns a single certificate record by its ARN.
		/// </summary>
		/// <param name="caRequestID">The CA request ID for the certificate (presently the ARN).</param>
		/// <returns></returns>
		public override CAConnectorCertificate GetSingleRecord(string caRequestID)
		{
			CAConnectorCertificate cert = Client.GetCertificateByARN(caRequestID);
			return cert;
		}

		/// <summary>
		/// Attempts to reach the CA over the network.
		/// </summary>
		public override void Ping()
		{
			Client.VerifyCAConnection();
		}

		/// <summary>
		/// Revokes a certificate by its ARN.
		/// </summary>
		/// <param name="caRequestID">The CA request ID (presently the ARN).</param>
		/// <param name="hexSerialNumber">The hex-encoded serial number.</param>
		/// <param name="revocationReason">The revocation reason.</param>
		public override int Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
		{
			string serialNum = caRequestID.Substring(caRequestID.LastIndexOf('/') + 1);

			RevokeCertificateRequest revokeCertificateRequest = new RevokeCertificateRequest()
			{
				CertificateSerial = serialNum
			};

			switch (revocationReason)
			{
				case 1:
					revokeCertificateRequest.RevocationReason = RevocationReason.KEY_COMPROMISE;
					break;

				case 2:
					revokeCertificateRequest.RevocationReason = RevocationReason.CERTIFICATE_AUTHORITY_COMPROMISE;
					break;

				case 3:
					revokeCertificateRequest.RevocationReason = RevocationReason.AFFILIATION_CHANGED;
					break;

				case 4:
					revokeCertificateRequest.RevocationReason = RevocationReason.SUPERSEDED;
					break;

				case 5:
					revokeCertificateRequest.RevocationReason = RevocationReason.CESSATION_OF_OPERATION;
					break;

				case 9:
					revokeCertificateRequest.RevocationReason = RevocationReason.PRIVILEGE_WITHDRAWN;
					break;

				case 10:
					revokeCertificateRequest.RevocationReason = RevocationReason.A_A_COMPROMISE;
					break;

				default:
					revokeCertificateRequest.RevocationReason = RevocationReason.UNSPECIFIED;
					break;
			}
			return Client.RevokeCertificate(revokeCertificateRequest);
		}

		/// <summary>
		/// Synchronizes the gateway with the external CA.
		/// </summary>
		/// <param name="certificateDataReader">Provides information about the gateway's certificates.</param>
		/// <param name="blockingBuffer">Buffer into which certificates are placed from the CA.</param>
		/// <param name="certificateAuthoritySyncInfo">Information about the last CA sync.</param>
		/// <param name="cancelToken">The cancellation token.</param>
		public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CAConnectorCertificate> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken)
		{
			var certs = Client.GetAuditReport();

			foreach (var cert in certs)
			{
				CAConnectorCertificate dbCert = certificateDataReader.GetCertificateRecord(cert.CertificateARN, string.Empty);
				int status = 20;
				if (cert.RevocationDate.HasValue)
				{
					status = 21;
				}

				if (certificateAuthoritySyncInfo.DoFullSync || dbCert == null || dbCert.Status != status) // Only sync if the cert is new, or the status has changed (or if doing a full sync)
				{
					if (dbCert == null) dbCert = new CAConnectorCertificate();
					CAConnectorCertificate pcaCert = Client.GetCertificateByARN(cert.CertificateARN);

					dbCert.CARequestID = cert.CertificateARN;
					dbCert.Certificate = pcaCert.Certificate;
					dbCert.Status = status;
					if (status == (int)RequestDisposition.REVOKED)
					{
						dbCert.RevocationDate = cert.RevocationDate.Value;
						dbCert.RevocationReason = GetRevocationReasonCodeFromString(cert.RevocationReason);
					}
					dbCert.ProductID = pcaCert.ProductID;
					dbCert.SubmissionDate = pcaCert.SubmissionDate;

					blockingBuffer.Add(dbCert);
				}
			}
		}

		/// <summary>
		/// Validates that the CA connection info is correct.
		/// </summary>
		/// <param name="connectionInfo">The information used to connect to the CA.</param>
		public override void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
		{
			List<string> errors = new List<string>();

			string accessKey = connectionInfo.ContainsKey(ACMPCAConstants.ACCESS_KEY) ? (string)connectionInfo[ACMPCAConstants.ACCESS_KEY] : string.Empty;
			if (string.IsNullOrWhiteSpace(accessKey))
			{
				errors.Add("AccessKey is required");
			}

			string accessSecret = connectionInfo.ContainsKey(ACMPCAConstants.ACCESS_SECRET) ? (string)connectionInfo[ACMPCAConstants.ACCESS_SECRET] : string.Empty;
			if (string.IsNullOrWhiteSpace(accessSecret))
			{
				errors.Add("AccessSecret is required");
			}

			string region = connectionInfo.ContainsKey(ACMPCAConstants.REGION) ? (string)connectionInfo[ACMPCAConstants.REGION] : string.Empty;
			if (string.IsNullOrWhiteSpace(region))
			{
				errors.Add("Region is required");
			}
			else
			{
				var regionEndpoint = RegionEndpoint.GetBySystemName(region);
				if (regionEndpoint.DisplayName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
				{
					errors.Add("Unknown region specified");
				}
			}

			string caarn = connectionInfo.ContainsKey(ACMPCAConstants.CAARN) ? (string)connectionInfo[ACMPCAConstants.CAARN] : string.Empty;
			if (string.IsNullOrWhiteSpace(caarn))
			{
				errors.Add("CAARN is required");
			}

			string s3 = connectionInfo.ContainsKey(ACMPCAConstants.S3_BUCKET) ? (string)connectionInfo[ACMPCAConstants.S3_BUCKET] : string.Empty;
			if (string.IsNullOrWhiteSpace(s3))
			{
				errors.Add("S3Bucket is required");
			}

			ACMPCAConfig config = new ACMPCAConfig()
			{
				AccessKey = accessKey,
				AccessSecret = accessSecret,
				//Region = region,
				CAArn = caarn,
				S3Bucket = s3
			};
			ACMPCAClient client = new ACMPCAClient(config);
			try
			{
				client.VerifyCAConnection();
			}
			catch
			{
				errors.Add("Unable to connect to ACM PCA");
			}

			if (errors.Any())
			{
				ThrowValidationException(errors);
			}
		}

		/// <summary>
		/// Validates that the product information for the CA is correct.
		/// </summary>
		/// <param name="productInfo">The product information.</param>
		public override void ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
		{
			try
			{
				var template = ACMPCAConstants.TemplateARNs[productInfo.ProductID.ToLower()];
				if (string.IsNullOrEmpty(template))
				{
					throw new Exception("ProductID not recognized.");
				}
			}
			catch (Exception ex)
			{
				throw new Exception("ProductID not recognized.", ex);
			}
		}

		#region Obsolete Methods

		public override EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
		{
			throw new NotImplementedException();
		}

		public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CertificateRecord> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken, string logicalName)
		{
			throw new NotImplementedException();
		}

		#endregion Obsolete Methods

		#endregion ICAConnector Methods

		#region ICAConnectorConfigInfoProvider Methods

		/// <summary>
		/// Returns the default CA connector section of the config file.
		/// </summary>
		public Dictionary<string, object> GetDefaultCAConnectorConfig()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the default comment on the default product type.
		/// </summary>
		/// <returns></returns>
		public string GetProductIDComment()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets annotations for the CA connector properties.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, PropertyConfigInfo> GetCAConnectorAnnotations()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets annotations for the template mapping parameters.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, PropertyConfigInfo> GetTemplateParameterAnnotations()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets default template map parameters for ACM PCA product types.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string> GetDefaultTemplateParametersConfig()
		{
			throw new NotImplementedException();
		}

		#endregion ICAConnectorConfigInfoProvider Methods

		#region Helper Methods

		private void ThrowValidationException(List<string> errors)
		{
			throw new Exception(string.Join("\n", errors));
		}

		private int GetRevocationReasonCodeFromString(string reasonString)
		{
			if (reasonString.Equals(RevocationReason.KEY_COMPROMISE, StringComparison.OrdinalIgnoreCase))
			{
				return 1;
			}
			else if (reasonString.Equals(RevocationReason.CERTIFICATE_AUTHORITY_COMPROMISE, StringComparison.OrdinalIgnoreCase))
			{
				return 2;
			}
			else if (reasonString.Equals(RevocationReason.AFFILIATION_CHANGED, StringComparison.OrdinalIgnoreCase))
			{
				return 3;
			}
			else if (reasonString.Equals(RevocationReason.SUPERSEDED, StringComparison.OrdinalIgnoreCase))
			{
				return 4;
			}
			else if (reasonString.Equals(RevocationReason.CESSATION_OF_OPERATION, StringComparison.OrdinalIgnoreCase))
			{
				return 5;
			}
			else if (reasonString.Equals(RevocationReason.PRIVILEGE_WITHDRAWN))
			{
				return 9;
			}
			else if (reasonString.Equals(RevocationReason.A_A_COMPROMISE))
			{
				return 10;
			}
			else
			{
				return 0; // Unspecified
			}
		}

		#endregion Helper Methods
	}
}