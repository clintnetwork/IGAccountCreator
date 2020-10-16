using System;
using System.Linq;
using System.Threading;
using ClintSharp;
using MailKit;
using MailKit.Net.Imap;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Service;

namespace IGAccountCreator
{
    public class InstagramAutomator
    {
        private static AppiumLocalService _appiumLocalService;
        private static AndroidDriver<AppiumWebElement> _driver;
        
        public InstagramAutomator()
        {
            Environment.SetEnvironmentVariable(AppiumServiceConstants.NodeBinaryPath, "/usr/local/bin/node");
            Environment.SetEnvironmentVariable(AppiumServiceConstants.AppiumBinaryPath, "/usr/local/bin/appium");
            Environment.SetEnvironmentVariable("ANDROID_HOME", "/Users/clint.network/Library/Android/sdk");
            Environment.SetEnvironmentVariable("JAVA_HOME", "/Library/Java/JavaVirtualMachines/jdk1.8.0_261.jdk/Contents/Home");

            _appiumLocalService = new AppiumServiceBuilder().UsingAnyFreePort().Build();
            _appiumLocalService.Start();

            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.DeviceName, "EML-L29");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, "10.0");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.NoReset, false);
            // appiumOptions.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, "120");
            // appiumOptions.AddAdditionalCapability("preventWDAAttachments", false);
            // appiumOptions.AddAdditionalCapability("clearSystemFiles", true);
            appiumOptions.AddAdditionalCapability("appPackage", "com.instagram.android");
            appiumOptions.AddAdditionalCapability("appActivity", "com.instagram.android.activity.MainTabActivity");
            // appiumOptions.AddAdditionalCapability("forceMjsonwp", true);
            
            Console.WriteLine("Starting...");
            _driver = new AndroidDriver<AppiumWebElement>(_appiumLocalService, appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        public void LoadCsv(string inputFile)
        {
        }
        
        public void Run()
        {
            Console.Write("Enter email: ");
            var email = Console.ReadLine();
            Console.Write("Enter password: ");
            var password = Console.ReadLine();
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            
            var signupCta = _driver.FindElements(By.Id("com.instagram.android:id/sign_up_with_email_or_phone"));
            Console.WriteLine($"Is setup CTA is present? {signupCta.Any()}");
            if (signupCta.Any())
            {
                signupCta.First().Click();
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/email_field")).SendKeys(email);
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab_next_button")).Click();
                
                // is already existing account
                
                var confirmationCode = GetConfirmationCode(email, password);
                if (confirmationCode.IsNull())
                {
                    throw new Exception("Unable to get confirmation code");
                }
                
                _driver.FindElement(By.Id("com.instagram.android:id/confirmation_code")).SendKeys(confirmationCode);
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
            
                _driver.FindElement(By.Id("com.instagram.android:id/full_name")).SendKeys("Nom complet");
                _driver.FindElement(By.Id("com.instagram.android:id/password")).SendKeys("jdks3202390");
                _driver.FindElement(By.Id("com.instagram.android:id/remember_password_checkbox")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/continue_without_ci")).Click();

                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/primary_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/add_age_link")).Click();
            
                _driver.FindElement(By.Id("com.instagram.android:id/entered_age")).SendKeys("21");
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/change_username")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/username")).SendKeys(username);
                _driver.FindElement(By.Id("com.instagram.android:id/next_button")).Click();
                
                _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                // _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/negative_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/skip_button")).Click();
                _driver.FindElement(By.Id("com.instagram.android:id/action_bar_button_action")).Click();
            }
        }

        private static string GetConfirmationCode(string email, string password)
        {
            for (var i = 0; i <= 10; i++)
            {
                Console.WriteLine($"Check confirmation code.");
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
                    Console.WriteLine($"Confirmation code found: {confirmationCode}.");
                    return confirmationCode;
                }
                Console.WriteLine("Confirmation code not found.");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            return null;
        }

        private static bool IsNumeric(string s) => int.TryParse(s, out var i);
    }
}