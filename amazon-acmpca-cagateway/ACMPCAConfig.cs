using Amazon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA
{
	public class ACMPCAConfig
	{
		public string AccessKey { get; set; }
		public string AccessSecret { get; set; }
		public string CAArn { get; set; }
		public string S3Bucket { get; set; }

		public RegionEndpoint GetRegion()
		{
			string[] arnParts = CAArn.Split(':');
			string region = arnParts[3];
			return RegionEndpoint.GetBySystemName(region);
		}
	}
}