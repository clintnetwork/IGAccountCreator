using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClintSharp;
using CsvHelper;
using IGAccountCreator.Models;
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

        private const string ProxyId = "DA85696F";

        public InstagramAutomator(ILogger<InstagramAutomator> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            Environment.SetEnvironmentVariable(AppiumServiceConstants.NodeBinaryPath, "/usr/local/bin/node");
            Environment.SetEnvironmentVariable(AppiumServiceConstants.AppiumBinaryPath, "/usr/local/bin/appium");
            Environment.SetEnvironmentVariable("ANDROID_HOME", "/Users/clint.network/Library/Android/sdk");
            Environment.SetEnvironmentVariable("JAVA_HOME", "/Library/Java/JavaVirtualMachines/jdk1.8.0_261.jdk/Contents/Home");

            // var appiumRemoteService = new AppiumServiceBuilder()..Build();
            var appiumLocalService = new AppiumServiceBuilder().UsingAnyFreePort().Build();
            appiumLocalService.Start();
            
            var appiumOptions = new AppiumOptions();
            var deviceName = configuration.GetSection("DeviceSetup").GetValue<string>("DeviceName");
            
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.DeviceName, deviceName);
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.AutomationName, "uiautomator2");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, "10.0");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.NoReset, true);
            appiumOptions.AddAdditionalCapability("appPackage", "com.instagram.android");
            appiumOptions.AddAdditionalCapability("appActivity", "com.instagram.android.activity.MainTabActivity");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.Language, "en");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.Locale, "US");
            appiumOptions.AddAdditionalCapability(AndroidMobileCapabilityType.AutoGrantPermissions, true);
            appiumOptions.AddAdditionalCapability("unicodeKeyboard", true);
            // appiumOptions.AddAdditionalCapability("resetKeyboard", true);
            
            _logger.LogInformation("Starting InstagramAutomator...");
            _driver = new AndroidDriver<AppiumWebElement>(appiumLocalService, appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);

            // var t = _driver.Manage().Logs.GetLog(LogType.Client);
            // Console.WriteLine(t.ToJsonF());
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
                
                var output = new List<AutomatorStatus>();
            
                _logger.LogInformation($"There are {_records.Count} accounts to create.");
                
                foreach (var record in _records)
                {
                    _logger.LogInformation($"Creating account:\n{record.ToJsonF()}");

                    // var registerAccount = RegisterAccount(record);
                    var changeProfilePicture = ChangeProfilePicture(record.ProfilePicture);
                    var changeBiography = ChangeBiography(record.Bio);
                    var renewIp = RenewIp();
                    _driver.ResetApp();
                    
                    output.Add(new AutomatorStatus
                    {
                        Email = record.Email,
                        Username = record.Username,
                        // HasRegistredAccount = registerAccount.IsSuccess,
                        HasUpdatedProfilePicture = changeProfilePicture.IsSuccess,
                        HasUpdatedBio = changeBiography.IsSuccess,
                        HasRenewedIp = renewIp.IsSuccess,
                        Details = $"pp={changeProfilePicture.ErrorMessage ?? "N/a"}, bio={changeBiography.ErrorMessage ?? "N/a"}",
                        UsedIp = GetCurrentIp()
                    });
                }
                
                _logger.LogInformation($"Account creation result:\n{output}");
                
                using var outputWriter = new StreamWriter("output.csv");
                using var outputCsv = new CsvWriter(outputWriter, CultureInfo.InvariantCulture);
                outputCsv.WriteRecords(output);
            }
            else
            {
                _logger.LogError("The input file (input.csv) does not exists.");
            }
            return Task.CompletedTask;
        }
        
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }

        private AutomationResult RegisterAccount(InstagramAccountInformation record)
        {
            try
            {
                // var multiLogin = FindElements(By.Id("com.google.android.gms:id/credential_picker_layout"));
                // if (multiLogin.Any())
                //     FindElement(By.Id("com.google.android.gms:id/cancel")).Click();
            
                var signupCta = FindElements(By.Id("com.instagram.android:id/sign_up_with_email_or_phone"));
                _logger.LogInformation($"Is setup CTA is present? {signupCta.Any()}");
                if (signupCta.Any())
                {
                    signupCta.First().Click();
                    FindElement(By.Id("com.instagram.android:id/right_tab")).Click();
                
                    FindElement(By.Id("com.instagram.android:id/email_field")).SendKeys(record.Email);
                    FindElement(By.Id("com.instagram.android:id/right_tab_next_button")).Click();
                
                    // is already existing account
                
                    var confirmationCode = GetConfirmationCode(record.Email, record.Password);
                    if (confirmationCode.IsNull())
                    {
                        throw new Exception("Unable to get confirmation code");
                    }
                
                    FindElement(By.Id("com.instagram.android:id/confirmation_code")).SendKeys(confirmationCode);
                    FindElement(By.Id("com.instagram.android:id/next_button")).Click();
            
                    FindElement(By.Id("com.instagram.android:id/full_name")).SendKeys(record.Fullname);
                    FindElement(By.Id("com.instagram.android:id/password")).SendKeys(record.Password);
                    FindElement(By.Id("com.instagram.android:id/remember_password_checkbox")).Click();
                    FindElement(By.Id("com.instagram.android:id/continue_without_ci")).Click();
        
                    FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                    FindElement(By.Id("com.instagram.android:id/primary_button")).Click();
                    FindElement(By.Id("com.instagram.android:id/add_age_link")).Click();
            
                    FindElement(By.Id("com.instagram.android:id/entered_age")).SendKeys(record.Age);
                    FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                
                    FindElement(By.Id("com.instagram.android:id/change_username")).Click();
                    FindElement(By.Id("com.instagram.android:id/username")).SendKeys(record.Username);
                    FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                    
                    Console.WriteLine(_driver.PageSource);
                
                    FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                    // FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                    FindElement(By.Id("com.instagram.android:id/negative_button")).Click();
                    FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                    FindElement(By.Id("com.instagram.android:id/action_bar_button_action")).Click();

                    return AutomationResult.Ok();
                }
                return AutomationResult.Error("Unable to found the signup CTA");
            }
            catch (Exception e)
            {
                return AutomationResult.Error(e.Message);
            }
        }
        
        private AutomationResult ChangeProfilePicture(string profilePicture)
        {
            _logger.LogInformation($"Update profile picture: {profilePicture}");

            try
            {
                var extension = new FileInfo(profilePicture).Extension.Replace(".", "");
                var photoBytes = new WebClient().DownloadData(profilePicture);
        
                // TODO: Check if the path is always the same
                _driver.PushFile($"/storage/emulated/0/DCIM/Camera/{Guid.NewGuid()}.{(string.IsNullOrWhiteSpace(extension) ? "jpg" : extension)}", photoBytes);
            
                _driver
                    .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                    .Click();
        
                _driver
                    .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                    .FindElements(By.ClassName("android.widget.Button"))
                    .First(x => x.Text.ToLower().Contains("edit profile"))
                    .Click();
            
                FindElement(By.Id("com.instagram.android:id/change_avatar_button")).Click();

                var hasActionSheetOpened = _driver.FindElement(By.Id("com.instagram.android:id/action_sheet_header_text_view")).Displayed;
                if (hasActionSheetOpened)
                {
                    _logger.LogInformation("First profile picture upload");
                    FindElement(By.XPath(@"/hierarchy/android.widget.FrameLayout/android.widget.LinearLayout/android.widget.FrameLayout/android.widget.LinearLayout/android.widget.FrameLayout/android.widget.FrameLayout/android.widget.FrameLayout[2]/android.widget.FrameLayout[2]/android.widget.FrameLayout/android.widget.FrameLayout/android.view.ViewGroup/android.widget.FrameLayout/android.view.ViewGroup/androidx.recyclerview.widget.RecyclerView/android.widget.TextView[1]")).Click();
                }
                else
                {
                    _logger.LogInformation("Profile picture upload");
                    FindElement(By.XPath("/hierarchy/android.widget.FrameLayout/android.widget.FrameLayout/android.widget.FrameLayout/android.widget.LinearLayout/android.widget.ListView/android.widget.FrameLayout[1]")).Click();
                }
                FindElement(By.Id("com.instagram.android:id/next_button_textview")).Click();
                FindElement(By.Id("com.instagram.android:id/next_button_textview")).Click();

                return AutomationResult.Ok();
            }
            catch (Exception e)
            {
                return AutomationResult.Error(e.Message);
            }
        }

        private AutomationResult ChangeFullName(string fullname)
        {
            _logger.LogInformation($"Update fullname: {fullname}");

            try
            {
                _driver
                    .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                    .Click();
        
                _driver
                    .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                    .FindElements(By.ClassName("android.widget.Button"))
                    .First(x => x.Text.ToLower().Contains("edit profile"))
                    .Click();
            
                FindElement(By.Id("com.instagram.android:id/full_name")).Click();
                FindElement(By.Id("com.instagram.android:id/full_name")).FindElement(By.ClassName("android.widget.EditText")).Clear();
                FindElement(By.Id("com.instagram.android:id/full_name")).FindElement(By.ClassName("android.widget.EditText")).SendKeys(fullname);
                // _appium.Driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).Clear();
                // _appium.Driver.FindElement(By.Id("com.instagram.android:id/caption_edit_text")).SendKeys(fullname);
                FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();

                Console.ReadLine();
                FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();

                return AutomationResult.Ok();
            }
            catch (Exception e)
            {
                return AutomationResult.Error(e.Message);
            }
        }
        
        private AutomationResult ChangeBiography(string bio)
        {
            _logger.LogInformation($"Update biography: {bio}");
            try
            {
                _driver
                    .FindElement(By.Id("com.instagram.android:id/profile_tab"))
                    .Click();
        
                _driver
                    .FindElement(By.Id("com.instagram.android:id/coordinator_root_layout"))
                    .FindElements(By.ClassName("android.widget.Button"))
                    .First(x => x.Text.ToLower().Contains("edit profile"))
                    .Click();
            
                FindElement(By.Id("com.instagram.android:id/bio")).Click();
                FindElement(By.Id("com.instagram.android:id/caption_edit_text")).Clear();
                FindElement(By.Id("com.instagram.android:id/caption_edit_text")).SendKeys(bio);
                FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
                FindElement(By.XPath(@"//android.widget.ViewSwitcher[@content-desc=""Done""]/android.widget.ImageView")).Click();
                
                return AutomationResult.Ok();
            }
            catch (Exception e)
            {
                return AutomationResult.Error(e.Message);
            }
        }

        private AutomationResult RenewIp()
        {
            var client = new RestClient($"https://hypeproxy.io/api/Utils/DirectRenewIp/{ProxyId}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                _logger.LogInformation($"Successfully renewed IP, new IP: {GetCurrentIp()}");
                return AutomationResult.Ok();
            }
            else
            {
                _logger.LogError("Unable to renew IP");
                return AutomationResult.Error("Unable to renew the IP");
            }
        }

        private static string GetCurrentIp()
        {
            var client = new RestClient($"https://hypeproxy.io/api/Utils/GetExternalIp/{ProxyId}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            return response.Content.Replace("\"", "");
        }
        
        private string GetConfirmationCode(string email, string password)
        {
            for (var i = 0; i <= 10; i++)
            {
                _logger.LogInformation($"Check confirmation code.");
                using var imapClient = new ImapClient();
                try
                {
                    imapClient.Connect("outlook.office365.com", 993, true);
                    imapClient.Authenticate(email, password);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unable to connect to the email: {e.Message}");
                }
                
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

        private AppiumWebElement FindElement(By by)
        {
            var element = _driver.FindElement(by);
            _logger.LogTrace($"Find element (element={by}, found={element != null})");
            return element;
        }

        private ReadOnlyCollection<AppiumWebElement> FindElements(By by)
        {
            _logger.LogTrace($"Find elements (element={by})");
            return _driver.FindElements(by);
        }
        
        private static bool IsNumeric(string s) => int.TryParse(s, out var i);
    }
}