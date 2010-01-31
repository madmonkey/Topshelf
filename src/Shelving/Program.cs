﻿// Copyright 2007-2008 The Apache Software Foundation.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Shelving
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Configuration;
    using log4net;
    using Topshelf;
    using Topshelf.Configuration.Dsl;
    using Topshelf.Model.Shelving;

    internal class Program
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(Program));
        static IList<ShelvedServiceInfo> _shelvedServiceInfo;
        static Config _config = new DefaultConfiguration();

        static Dictionary<RunAs, Action<IRunnerConfigurator>> _runas = new Dictionary<RunAs, Action<IRunnerConfigurator>>()
                                                                       {
                                                                           {RunAs.Interactive, c => c.RunAsFromInteractive()},
                                                                           {RunAs.LocalSystem, c => c.RunAsLocalSystem()},
                                                                           {RunAs.NetworkService, c=>c.RunAsNetworkService()},
                                                                           {RunAs.LocalService, c=>c.RunAsLocalService()}
                                                                       };

        static void Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure();

            EnsureServicesDirectoryExists();
            _shelvedServiceInfo = CollectSubFolders();
            _log.DebugFormat("Found '{0}' services", _shelvedServiceInfo.Count);

            //register app domain?  
            var cfg = RunnerConfigurator.New(c =>
            {
                c.SetServiceName("shelving");
                c.SetDisplayName("Topshelf Shelving");
                c.SetDescription("A conventionbased .net service host");

                _runas[_config.HowToRun](c);

                foreach (var serviceInfo in _shelvedServiceInfo)
                {
                    _log.InfoFormat("Configuring service '{0}' for shelving", serviceInfo.InferredName);

                    var info = serviceInfo;
                    c.ConfigureServiceInShelving(sc =>
                    {
                        sc.Named(info.InferredName);
                        sc.CommandLineArguments(args);
                        sc.PathToPrivateBin(info.FullPath);
                    });

                }
            });

            Runner.Host(cfg, args);
        }

        static IList<ShelvedServiceInfo> CollectSubFolders()
        {
            return Directory.GetDirectories(_config.ServicesDirectory).Select(s =>
            {
                var name = new DirectoryInfo(s).Name;
                _log.DebugFormat("Found service '{0}'", name);
                return new ShelvedServiceInfo(s);
            }).ToList();
        }

        static void EnsureServicesDirectoryExists()
        {
            if (!Directory.Exists(_config.ServicesDirectory))
            {
                _log.Info("Didn't find services directory, creating now.");
                Directory.CreateDirectory(_config.ServicesDirectory);
            }
        }
    }
}