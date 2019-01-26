﻿using System;
using System.Threading.Tasks;

using Microsoft.Rest;
using Microsoft.PowerBI.Api.V2;

using Plugin.Settings;

using Xamarin.Forms;

namespace PowerBISampleApp
{
    abstract class BasePowerBIService
    {
        #region Fields
        static int _networkIndicatorCount = 0;
        static PowerBIClient _powerBIClient;
        #endregion

        #region Properties
        static string AccessToken
        {
            get => CrossSettings.Current.GetValueOrDefault(nameof(AccessToken), string.Empty);
            set => CrossSettings.Current.AddOrUpdateValue(nameof(AccessToken), value);
        }

        static string AccessTokenType
        {
            get => CrossSettings.Current.GetValueOrDefault(nameof(AccessTokenType), string.Empty);
            set => CrossSettings.Current.AddOrUpdateValue(nameof(AccessTokenType), value);
        }

        static DateTimeOffset AccessTokenExpiresOnDateTimeOffset
        {
            get
            {
                DateTimeOffset expirationAsDateTimeOffset;

                var expirationAsString = CrossSettings.Current.GetValueOrDefault(nameof(AccessTokenExpiresOnDateTimeOffset), string.Empty);

                if (string.IsNullOrEmpty(expirationAsString))
                    expirationAsDateTimeOffset = new DateTimeOffset(0, 0, 0, 0, 0, 0, TimeSpan.FromMilliseconds(0));
                else
                    DateTimeOffset.TryParse(expirationAsString, out expirationAsDateTimeOffset);

                return expirationAsDateTimeOffset;
            }
            set
            {
                var dateTimeOffsetAsString = value.ToString();
                CrossSettings.Current.AddOrUpdateValue(nameof(AccessTokenExpiresOnDateTimeOffset), dateTimeOffsetAsString);
            }
        }
        #endregion

        #region Methods
        protected static async ValueTask<PowerBIClient> GetPowerBIClient()
        {
            if (_powerBIClient is null)
            {
                await Authenticate();
                _powerBIClient = new PowerBIClient(new TokenCredentials(AccessToken, AccessTokenType));
            }

            return _powerBIClient;
        }

        protected static void UpdateActivityIndicatorStatus(bool isActivityIndicatorDisplayed)
        {
            if (isActivityIndicatorDisplayed)
            {
                XamarinFormsHelpers.BeginInvokeOnMainThread(() => Application.Current.MainPage.IsBusy = true);
                _networkIndicatorCount++;
            }
            else if (--_networkIndicatorCount <= 0)
            {
                XamarinFormsHelpers.BeginInvokeOnMainThread(() => Application.Current.MainPage.IsBusy = false);
                _networkIndicatorCount = 0;
            }
        }

        static async Task Authenticate()
        {
            if (string.IsNullOrWhiteSpace(AccessToken)
                    || string.IsNullOrWhiteSpace(AccessTokenType)
                    || DateTimeOffset.UtcNow.CompareTo(AccessTokenExpiresOnDateTimeOffset) >= 1)
            {
                UpdateActivityIndicatorStatus(true);

                try
                {
                    var authenticationResult = await DependencyService.Get<IAuthenticator>()?.Authenticate(
                                AzureConstants.OAuth2Authority,
                                AzureConstants.Resource,
                                AzureConstants.ApplicationId,
                                AzureConstants.RedirectURL);

                    AccessToken = authenticationResult.AccessToken;
                    AccessTokenExpiresOnDateTimeOffset = authenticationResult.ExpiresOn;
                    AccessTokenType = authenticationResult.AccessTokenType;
                }
                finally
                {
                    UpdateActivityIndicatorStatus(false);
                }
            }
        }
        #endregion
    }
}
