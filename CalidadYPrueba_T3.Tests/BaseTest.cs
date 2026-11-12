using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace CalidadYPrueba_T3.Tests
{
    public class BaseTest
    {
        protected IWebDriver driver;
        protected WebDriverWait wait;
        protected const string BaseUrl = "https://localhost:7116";

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            // Opcional: descomenta la siguiente línea si quieres que corra sin abrir ventana visible
            // options.AddArgument("--headless=new");

            driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(8));
        }

        protected void IniciarSesion(string codigo, string password)
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Cuenta/Login");
            var inputCodigo = driver.FindElement(By.CssSelector("input[type='text']"));
            var inputPass = driver.FindElement(By.CssSelector("input[type='password']"));

            inputCodigo.Clear();
            inputCodigo.SendKeys(codigo);
            inputPass.Clear();
            inputPass.SendKeys(password);

            driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        }
        protected string ObtenerTextoPantalla()
        {
            // 1. Esperar a que el navegador termine de cargar completamente la página actual
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            // 2. Leer el texto reintentando si la página aún estaba terminando de redirigir
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return driver.FindElement(By.TagName("body")).Text;
                }
                catch (StaleElementReferenceException)
                {
                    System.Threading.Thread.Sleep(300);
                }
            }
            return driver.FindElement(By.TagName("body")).Text;
        }


        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }
    }
}
