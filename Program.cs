using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Service;

namespace IGAccountCreatorBeta
{
    class Program
    {
        private static AppiumLocalService _appiumLocalService;
        private static AndroidDriver<AppiumWebElement> _driver;

        static void Main(string[] args)
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
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.NoReset, true);
            // appiumOptions.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, "120");
            // appiumOptions.AddAdditionalCapability("preventWDAAttachments", false);
            // appiumOptions.AddAdditionalCapability("clearSystemFiles", true);
            appiumOptions.AddAdditionalCapability("appPackage", "com.instagram.android");
            appiumOptions.AddAdditionalCapability("appActivity", "com.instagram.android.activity.MainTabActivity");
            // appiumOptions.AddAdditionalCapability("forceMjsonwp", true);
            
            Console.WriteLine("Starting...");
            _driver = new AndroidDriver<AppiumWebElement>(_appiumLocalService, appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            
            var signupCta = _driver.FindElements(By.Id("com.instagram.android:id/sign_up_with_email_or_phone"));
            Console.WriteLine($"Is setup CTA is present? {signupCta.Any()}");
            if (signupCta.Any())
            {
                signupCta.First().Click();
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab")).Click();
                
                Console.Write("Enter email: ");
                var email = Console.ReadLine();
                _driver.FindElement(By.Id("com.instagram.android:id/email_field")).SendKeys(email);
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab_next_button")).Click();
                
                // is already existing account

                Console.Write("Confirmation code: ");
                var confirmationCode = Console.ReadLine();
                
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
            }

        }
    }
}