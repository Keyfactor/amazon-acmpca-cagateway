﻿// Copyright 2022 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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