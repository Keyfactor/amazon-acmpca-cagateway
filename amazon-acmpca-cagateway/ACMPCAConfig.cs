// Copyright 2022 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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