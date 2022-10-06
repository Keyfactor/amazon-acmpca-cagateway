using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA
{
	public static class ACMPCAConstants
	{
		public static Dictionary<string, string> TemplateARNs = new Dictionary<string, string>
		{
			{
				"endentity",
				"arn:aws:acm-pca:::template/EndEntityCertificate/V1"
			},
			{
				"endentityclientauth",
				"arn:aws:acm-pca:::template/EndEntityClientAuthCertificate/V1"
			},
			{
				"endentityserverauth",
				"arn:aws:acm-pca:::template/EndEntityServerAuthCertificate/V1"
			}
		};

		public static string ACCESS_KEY = "AccessKey";
		public static string ACCESS_SECRET = "AccessSecret";
		public static string REGION = "Region";
		public static string CAARN = "CAARN";
		public static string S3_BUCKET = "S3Bucket";
	}
}