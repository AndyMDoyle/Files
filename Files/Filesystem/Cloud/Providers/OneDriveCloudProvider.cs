using Files.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                using var appServiceConnection = await SetupAppServiceConnectionAsync();

                if (appServiceConnection != null)
                {
                    var response = await appServiceConnection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "GetOneDriveAccounts" }
                        });

                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        foreach (var key in response.Message.Keys
                            .OrderByDescending(o => string.Equals(o, "OneDrive - Personal", StringComparison.OrdinalIgnoreCase))
                            .ThenBy(o => o))
                        {
                            cloudProviders.Add(new CloudProvider()
                            {
                                ID = CloudProviders.OneDrive,
                                Name = key,
                                SyncFolder = (string)response.Message[key]
                            });
                        }
                    }
                }
            }
            catch
            {
                // Not detected
            }
        }

        private async Task<AppServiceConnection> SetupAppServiceConnectionAsync()
        {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                // Launch fulltrust process
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }

            var appServiceConnection = new AppServiceConnection();
            appServiceConnection.AppServiceName = "FilesInteropService";
            appServiceConnection.PackageFamilyName = Package.Current.Id.FamilyName;
            appServiceConnection.ServiceClosed += Connection_ServiceClosed;
            AppServiceConnectionStatus status = await appServiceConnection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                appServiceConnection?.Dispose();
                appServiceConnection = null;
            }

            return appServiceConnection;
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            sender?.Dispose();
            sender = null;
        }
    }
}