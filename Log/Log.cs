using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Configuration;
using System.Xml;
using Serilog;

namespace DocumentGenerator
{
    public static class Log
    {
        private static ILogger _logger;
        private static bool _enabled;

        private const int MAXSIZE = 16777216;

        static Log()
        {
            if ((System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime))
            {
                _enabled = false;
                return;
            }

            try
            {
                string log = ConfigurationManager.AppSettings.Get("enableLogging");

                if (log == string.Empty || log.ToLower() != "false")
                {
                    string progKey = string.Empty;

                    try
                    {
                        progKey = getSetting("defaultSettings", "progKeyName");

                        if (progKey != string.Empty)
                            progKey = progKey + ".";
                    }
                    catch (Exception ex)
                    {
                    }

                    if (progKey.Trim() == string.Empty)
                        progKey = "General.";

                    string logName;
                    logName = System.IO.Path.GetTempPath() + "civiltech." + progKey + "log";

                    if (System.IO.File.Exists(logName))
                    {
                        string logText;
                        logText = System.IO.File.ReadAllText(logName);

                        if ((logText.Length > MAXSIZE))
                            System.IO.File.WriteAllText(logName, logText.Substring(MAXSIZE / 2) + string.Format("{0}----------------------------------------------------------------------------------------------------------------------------------------------------------------{0}", Environment.NewLine));
                    }

                    // REM System.IO.File.AppendAllText(logName, String.Format("{0}----------------------------------------------------------------------------------------------------------------------------------------------------------------{0}", vbCrLf))
                    //Serilog.Log.Logger = new LoggerConfiguration()
                    //    .WriteTo.File(logName, 
                    //    Serilog.Events.LogEventLevel.Verbose, 
                    //    null/* Conversion error: Set to default value for this argument */, 
                    //    null/* Conversion error: Set to default value for this argument */, MAXSIZE * 4)
                    //    .CreateLogger();


                    Serilog.Log.Logger = new LoggerConfiguration()
                        .WriteTo.File(logName, rollingInterval: RollingInterval.Day)
                        .CreateLogger();

                    // var log = new LoggerConfiguration()
                    //.WriteTo.Console()
                    //.WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
                    //.CreateLogger();

                    Serilog.Log.Verbose("Beginning logging ...");
                    _enabled = true;
                }
                else
                    _enabled = false;
            }
            catch (Exception ex)
            {
                // MsgBox("Logger initialization failed! (" & ex.Message & ")", MsgBoxStyle.Critical)
                _enabled = false;
            }
        }

        public static string getSetting(string section, string name)
        {
            //Common.Configuration.IConfigurationSettings settings;

            //settings = (Common.Configuration.IConfigurationSettings) System.Configuration.ConfigurationManager.GetSection(section);

            //if (settings != null)
            //{
            //    foreach (XmlNode node in settings.GetConfig().ChildNodes)
            //    {
            //        if (!node.Attributes == null && !node.Attributes["name"] == null && node.Attributes["name"].Value == name)
            //        {
            //            if (!node.Attributes["value"] == null)
            //                return node.Attributes["value"].Value;
            //            else
            //                break;
            //        }
            //    }
            //}
            return string.Empty;
        }

        public static string GetMethod()
        {
            System.Reflection.MethodBase method = (new System.Diagnostics.StackTrace()).GetFrame(2).GetMethod();

            return string.Format("{0}.[{1}]", method.DeclaringType.FullName, method.ToString());
        }


        // Verbose
        public static void Verbose(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Verbose(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Verbose(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Verbose(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        // Debug
        public static void Debug(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Debug(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Debug(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Debug(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        // Information
        public static void Information(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Information(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Information(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Information(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        // Warning
        public static void Warning(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Warning(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Warning(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Warning(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        // Error
        public static void Error(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Error(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Error(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Error(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        // Fatal
        public static void Fatal(string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Fatal(string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }

        public static void Fatal(Exception ex, string msg, params object[] properties)
        {
            if (_enabled)
                Serilog.Log.Fatal(ex, string.Format("{0}{1}        [ -> {2}]", msg, Environment.NewLine, GetMethod()), properties);
        }
    }

}
