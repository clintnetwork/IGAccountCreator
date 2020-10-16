using System;
using System.Linq;
using System.Threading;
using ClintSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Support.UI;

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
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, "120");
            appiumOptions.AddAdditionalCapability("preventWDAAttachments", false);
            appiumOptions.AddAdditionalCapability("clearSystemFiles", true);
            appiumOptions.AddAdditionalCapability("appPackage", "com.instagram.android");
            appiumOptions.AddAdditionalCapability("appActivity", "com.instagram.android.activity.MainTabActivity");
            // appiumOptions.AddAdditionalCapability("forceMjsonwp", true);
            
            Console.WriteLine("Starting...");
            _driver = new AndroidDriver<AppiumWebElement>(_appiumLocalService, appiumOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            
            Console.WriteLine(_driver.PageSource);
            
            var multiLogin = _driver.FindElements(By.Id("com.google.android.gms:id/credential_picker_layout"));
            Console.WriteLine($"Is multi login? {multiLogin}");
            if (multiLogin.Any())
            {
                Console.WriteLine("vire le multi login");
                _driver.FindElement(By.Id("com.google.android.gms:id/cancel")).Click();
                // Thread.Sleep(TimeSpan.FromSeconds(15));
            }

            var isLoginFormPresent = _driver.FindElements(By.Id("com.instagram.android:id/log_in_button"));
            Console.WriteLine($"Is login form is present? {isLoginFormPresent.Any()}");
            if (isLoginFormPresent.Any() && isLoginFormPresent.First().Text.ToLower().Contains("sign up"))
            {
                Console.WriteLine("signup first");
                isLoginFormPresent.First().Click();
                // Thread.Sleep(TimeSpan.FromSeconds(15));
            }
            
            var signupCta = _driver.FindElements(By.Id("com.instagram.android:id/sign_up_with_email_or_phone"));
            Console.WriteLine($"Is setup CTA is present? {signupCta.Any()}");
            if (signupCta.Any())
            {
                Console.WriteLine("signup second");
                _driver.FindElement(By.Id("com.instagram.android:id/sign_up_with_email_or_phone")).Click();
                // Thread.Sleep(TimeSpan.FromSeconds(15));
                Console.WriteLine("right tab");
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab")).Click();
                // Thread.Sleep(TimeSpan.FromSeconds(15));
                // _driver.HideKeyboard();
                // Thread.Sleep(TimeSpan.FromSeconds(3));
                // var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(27));
                // wait.Until(x => x.FindElement(By.Id("com.instagram.android:id/email_field"))).SendKeys("shanelle.dewey9826@outlook.com");
                Console.WriteLine("email field");
                _driver.FindElement(By.Id("com.instagram.android:id/email_field")).SendKeys("shanelle.dewey9826@outlook.com");
                // Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.WriteLine("next button");
                _driver.FindElement(By.Id("com.instagram.android:id/right_tab_next_button")).Click();
                // Thread.Sleep(TimeSpan.FromSeconds(15));
                
                var isAlreadyUsedEmail = _driver.FindElements(By.Id("com.instagram.android:id/negative_button"));
                
                Console.WriteLine(isAlreadyUsedEmail
                    .Select(x => x.Text)
                    .ToJsonF());

                Console.WriteLine("END OF LIFE");
                Console.ReadLine();

                if (isAlreadyUsedEmail.Any())
                {
                    isAlreadyUsedEmail.First().Click();
                }

            }

        }
    }
}