using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA
{
	public class ACMPCACertificate
	{
		[JsonProperty("awsAccountId")]
		public string AccountId { get; set; }

		[JsonProperty("certificateArn")]
		public string CertificateARN { get; set; }

		[JsonProperty("serial")]
		public string SerialNumber { get; set; }

		[JsonProperty("subject")]
		public string Subject { get; set; }

		[JsonProperty("notBefore")]
		public DateTime? NotBefore { get; set; }

		[JsonProperty("notAfter")]
		public DateTime? NotAfter { get; set; }

		[JsonProperty("issuedAt")]
		public DateTime? IssuedDate { get; set; }

		[JsonProperty("revokedAt")]
		public DateTime? RevocationDate { get; set; }

		[JsonProperty("revocationReason")]
		public string RevocationReason { get; set; }

		[JsonProperty("templateArn")]
		public string TemplateARN { get; set; }
	}
}