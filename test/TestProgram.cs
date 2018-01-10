//
// TestProgram.cs
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Mictlanix.CFDv33;
using Mictlanix.ProFact.Client;

namespace Tests {
	public class TestProgram {
		const string CSD_CERTIFICATE_FILE = "CSD01_AAA010101AAA.cer";
		const string CSD_PRIVATE_KEY_FILE = "CSD01_AAA010101AAA.key";
		const string CSD_PRIVATE_KEY_PWD = "12345678a";
		const string USERNAME = "mvpNUXmQfK8=";

		static DateTime NOW = DateTime.Now;
		static DateTime TEST_DATE = new DateTime (NOW.Year, NOW.Month, NOW.Day,
							  NOW.Hour, NOW.Minute, NOW.Second, DateTimeKind.Unspecified);
		//static DateTime TEST_DATE = new DateTime (2016, 02, 09, 10, 11, 12, DateTimeKind.Unspecified);

		static void Main (string[] args)
		{
			//StampTest ();
			//GetStampTest ();
			//CancelTest ();
			//SaveIssuerTest ();
		}

		static void StampTest ()
		{
			var cfd = CreateCFD ();
			var cli = new ProFactClient (USERNAME, ProFactClient.URL_TEST);

			AddItems (cfd, "Product", 3);
			cfd.Sign (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), Encoding.UTF8.GetBytes (CSD_PRIVATE_KEY_PWD));

			var tfd = cli.Stamp ("WS01", cfd);
			Console.WriteLine (tfd.ToXmlString ());
			Console.WriteLine (tfd.ToString ());

			cfd.Complemento = new List<object>();
			cfd.Complemento.Add (tfd);

			Console.WriteLine (cfd.ToXmlString ());
			Console.WriteLine (cfd.ToString ());
		}

		static void GetStampTest ()
		{
			var cli = new ProFactClient (USERNAME, ProFactClient.URL_TEST);
			var tfd = cli.GetStamp ("AAA010101AAA", "B1930368-6194-447D-8F41-95FAF528E72B");

			Console.WriteLine (tfd.ToString ());
			Console.WriteLine (tfd.ToXmlString ());
		}

		static void CancelTest ()
		{
			var cli = new ProFactClient (USERNAME, ProFactClient.URL_TEST);
			var ret = cli.Cancel ("AAA010101AAA", "B1930368-6194-447D-8F41-95FAF528E72B");

			Console.WriteLine ("Cancel: {0}", ret);
		}

		static void SaveIssuerTest ()
		{
			var cli = new ProFactClient (USERNAME, ProFactClient.URL_TEST);
			var ret = cli.SaveIssuer ("AAA010101AAA", File.ReadAllBytes (CSD_CERTIFICATE_FILE),
				          			  File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), CSD_PRIVATE_KEY_PWD);

			Console.WriteLine ("Save Issuer: {0}", ret);
		}

		#region Helper Functions

		static Comprobante CreateCFD()
		{
			var cfd = new Comprobante {
				TipoDeComprobante = c_TipoDeComprobante.Ingreso,
				Serie = "A",
				Folio = "1",
				Fecha = TEST_DATE,
				LugarExpedicion = "03810", // código postal
				MetodoPago = c_MetodoPago.PagoEnUnaSolaExhibicion,
				FormaPago = c_FormaPago.Efectivo,
				TipoCambio = 1m,
				Moneda = "MXN",
				NoCertificado = "20001000000200001428",
				Certificado = Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
				Emisor = new ComprobanteEmisor {
					Rfc = "AAA010101AAA",
					Nombre = "ACME SC",
					RegimenFiscal = c_RegimenFiscal.GeneralDeLeyPersonasMorales,
				},
				Receptor = new ComprobanteReceptor {
					Rfc = "XAXX010101000",
					Nombre = "DEMO COMPANY SC",
					UsoCFDI = c_UsoCFDI.AdquisicionDeMercancias
				},
				Impuestos = new ComprobanteImpuestos()
			};

			return cfd;
		}

		static void AddItem (Comprobante cfd, string code, string name, decimal qty, decimal amount)
		{
			int count = 1;

			if (cfd.Conceptos == null) {
				cfd.Conceptos = new ComprobanteConcepto [count];
			} else {
				count = cfd.Conceptos.Length + 1;
				var items = cfd.Conceptos;
				Array.Resize (ref items, count);
				cfd.Conceptos = items;
			}

			cfd.Conceptos[count - 1] = new ComprobanteConcepto {
				Cantidad = qty,
				ClaveUnidad = "H87",
				Unidad = "Pieza",
				NoIdentificacion = code,
				ClaveProdServ = "52161500",
				Descripcion = name,
				ValorUnitario = amount,
				Importe = Math.Round(qty * amount, 6),
				Impuestos = new ComprobanteConceptoImpuestos {
					Traslados = new ComprobanteConceptoImpuestosTraslado[] {
						new ComprobanteConceptoImpuestosTraslado {
							Impuesto = c_Impuesto.IVA,
							TipoFactor = c_TipoFactor.Tasa,
							Base = Math.Round(qty * amount, 6),
							Importe = Math.Round(qty * amount * 0.16m, 6),
							ImporteSpecified = true,
							TasaOCuota = 0.160000m,
							TasaOCuotaSpecified = true
						}
					}
				}
			};

			cfd.SubTotal = cfd.Conceptos.Sum (x => x.Importe);
			cfd.Total = Math.Round (cfd.SubTotal * 1.16m, 6);

			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Total - cfd.SubTotal;
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;
			cfd.Impuestos.Traslados = new ComprobanteImpuestosTraslado[] {
				new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					Importe = cfd.Total - cfd.SubTotal,
					TasaOCuota = 0.160000m
				}
			};
		}

		static void AddItems (Comprobante cfd, string prefix, int count)
		{
			var sum = 0m;

			cfd.Conceptos = new ComprobanteConcepto[count];

			for(int i = 1; i <= count; i++) {
				cfd.Conceptos[i-1] = new ComprobanteConcepto {
					Cantidad = i,
					ClaveUnidad = "H87",
					Unidad = "Pieza",
					NoIdentificacion = string.Format("P{0:D4}", i),
					ClaveProdServ = "52161500",
					Descripcion = string.Format("{0} {1:D4}", prefix, i),
					ValorUnitario = 5m * i,
					Importe = 5m * i * i,
					Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado[] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = Math.Round(5m * i * i, 6),
								Importe = Math.Round(5m * i * i * 0.16m, 6),
								ImporteSpecified = true,
								TasaOCuota = 0.160000m,
								TasaOCuotaSpecified = true
							}
						}
					}
				};
				sum += 5m * i * i;
			}

			cfd.SubTotal = sum;
			cfd.Total = Math.Round(sum * 1.16m, 6);

			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Total - cfd.SubTotal;
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;
			cfd.Impuestos.Traslados = new ComprobanteImpuestosTraslado[] {
				new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					Importe = cfd.Total - cfd.SubTotal,
					TasaOCuota = 0.160000m
				}
			};
		}

		#endregion
	}
}

