// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using Chem4Word.Core.Helpers;

namespace Chem4Word.Telemetry
{
    public class WmiHelper
    {

        private const string QueryProcessor = "SELECT Name,NumberOfLogicalProcessors,CurrentClockSpeed FROM Win32_Processor";
        private const string QueryOperatingSystem = "SELECT ProductType,LastBootUpTime FROM Win32_OperatingSystem";
        private const string QueryPhysicalMemory = "SELECT Capacity FROM Win32_PhysicalMemory";
        private const string QueryAntiVirusProduct = "SELECT DisplayName,ProductState FROM AntiVirusProduct";

        private const string Workstation = "Workstation";
        private const string DomainController = "Domain Controller";
        private const string Server = "Server";

        private const string Unknown = "Unknown";

        public WmiHelper()
        {
            GetWin32ProcessorData();
            GetWin32PhysicalMemoryData();
            GetWin32OperatingSystemData();
            GetAntiVirusStatus();
        }

        private string _cpuName;

        public string CpuName
        {
            get
            {
                if (_cpuName == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _cpuName;
            }
        }

        private string _cpuSpeed;

        public string CpuSpeed
        {
            get
            {
                if (_cpuSpeed == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _cpuSpeed;
            }
        }

        private string _logicalProcessors;

        public string LogicalProcessors
        {
            get
            {
                if (_logicalProcessors == null)
                {
                    try
                    {
                        GetWin32ProcessorData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _logicalProcessors;
            }
        }

        private string _physicalMemory;

        public string PhysicalMemory
        {
            get
            {
                if (_physicalMemory == null)
                {
                    try
                    {
                        GetWin32PhysicalMemoryData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _physicalMemory;
            }
        }

        private string _lastBootUpTime;

        public string LastLastBootUpTime
        {
            get
            {
                if (_lastBootUpTime == null)
                {
                    try
                    {
                        GetWin32OperatingSystemData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _lastBootUpTime;
            }
        }

        private string _productType;

        public string ProductType
        {
            get
            {
                if (_productType == null)
                {
                    try
                    {
                        GetWin32OperatingSystemData();
                    }
                    catch (Exception)
                    {
                        //
                    }
                }

                return _productType;
            }
        }

        private string _antiVirusStatus;

        public string AntiVirusStatus
        {
            get
            {
                if (_antiVirusStatus == null)
                {
                    GetAntiVirusStatus();
                }

                return _antiVirusStatus;
            }
        }

        private void GetWin32ProcessorData()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(QueryProcessor);
            ManagementObjectCollection objCol = searcher.Get();

            foreach (var o in objCol)
            {
                var mgtObject = (ManagementObject)o;
                try
                {
                    string temp = mgtObject["Name"].ToString();
                    // Replace tab with space
                    temp = temp.Replace("\t", " ");
                    // Replace up to 15 double spaces with single space
                    int i = 0;
                    while (temp.IndexOf("  ", StringComparison.InvariantCulture) != -1)
                    {
                        temp = temp.Replace("  ", " ");
                        i++;
                        if (i > 15)
                        {
                            break;
                        }
                    }

                    _cpuName = temp;
                }
                catch
                {
                    _cpuName = "?";
                }

                try
                {
                    _logicalProcessors = mgtObject["NumberOfLogicalProcessors"].ToString();
                }
                catch
                {
                    _logicalProcessors = "?";
                }

                try
                {
                    double speed = double.Parse(mgtObject["CurrentClockSpeed"].ToString()) / 1024;
                    _cpuSpeed = speed.ToString("#,##0.00", CultureInfo.InvariantCulture) + "GHz";
                }
                catch
                {
                    _cpuSpeed = "?";
                }
            }
        }

        private void GetWin32OperatingSystemData()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(QueryOperatingSystem);
            ManagementObjectCollection objCol = searcher.Get();

            try
            {
                foreach (var o in objCol)
                {
                    var mgtObject = (ManagementObject)o;
                    DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mgtObject["LastBootUpTime"].ToString());
                    _lastBootUpTime = SafeDate.ToLongDate(lastBootUp.ToUniversalTime()) + " UTC";

                    var productType = int.Parse(mgtObject["ProductType"].ToString());
                    switch (productType)
                    {
                        case 1:
                            _productType = Workstation;
                            break;
                        case 2:
                            _productType = DomainController;
                            break;
                        case 3:
                            _productType = Server;
                            break;
                        default:
                            _productType = Unknown + $" [{productType}]";
                            break;
                    }
                    break;
                }
            }
            catch
            {
                _lastBootUpTime = "?";
            }
        }

        private void GetWin32PhysicalMemoryData()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(QueryPhysicalMemory);
            ManagementObjectCollection objCol = searcher.Get();

            try
            {
                UInt64 capacity = 0;
                foreach (var o in objCol)
                {
                    var mgtObject = (ManagementObject)o;
                    capacity += (UInt64)mgtObject["Capacity"];
                }
                _physicalMemory = (capacity / (1024 * 1024 * 1024)).ToString("#,##0") + "GB";
            }
            catch
            {
                _physicalMemory = "?";
            }
        }

        private void GetAntiVirusStatus()
        {
            // http://neophob.com/2010/03/wmi-query-windows-securitycenter2/
            // https://mspscripts.com/get-installed-antivirus-information-2/
            // https://gallery.technet.microsoft.com/scriptcenter/Get-the-status-of-4b748f25

            // Only works if not a server
            if (!string.IsNullOrEmpty(ProductType) && ProductType.Equals(Workstation))
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", QueryAntiVirusProduct);
                ManagementObjectCollection objCol = searcher.Get();

                try
                {
                    List<string> products = new List<string>();
                    foreach (var o in objCol)
                    {
                        var mgtObject = (ManagementObject)o;
                        var product = mgtObject["DisplayName"].ToString();
                        var status = int.Parse(mgtObject["ProductState"].ToString());
                        var hex = Hex(status);
                        products.Add($"{product} {status} [{Hex(status)}] {ProductState(hex.Substring(2,2))} {DefinitionsState(hex.Substring(4, 2))}");
                    }
                    _antiVirusStatus = string.Join(";", products);
                }
                catch (Exception exception)
                {
                    _antiVirusStatus = $"{exception.Message}";
                }
            }

            // Local Functions

            string ProductState(string value)
            {
                switch (value)
                {
                    case "00":
                        return "Off";
                    case "01":
                        return "Expired";
                    case "10":
                        return "On";
                    case "11":
                        return "Snoozed";
                }

                return Unknown;
            }

            string DefinitionsState(string value)
            {
                switch (value)
                {
                    case "00":
                        return "Up to date";

                    case "10":
                        return "Outdated";
                }

                return Unknown;
            }
        }

        private string Hex(int value)
        {
            try
            {
                return value.ToString("X6");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}