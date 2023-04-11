using Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace amazon_acm_testbed
{
	internal class Program
	{
		//private string accessKey;
		//private string accessSecret;
		//private string caarn = "arn:aws:acm-pca:us-east-2:220531701667:certificate-authority/46916568-f54c-4d7a-8023-accc700a8c98";

		//private AmazonCertificateManagerClient getClient()
		//{
		//	accessKey = "AKIATGWFWW6RZV7QB4ES";
		//	accessSecret = "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";
		//	AmazonACMPCAConfig config = new AmazonACMPCAConfig();
		//	config.RegionEndpoint = RegionEndpoint.USEast2;
		//	IAmazonACMPCA client = new AmazonACMPCAClient(accessKey, accessSecret, config);
		//	AmazonCertificateManagerClient client1 = new AmazonCertificateManagerClient(accessKey, accessSecret, RegionEndpoint.USEast2);
		//	// test value "arn:aws:acm-pca:us-east-2:220531701667:certificate-authority/46916568-f54c-4d7a-8023-accc700a8c98";
		//	return client1;
		//}

		//public void Enroll(string csr)
		//{
		//	IssueCertificateRequest icr = new IssueCertificateRequest()
		//	{
		//		Csr = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(csr)),
		//		CertificateAuthorityArn = caarn,
		//		SigningAlgorithm = SigningAlgorithm.SHA256WITHRSA,
		//		Validity = new Validity()
		//		{
		//			Type = ValidityPeriodType.YEARS,
		//			Value = 1  // TODO - product info
		//		}
		//	};

		//	IssueCertificateResponse response = getClient().IssueCertificate(icr);
		//}

		//ACMPCAConfig config = new ACMPCAConfig();
		//config.AccessKey = "AKIATGWFWW6RZV7QB4ES";
		//	config.AccessSecret = "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";
		//	config.CAArn = "arn:aws:acm-pca:us-east-2:220531701667:certificate-authority/46916568-f54c-4d7a-8023-accc700a8c98";
		//	//config.Region = "us-east-2";
		//	config.S3Bucket = "acmtestbucket";
		private static void Main(string[] args)
		{
			ACMPCAConnector connector = new ACMPCAConnector();
			string csr = "";
			string certarn = "arn:aws:acm-pca:us-east-2:220531701667:certificate-authority/46916568-f54c-4d7a-8023-accc700a8c98/certificate/f810012f0ac381c89198369739f6cb87";
			connector.Initialize(null);
			//connector.GetSingleRecord(certarn);
			//connector.Revoke(certarn, null, 2);
			connector.Synchronize(null, null, null, new System.Threading.CancellationToken());
		}
	}
}