//
// FelClient.cs
//
// Author:
//       Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (c) 2016 Eddy Zavaleta, Mictlanix, and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Security;
using System.Xml;
using Mictlanix.CFDv32;
using Mictlanix.CFDLib;
using Mictlanix.ProFact.Client.Internals;

namespace Mictlanix.ProFact.Client {
	public class ProFactClient {
		public static string URL_PRODUCTION = @"https://www.timbracfdi.mx/serviciointegracion/Timbrado.asmx";
		public static string URL_TEST		= @"https://www.timbracfdipruebas.mx/serviciointegracionpruebas/Timbrado.asmx";

		static readonly BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport) {
			MaxBufferPoolSize = int.MaxValue,
			MaxReceivedMessageSize = int.MaxValue,
			ReaderQuotas = new XmlDictionaryReaderQuotas {
				MaxDepth = int.MaxValue,
				MaxStringContentLength = int.MaxValue,
				MaxArrayLength = int.MaxValue,
				MaxBytesPerRead = int.MaxValue,
				MaxNameTableCharCount = int.MaxValue,
			}
		};

		string url;
		EndpointAddress address;

		public ProFactClient (string username) : this (username, URL_PRODUCTION)
		{
		}

		public ProFactClient (string username, string url)
		{
			Username = username;
			Url = url;

			ServicePointManager.ServerCertificateValidationCallback = 
				(object sp, X509Certificate c, X509Chain r, SslPolicyErrors e) => true;
		}

		public string Username { get; protected set; }

		public string Url {
			get { return url;}
			set {
				if (url == value)
					return;

				url = value;
				address = new EndpointAddress (url);
			}
		}

		public TimbreFiscalDigital Stamp (string id, Comprobante cfd)
		{
			return Stamp (id, cfd.ToXmlBytes ());
		}

		public TimbreFiscalDigital Stamp (string id, string xml)
		{
			return Stamp (id, Encoding.UTF8.GetBytes (xml));
		}

		public TimbreFiscalDigital Stamp (string id, byte[] xml)
		{
			return StampBase64String (id, Convert.ToBase64String (xml));
		}

		/*
		 * TimbradoSoapClient Reponse Array
		 * 
		 * Index 0: Exception type
		 * Index 1: Error number
		 * Index 2: Result description
		 * Index 3: Stamped xml document
		 * Index 4: Byte array for QRCode image
		 * Index 5: Stamp string
		 * 
		 */

		public TimbreFiscalDigital StampBase64String (string id, string base64Xml)
		{
			string xml_response = null;
			TimbreFiscalDigital tfd = null;

			using (var ws = new TimbradoSoapClient (binding, address)) {
				var response = ws.TimbraCFDI (Username, base64Xml, id);
				string err_number = response [1].ToString ();
				string err_description = response [2].ToString ();

				if (err_number != "0") {
					throw new ProFactClientException (err_number, err_description);
				}

				xml_response = response [3].ToString ();
			}

			var cfd = Comprobante.FromXml (xml_response);

			foreach (var item in cfd.Complemento) {
				if (item is TimbreFiscalDigital) {
					tfd = item as TimbreFiscalDigital;
					break;
				}
			}

			if (tfd == null) {
				throw new ProFactClientException ("TimbreFiscalDigital not found.");
			}

			return new TimbreFiscalDigital {
				UUID = tfd.UUID,
				FechaTimbrado = tfd.FechaTimbrado,
				selloCFD = tfd.selloCFD,
				noCertificadoSAT = tfd.noCertificadoSAT,
				selloSAT = tfd.selloSAT
			};
		}

		public TimbreFiscalDigital GetStamp (string issuer, string uuid)
		{
			string xml_response = null;
			TimbreFiscalDigital tfd = null;

			using (var ws = new TimbradoSoapClient (binding, address)) {
				var response = ws.ObtieneCFDI (Username, issuer, uuid.ToUpper ());
				string err_number = response [1].ToString ();
				string err_description = response [2].ToString ();

				if (err_number != "0") {
					throw new ProFactClientException (err_number, err_description);
				}

				xml_response = response [3].ToString ();
			}

			var cfd = Comprobante.FromXml (xml_response);

			foreach (var item in cfd.Complemento) {
				if (item is TimbreFiscalDigital) {
					tfd = item as TimbreFiscalDigital;
					break;
				}
			}

			if (tfd == null) {
				throw new ProFactClientException ("TimbreFiscalDigital not found.");
			}

			return new TimbreFiscalDigital {
				UUID = tfd.UUID,
				FechaTimbrado = tfd.FechaTimbrado,
				selloCFD = tfd.selloCFD,
				noCertificadoSAT = tfd.noCertificadoSAT,
				selloSAT = tfd.selloSAT
			};
		}

		public bool Cancel (string issuer, string uuid)
		{
			using (var ws = new TimbradoSoapClient (binding, address)) {
				var response = ws.CancelaCFDI (Username, issuer, uuid.ToUpper ());
				string err_number = response [1].ToString ();
				string err_description = response [2].ToString ();

				if (err_number != "0") {
					throw new ProFactClientException (err_number, err_description);
				}
			}

			return true;
		}

		public bool SaveIssuer (string issuer, byte[] certificate, byte[] privateKey, string password)
		{
			using (var ws = new TimbradoSoapClient (binding, address)) {
				var response = ws.RegistraEmisor (Username, issuer, Convert.ToBase64String (certificate),
												  Convert.ToBase64String (privateKey), password);
				string err_number = response [1].ToString ();
				string err_description = response [2].ToString ();

				if (err_number != "0") {
					throw new ProFactClientException (err_number, err_description);
				}
			}

			return true;
		}
	}
}

