# Amazon ACM PCA CA AnyGateway

This integration allows for the Synchronization, Enrollment, and Revocation of certificates from Amazon Certificate Manager Private CA.

#### Integration status: Prototype - Demonstration quality. Not for use in customer environments.

## About the Keyfactor AnyGateway CA Connector

This repository contains an AnyGateway CA Connector, which is a plugin to the Keyfactor AnyGateway. AnyGateway CA Connectors allow Keyfactor Command to be used for inventory, issuance, and revocation of certificates from a third-party certificate authority.

---





---

# Introduction
This AnyGateway plug-in enables issuance, revocation, and synchronization of certificates from Amazon's AWS Certificate Manager Private CA
Note that this gateway is specific to Private CAs, and will not work against other AWS CAs.

# Prerequisites for Installation

## AnyGateway Platform Minimum Version
The ACMPCA AnyGateway requires the Keyfactor AnyGateway v21.5.1 or newer

## Certificate Chain

In order to enroll for certificates the Keyfactor Command server must trust the trust chain. Once you create your Root and/or Subordinate CA, make sure to import the certificate chain into the AnyGateway and Command Server certificate store


# Install
* Download latest successful build from [GitHub Releases](../../releases/latest)

* Copy AmazonACMPCAGateway.dll to the Program Files\Keyfactor\Keyfactor AnyGateway directory

* Copy all of the AWSSDK DLLs to the Program Files\Keyfactor\Keyfactor AnyGateway directory

* Update the CAProxyServer.config file
  * Update the CAConnection section to point at the ACMPCAConnector class
  ```xml
  <alias alias="CAConnector" type="Keyfactor.Extensions.AnyGateway.Amazon.ACMPCA.ACMPCAConnector, AmazonACMPCAGateway"/>
  ```

# Configuration
The following sections will breakdown the required configurations for the AnyGatewayConfig.json file that will be imported to configure the AnyGateway.

## Templates
The Template section will map the CA's products to an AD template.
* ```ProductID```
This is the ID of the ACM PCA product to map to the specified template.

Currently supported options:
EndEntity
EndEntityClientAuth
EndEntityServerAuth

* ```LifetimeDays```
OPTIONAL: The number of days of validity to use when requesting certs. If not provided, default is 365

 ```json
  "Templates": {
	"WebServer": {
      "ProductID": "EndEntity",
      "Parameters": {
		"LifetimeDays":"365"
      }
   }
}
 ```
 
## Security
The security section does not change specifically for the ACM PCA Gateway.  Refer to the AnyGateway Documentation for more detail.
```json
  /*Grant permissions on the CA to users or groups in the local domain.
	READ: Enumerate and read contents of certificates.
	ENROLL: Request certificates from the CA.
	OFFICER: Perform certificate functions such as issuance and revocation. This is equivalent to "Issue and Manage" permission on the Microsoft CA.
	ADMINISTRATOR: Configure/reconfigure the gateway.
	Valid permission settings are "Allow", "None", and "Deny".*/
    "Security": {
        "Keyfactor\\Administrator": {
            "READ": "Allow",
            "ENROLL": "Allow",
            "OFFICER": "Allow",
            "ADMINISTRATOR": "Allow"
        },
        "Keyfactor\\gateway_test": {
            "READ": "Allow",
            "ENROLL": "Allow",
            "OFFICER": "Allow",
            "ADMINISTRATOR": "Allow"
        },		
        "Keyfactor\\SVC_TimerService": {
            "READ": "Allow",
            "ENROLL": "Allow",
            "OFFICER": "Allow",
            "ADMINISTRATOR": "None"
        },
        "Keyfactor\\SVC_AppPool": {
            "READ": "Allow",
            "ENROLL": "Allow",
            "OFFICER": "Allow",
            "ADMINISTRATOR": "Allow"
        }
    }
```
## CerificateManagers
The Certificate Managers section is optional.
	If configured, all users or groups granted OFFICER permissions under the Security section
	must be configured for at least one Template and one Requester. 
	Uses "<All>" to specify all templates. Uses "Everyone" to specify all requesters.
	Valid permission values are "Allow" and "Deny".
```json
  "CertificateManagers":{
		"DOMAIN\\Username":{
			"Templates":{
				"MyTemplateShortName":{
					"Requesters":{
						"Everyone":"Allow",
						"DOMAIN\\Groupname":"Deny"
					}
				},
				"<All>":{
					"Requesters":{
						"Everyone":"Allow"
					}
				}
			}
		}
	}
```
## CAConnection
The CA Connection section will determine the API endpoint and configuration data used to connect to the ACM PCA. 
* ```AccessKey```
This is the access key to use to connect to the ACM API.
* ```AccessSecret```
This is the secret to use with the corresponding access key to connect to the ACM API
* ```CAARN```
This is the Amazon Resource Name (ARN) of the CA in AWS. This can be found in the list of private CAs in your Amazon account.
The ARN will be of a form similar to: "arn:aws:acm-pca:region:account:certificate-authority/GUID"
* ```S3Bucket```
Since the ACM PCA API does not have direct inventory capabilities, the gateway performs an inventory by generating an audit report and then parsing that report.
The audit reports themselves need to be stored in an S3 Bucket in the Amazon account. The name of the bucket you wish to use should go here.
Note: Make sure that the account being used with the accesskey/secret has read/write permissions to that S3 bucket.

```json
  "CAConnection": {
	"AccessKey" : "ACM Access Key",
    "AccessSecret": "ACM Access Secret",
    "CAARN": "arn:aws:acm-pca:region:account:certificate-authority/GUID",
    "S3Bucket": "bucketname"
  },
```
## GatewayRegistration
There are no specific Changes for the GatewayRegistration section. Refer to the AnyGateway Documentation for more detail.
```json
  "GatewayRegistration": {
    "LogicalName": "ACMPCASandbox",
    "GatewayCertificate": {
      "StoreName": "CA",
      "StoreLocation": "LocalMachine",
      "Thumbprint": "0123456789abcdef"
    }
  }
```

## ServiceSettings

Amazon ACM PCA places a limit on inventory requests of once per 30 minutes, so do not set your scans to less than 30 minutes.
Refer to the AnyGateway Documentation for more detail.
```json
  "ServiceSettings": {
    "ViewIdleMinutes": 8,
    "FullScanPeriodHours": 24,
	"PartialScanPeriodMinutes": 240 
  }
```

