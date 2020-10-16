using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClintSharp;
using CsvHelper;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Service;
using RestSharp;

namespace IGAccountCreator
{
    /// <summary>
    /// 
    /// </summary>
    public class InstagramAutomator : BackgroundService
    {
        private readonly ILogger<InstagramAutomator> _logger;
        private readonly IConfiguration _configuration;
        private static AndroidDriver<AppiumWebElement> _driver;
        private List<InstagramAccountInformation> _records;

        private const string ProxyId = "1B5C2EDC";

        public InstagramAutomator(ILogger<InstagramAutomator> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            Environment.SetEnvironmentVariable(AppiumServiceConstants.NodeBinaryPath, "/usr/local/bin/node");
            Environment.SetEnvironmentVariable(AppiumServiceConstants.AppiumBinaryPath, "/usr/local/bin/appium");
            Environment.SetEnvironmentVariable("ANDROID_HOME", "/Users/clint.network/Library/Android/sdk");
            Environment.SetEnvironmentVariable("JAVA_HOME", "/Library/Java/JavaVirtualMachines/jdk1.8.0_261.jdk/Contents/Home");
            
            var appiumLocalService = new AppiumServiceBuilder().UsingAnyFreePort().Build();
            appiumLocalService.Start();
            
            var appiumOptions = new AppiumOptions();
            var deviceName = configuration.GetSection("DeviceSetup").GetValue<string>("DeviceName");
            
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.DeviceName, deviceName);
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, "10.0");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.NoReset, false);
            appiumOptions.AddAdditionalCapability("appPackage", "com.instagram.android");
            appiumOptions.AddAdditionalCapability("appActivity", "com.instagram.android.activity.MainTabActivity");
            
            _logger.LogInformation("Starting InstagramAutomator...");
            _driver = new AndroidDriver<AppiumWebElement>(appiumLocalService, appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (File.Exists("input.csv"))
            {
                _logger.LogInformation("Load accounts from input.csv");
                using var reader = new StreamReader("input.csv");
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Configuration.Delimiter = ";";
                _records = csv.GetRecords<InstagramAccountInformation>().ToList();
            
                _logger.LogInformation($"There are {_records.Count} accounts to create.");
                
                foreach (var record in _records)
                {
                    _logger.LogInformation($"Creating account:\n{record.ToJsonF()}");
                    RegisterAccount(record);
                    ChangeProfilePicture(record.ProfilePicture);
                    ChangeFullName(record.Fullname);
                    ChangeBiography(record.Bio);
                    RenewIp();
                }
            }
            else
            {
                _logger.LogError("The input file (input.csv) does not exists.");
            }
            return Task.CompletedTask;
        }

        private void RenewIp()
        {
            var client = new RestClient($"https://hypeproxy.io/api/Utils/DirectRenewIp/{ProxyId}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                _logger.LogInformation($"Successfully renewed IP, new IP: {DisplayCurrentIp()}");
            }
            else
            {
                _logger.LogError("Unable to renew IP");
            }
        }

        private static string DisplayCurrentIp()
        {
            var client = new RestClient($"https://hypeproxy.io/api/Utils/GetExternalIp/{ProxyId}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            return response.Content;
        }

        private void RegisterAccount(InstagramAccountInformation record)
        {
            var signupCta = _driver.FindElements(By.Id("com.instagram.android:id/sign_up_with_email_or_phone"));
            _logger.LogInformation($"Is setup CTA is present? {signupCta.Any()}");
            if (signupCta.Any())
            {
                signupCta.First().Click();
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/email_field")).SendKeys(record.Email);
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab_next_button")).Click();
                
                // is already existing account
                
                var confirmationCode = GetConfirmationCode(record.Email, record.Password);
                if (confirmationCode.IsNull())
                {
                    throw new Exception("Unable to get confirmation code");
                }
                
                _driver.FindElement(By.Id("com.instagram.android:id/confirmation_code")).SendKeys(confirmationCode);
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
            
                _driver.FindElement(By.Id("com.instagram.android:id/full_name")).SendKeys(record.Fullname);
                _driver.FindElement(By.Id("com.instagram.android:id/password")).SendKeys(record.Password);
                _driver.FindElement(By.Id("com.instagram.android:id/remember_password_checkbox")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/continue_without_ci")).Click();
        
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/primary_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/add_age_link")).Click();
            
                _driver.FindElement(By.Id("com.instagram.android:id/entered_age")).SendKeys(record.Age);
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/change_username")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/username")).SendKeys(record.Username);
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                // _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/negative_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/action_bar_button_action")).Click();
            }
        }
        
        private void ChangeProfilePicture(string profilePicture)
        {
            _logger.LogInformation($"Update profile picture: {profilePicture}");
            
            try
            {
                var extension = new FileInfo(profilePicture).Extension.Replace(".", "");
                var photoBytes = new WebClient().DownloadData(profilePicture);
            
                // TODO: Check if the path is always the same
                _driver.PushFile($"/storage/emulated/0/DCIM/Camera/{Guid.NewGuid()}.{(string.IsNullOrWhiteSpace(extension) ? "jpg" : extension)}", photoBytes);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to download the picture ({e.Message})");
            }
            
            _driver
                .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                .Click();
        
            _driver
                .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                .FindElements(By.ClassName("android.widget.Button"))
                .First(x => x.Text.ToLower().Contains("edit profile"))
                .Click();
            
            _driver.FindElement(By.Id("com.instagram.android:id/change_avatar_button")).Click();
            _driver.FindElement(By.XPath("/hierarchy/android.widget.FrameLayout/android.widget.FrameLayout/android.widget.FrameLayout/android.widget.LinearLayout/android.widget.ListView/android.widget.FrameLayout[1]")).Click();
            _driver.FindElement(By.Id("com.instagram.android:id/next_button_textview")).Click();
            _driver.FindElement(By.Id("com.instagram.android:id/next_button_textview")).Click();
        }
        
        private void ChangeFullName(string fullname)
        {
            _logger.LogInformation($"Update ful name: {fullname}");
            
            _driver
                .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                .Click();
        
            _driver
                .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                .FindElements(By.ClassName("android.widget.Button"))
                .First(x => x.Text.ToLower().Contains("edit profile"))
                .Click();
            
            _driver.FindElement(By.Id("com.instagram.android:id/full_name")).Click();
            _driver.FindElement(By.Id("com.instagram.android:id/full_name")).FindElement(By.ClassName("android.widget.EditText")).Clear();
            _driver.FindElement(By.Id("com.instagram.android:id/full_name")).FindElement(By.ClassName("android.widget.EditText")).SendKeys(fullname);
            // _appium.Driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).Clear();
            // _appium.Driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).SendKeys(fullname);
            _driver.FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
            
            _driver.FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
        }
        
        private void ChangeBiography(string bio)
        {
            _logger.LogInformation($"Update biography: {bio}");
            _driver
                .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                .Click();
        
            _driver
                .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                .FindElements(By.ClassName("android.widget.Button"))
                .First(x => x.Text.ToLower().Contains("edit profile"))
                .Click();
            
            _driver.FindElement(By.Id("com.instagram.android:id/bio")).Click();
            _driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).Clear();
            _driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).SendKeys(bio);
            _driver.FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
            _driver.FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
        }
        
        private string GetConfirmationCode(string email, string password)
        {
            for (var i = 0; i <= 10; i++)
            {
                _logger.LogInformation($"Check confirmation code.");
                using var imapClient = new ImapClient();
                imapClient.Connect("outlook.office365.com", 993, true);
                imapClient.Authenticate(email, password);
            
                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadOnly);
                
                var confirmationCode = inbox
                    .GetMessage(inbox.Count() - 1)
                    .Subject
                    .Split(" ")[0]
                    .Trim();
            
                imapClient.Disconnect(true);
                
                if (confirmationCode.IsNotNull() && IsNumeric(confirmationCode))
                {
                    _logger.LogWarning($"Confirmation code found: {confirmationCode}.");
                    return confirmationCode;
                }
                _logger.LogError("Confirmation code not found.");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            return null;
        }
        
        private static bool IsNumeric(string s) => int.TryParse(s, out var i);
    }
}