﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// host文件配置验证器
    /// </summary>
    sealed class HostsFileValidator
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<HostsFileValidator> logger;

        /// <summary>
        /// host文件配置验证器
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public HostsFileValidator(
            FastGithubConfig fastGithubConfig,
            ILogger<HostsFileValidator> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 验证host文件的域名解析配置 
        /// </summary>
        /// <returns></returns>
        public async Task ValidateAsync()
        {
            var hostsPath = @"/etc/hosts";
            if (OperatingSystem.IsWindows())
            {
                hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), $"drivers/{hostsPath}");
            }

            if (File.Exists(hostsPath) == false)
            {
                return;
            }

            var lines = await File.ReadAllLinesAsync(hostsPath);
            var records = lines.Where(item => item.TrimStart().StartsWith("#") == false);
            var localAddresses = GetLocalMachineIPAddress().ToArray();

            foreach (var record in records)
            {
                var items = record.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2)
                {
                    continue;
                }

                if (IPAddress.TryParse(items[0], out var address) == false)
                {
                    continue;
                }

                if (localAddresses.Contains(address))
                {
                    continue;
                }

                var domain = items[1];
                if (this.fastGithubConfig.IsMatch(domain))
                {
                    this.logger.LogWarning($"由于你的hosts文件设置了[{domain}->{address}]，{nameof(FastGithub)}无法加速此域名");
                }
            }
        }

        /// <summary>
        /// 获取本机所有ip
        /// </summary> 
        /// <returns></returns>
        private static IEnumerable<IPAddress> GetLocalMachineIPAddress()
        {
            yield return IPAddress.Loopback;
            yield return IPAddress.IPv6Loopback;

            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in @interface.GetIPProperties().UnicastAddresses)
                {
                    yield return addressInfo.Address;
                }
            }
        }
    }
}
