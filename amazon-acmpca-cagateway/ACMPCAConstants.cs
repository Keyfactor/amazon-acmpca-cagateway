// Copyright 2022 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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