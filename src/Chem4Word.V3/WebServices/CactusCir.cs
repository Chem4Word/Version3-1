// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chem4Word.WebServices
{
    public class CactusCir
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private IChem4WordTelemetry Telemetry { get; set; }

        public CactusCir(IChem4WordTelemetry telemetry)
        {
            Telemetry = telemetry;
        }

        private List<string> GetCirData(string inchiKey, string convertTo)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}(InChIKey, {convertTo})";

            string url = "";
            DateTime started = DateTime.Now;

            List<string> data = new List<string>();
            try
            {
                Telemetry.Write(module, "Verbose", "Calling WebService");
                if (Globals.Chem4WordV3.SystemOptions == null)
                {
                    Globals.Chem4WordV3.LoadOptions();
                }
                url = $"{Globals.Chem4WordV3.SystemOptions.ChemSpiderRdfServiceUri}InChIKey={inchiKey}/{convertTo}";

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 5000;
                request.UserAgent = "Chem4Word";
                HttpWebResponse response;

                response = (HttpWebResponse)request.GetResponse();
                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    var stream = response.GetResponseStream();
                    var lines = StreamToString(stream);
                    data.Add(lines);
                }
                else
                {
                    Telemetry.Write(module, "Error", "Http Status: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Telemetry.Write(module, "Exception(Data)", url);
                Telemetry.Write(module, "Exception", ex.Message);
            }

            TimeSpan ts = DateTime.Now - started;
            Telemetry.Write(module, "Timing", "Took " + ts.TotalMilliseconds.ToString("#,###.0", CultureInfo.InvariantCulture) + "ms");

            return data;
        }

        public Dictionary<string, string> GetSynonyms(string inchiKey)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(inchiKey))
            {
                // Get Cactus IUPAC Name
                var r1 = GetCirData(inchiKey, "iupac_name");
                if (r1.Any())
                {
                    result.Add(Constants.CactusResolverIupacName, r1[0]);
                }
                else
                {
                    result.Add(Constants.CactusResolverIupacName, "Unknown");
                }

                // Get Cactus SMILES
                var r2 = GetCirData(inchiKey, "smiles");
                if (r2.Any())
                {
                    result.Add(Constants.CactusResolverSmilesName, r2[0]);
                }
                else
                {
                    result.Add(Constants.CactusResolverSmilesName, "Unknown");
                }

                // Get Cactus Chemical Formula
                var r3 = GetCirData(inchiKey, "formula");
                if (r3.Any())
                {
                    result.Add(Constants.CactusResolverFormulaName, r3[0]);
                }
                else
                {
                    result.Add(Constants.CactusResolverFormulaName, "Unknown");
                }
            }

            return result;
        }

        private string StreamToString(Stream stream)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
