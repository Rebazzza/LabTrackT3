using NUnit.Framework;
using OpenQA.Selenium;

namespace CalidadYPrueba_T3.Tests
{
    [TestFixture]
    public class LoginTests : BaseTest
    {
        // Ajusta estas credenciales según tu base de datos
        private const string UsuarioValido = "N00111111";
        private const string PasswordValida = "Clave123!";
        private const string UsuarioBloqueo = "N00111111";

        [Test, Order(1)]
        public void CP01_Login_CodigoYContrasenaValidos_EntraAlSistema()
        {
            IniciarSesion(UsuarioValido, PasswordValida);

            Assert.That(driver.Url, Does.Not.Contain("/Cuenta/Login"));
        }

        [Test, Order(2)]
        public void CP02_Login_CodigoValidoContrasenaIncorrecta_MuestraCredencialesInvalidas()
        {
            IniciarSesion(UsuarioValido, "ClaveIncorrecta999!");

            Assert.That(driver.PageSource, Does.Contain("Credenciales inválidas"));
        }

        [Test, Order(3)]
        public void CP03_Login_CodigoInexistente_MuestraCredencialesInvalidas()
        {
            IniciarSesion("NO_EXISTE_999", "CualquierClave123!");

            Assert.That(driver.PageSource, Does.Contain("Credenciales inválidas"));
        }

        [Test, Order(4)]
        public void CP04_Login_CamposVacios_MuestraValidacionSinIngreso()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Cuenta/Login");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Verifica que sigue en Login y existen mensajes de validación HTML5 o ASP.NET
            Assert.That(driver.Url, Does.Contain("/Cuenta/Login"));
            var errores = driver.FindElements(By.CssSelector(".field-validation-error, .text-danger, input:invalid"));
            Assert.That(errores.Count, Is.GreaterThan(0));
        }

        [Test, Order(5)]
        public void CP05_Login_TresIntentosFallidos_BloqueaCuentaTemporalmente()
        {
            // Intentos 1 y 2
            IniciarSesion(UsuarioBloqueo, "ClaveMala1!");
            IniciarSesion(UsuarioBloqueo, "ClaveMala2!");

            // Intento 3 (dispara el bloqueo de MaxFailedAccessAttempts = 3)
            IniciarSesion(UsuarioBloqueo, "ClaveMala3!");

            Assert.That(driver.PageSource, Does.Contain("Cuenta bloqueada temporalmente por seguridad"));
        }

        [Test, Order(6)]
        public void CP06_Login_CuentaBloqueadaConPasswordCorrecta_NoPermiteIngreso()
        {
            // Se ejecuta inmediatamente después de CP-05 con la contraseña correcta
            IniciarSesion(UsuarioBloqueo, PasswordValida);

            Assert.That(driver.Url, Does.Contain("/Cuenta/Login"));
            Assert.That(driver.PageSource, Does.Contain("Cuenta bloqueada temporalmente por seguridad"));
        }
    }
}
