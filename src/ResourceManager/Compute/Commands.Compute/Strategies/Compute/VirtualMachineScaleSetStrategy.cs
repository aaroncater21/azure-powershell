﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Internal.Resources.Models;
using Microsoft.Azure.Commands.Compute.Strategies.ResourceManager;
using Microsoft.Azure.Management.Internal.Network.Version2017_10_01.Models;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Microsoft.Azure.Commands.Common.Strategies.Compute
{
    public static class VirtualMachineScaleSetStrategy
    {
        public static ResourceStrategy<VirtualMachineScaleSet> Strategy { get; }
            = ComputePolicy.Create(
                type: "virtual machine scale set",
                provider: "virtualMachines",
                getOperations: client => client.VirtualMachineScaleSets,
                getAsync: (o, p) => o.GetAsync(
                    p.ResourceGroupName, p.Name, p.CancellationToken),
                createOrUpdateAsync: (o, p) => o.CreateOrUpdateAsync(
                    p.ResourceGroupName, p.Name, p.Model, p.CancellationToken),
                createTime: _ => 180);

        internal static ResourceConfig<VirtualMachineScaleSet> CreateVirtualMachineScaleSetConfig(
            this ResourceConfig<ResourceGroup> resourceGroup,
            string name,
            NestedResourceConfig<Subnet, VirtualNetwork> subnet,
            IEnumerable<NestedResourceConfig<FrontendIPConfiguration, LoadBalancer>> frontendIpConfigurations,
            NestedResourceConfig<BackendAddressPool, LoadBalancer> backendAdressPool,
            bool isWindows,
            string adminUsername,
            string adminPassword,
            Image image,
            string vmSize,
            int instanceCount,
            UpgradeMode? upgradeMode)
            => Strategy.CreateResourceConfig(
                resourceGroup: resourceGroup,
                name: name,
                createModel: subscriptionId =>
                {
                    var vmss = new VirtualMachineScaleSet()
                    {
                        Zones = frontendIpConfigurations
                            ?.Select(f => f.CreateModel(subscriptionId))
                            ?.Where(z => z?.Zones != null)
                            .SelectMany(z => z.Zones)
                            .Where(z => z != null)
                            .ToList(),

                        UpgradePolicy = upgradeMode.HasValue ?
                            new UpgradePolicy
                            {
                                Mode = upgradeMode
                            }
                            : new UpgradePolicy
                            {
                                Mode = UpgradeMode.Manual
                            },

                        Sku = new Microsoft.Azure.Management.Compute.Models.Sku()
                        {
                            Capacity = instanceCount,
                            Name = vmSize,
                        },
                        VirtualMachineProfile = new VirtualMachineScaleSetVMProfile()
                    };

                    vmss.VirtualMachineProfile.OsProfile = new VirtualMachineScaleSetOSProfile
                    {
                        ComputerNamePrefix = name.Substring(0, Math.Min(name.Length, 9)),
                        WindowsConfiguration = isWindows ? new WindowsConfiguration { } : null,
                        LinuxConfiguration = isWindows ? null : new LinuxConfiguration(),
                        AdminUsername = adminUsername,
                        AdminPassword = adminPassword,
                    };

                    vmss.VirtualMachineProfile.StorageProfile = new VirtualMachineScaleSetStorageProfile
                    {
                        ImageReference = new ImageReference
                        {
                            Publisher = image.publisher,
                            Offer = image.offer,
                            Sku = image.sku,
                            Version = image.version
                        }
                    };

                    var ipConfig = new VirtualMachineScaleSetIPConfiguration
                    {
                        Name = name,
                        LoadBalancerBackendAddressPools = new List<Microsoft.Azure.Management.Compute.Models.SubResource>(
                            new[] {
                                new Microsoft.Azure.Management.Compute.Models.SubResource(
                                    id: backendAdressPool.GetId(subscriptionId).IdToString())
                            }),
                        Subnet = new ApiEntityReference { Id = subnet.GetId(subscriptionId).IdToString() }
                    };


                    vmss.VirtualMachineProfile.NetworkProfile = new VirtualMachineScaleSetNetworkProfile
                    {
                        NetworkInterfaceConfigurations = new[]
                        {
                            new VirtualMachineScaleSetNetworkConfiguration
                            {
                                Name = name,
                                IpConfigurations = new List<VirtualMachineScaleSetIPConfiguration>(
                                                new [] { ipConfig }),
                                Primary = true
                            }
                        }
                    };


                    return vmss;
                },
                dependencies: new IEntityConfig[] { subnet, backendAdressPool }
                                                  .Concat(frontendIpConfigurations));
    }
}
