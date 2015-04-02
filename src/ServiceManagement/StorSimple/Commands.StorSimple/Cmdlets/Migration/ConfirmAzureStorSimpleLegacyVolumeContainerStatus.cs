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

//using Microsoft.WindowsAzure.Commands.StorSimple.Models;
using Microsoft.WindowsAzure.Commands.StorSimple.Properties;
using Microsoft.WindowsAzure.Management.StorSimple.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Microsoft.WindowsAzure.Commands.StorSimple.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Confirm, "AzureStorSimpleLegacyVolumeContainerStatus")]
    public class ConfirmAzureStorSimpleLegacyVolumeContainerStatus : StorSimpleCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = StorSimpleCmdletHelpMessage.MigrationConfigId)]
        [ValidateNotNullOrEmpty]
        public string LegacyConfigId { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = StorSimpleCmdletHelpMessage.MigrationOperation)]
        [ValidateSet("Commit", "Rollback", IgnoreCase = true)]
        public string MigrationOperation { get; set; }

        [Parameter(Mandatory = false, Position = 2, HelpMessage = StorSimpleCmdletHelpMessage.MigrationLegacyDataContainers)]
        public string[] LegacyContainerNames { get; set; }

        public override void ExecuteCmdlet()
        {
            try
            {
                MigrationConfirmStatusRequest request = new MigrationConfirmStatusRequest();
                request.Operation = (MigrationOperation)Enum.Parse(typeof(MigrationOperation), MigrationOperation, true);
                request.DataContainerNameList = (null != LegacyContainerNames) ? new List<string>(LegacyContainerNames.ToList().Distinct()) : new List<string>();
                MigrationJobStatus status = StorSimpleClient.ConfirmLegacyVolumeContainerStatus(LegacyConfigId, request);

                WriteObject(this.GetResultMessage(status));
            }
            catch(Exception except)
            {
                this.HandleException(except);
            }
        }

        /// <summary>
        /// Gets Confirm migration success message to be displayed with error string obtained from service
        /// </summary>
        /// <param name="status">migration job status</param>
        private string GetResultMessage(MigrationJobStatus status)
        {
            StringBuilder builder = new StringBuilder();
            bool errorMessage = false;
            if (null != status.MessageInfoList)
            {
                foreach (HcsMessageInfo msgInfo in status.MessageInfoList)
                {
                    if (!string.IsNullOrEmpty(msgInfo.Message))
                    {
                        builder.AppendLine(msgInfo.Message);
                        errorMessage = true;
                    }
                }
            }
            if (!errorMessage)
            {
                builder.AppendLine(Resources.ConfirmMigrationSuccessMessage);
            }
            return builder.ToString();
        }
    }
}