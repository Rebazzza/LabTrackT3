using NUnit.Framework;
using OpenQA.Selenium;
using System;

namespace CalidadYPrueba_T3.Tests
{
    [TestFixture]
    public class ReservasTests : BaseTest
    {
        private const string EstudianteUser = "N00222222";
        private const string EstudiantePass = "Clave123!";

        private const int EquipoDisponibleId = 1;
        private const int EquipoPrestadoId = 2;     // Tiene reserva activa ahora (CP-14)
        private const int EquipoConCruceId = 3;     // Tiene reserva aprobada mañana 14:00-16:00 (CP-15)

        [SetUp]
        public void LoginEstudiante()
        {
            IniciarSesion(EstudianteUser, EstudiantePass);
            // Esperar a que termine de redirigir fuera del Login
            wait.Until(d => !d.Url.Contains("/Cuenta/Login"));
        }

        private void LlenarYEnviarReserva(DateTime fecha, string horaInicio, string horaFin, bool quitarMinimoHtml = false)
        {
            var js = (IJavaScriptExecutor)driver;

            var inputFecha = driver.FindElement(By.Id("Fecha"));
            var inputInicio = driver.FindElement(By.Id("HoraInicio"));
            var inputFin = driver.FindElement(By.Id("HoraFin"));

            if (quitarMinimoHtml)
            {
                js.ExecuteScript("arguments[0].removeAttribute('min');", inputFecha);
            }

            string scriptAsignar = "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true }));";

            js.ExecuteScript(scriptAsignar, inputFecha, fecha.ToString("yyyy-MM-dd"));
            js.ExecuteScript(scriptAsignar, inputInicio, horaInicio);
            js.ExecuteScript(scriptAsignar, inputFin, horaFin);

            driver.FindElement(By.XPath("//button[contains(normalize-space(), 'Enviar solicitud')]")).Click();
        }


        [Test, Order(1)]
        public void CP13_Reserva_ValidaManana_SeRegistraComoPendiente()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoDisponibleId}");

            DateTime manana = DateTime.Today.AddDays(1);
            LlenarYEnviarReserva(manana, "10:00", "12:00");

            // Esperar a que redirija fuera de /Crear hacia Mis Reservas
            wait.Until(d => !d.Url.Contains("/Crear"));

            string texto = ObtenerTextoPantalla();
            Assert.That(texto, Does.Contain("Mis reservas"));
            Assert.That(texto, Does.Contain("Pendiente"));
        }

        [Test, Order(2)]
        public void CP14_Reserva_EquipoPrestado_MuestraAvisoYOcultaBotonEnviar()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoPrestadoId}");

            string texto = ObtenerTextoPantalla();
            Assert.That(texto, Does.Contain("Este equipo está prestado"));

            var botonesEnviar = driver.FindElements(By.XPath("//button[contains(text(), 'Enviar solicitud')]"));
            Assert.That(botonesEnviar.Count, Is.EqualTo(0));
        }

        [Test, Order(3)]
        public void CP15_Reserva_HorarioCruzadoConReservaAprobada_MuestraError()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoConCruceId}");

            // Mañana de 14:30 a 15:30 (se cruza con la reserva SQL de 14:00 a 16:00)
            DateTime manana = DateTime.Today.AddDays(1);
            LlenarYEnviarReserva(manana, "14:30", "15:30");

            Assert.That(ObtenerTextoPantalla(), Does.Contain("El equipo ya tiene una reserva aprobada que se cruza con ese horario."));
        }

        [Test, Order(4)]
        public void CP16_Reserva_SegundoEquipoEnMismoHorario_MuestraError()
        {
            // Usa otro equipo en el mismo horario que CP-13 (mañana 10:00-12:00)
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoConCruceId}");

            DateTime manana = DateTime.Today.AddDays(1);
            LlenarYEnviarReserva(manana, "10:00", "12:00");

            Assert.That(ObtenerTextoPantalla(), Does.Contain("Ya tienes una solicitud pendiente o aprobada en ese horario. Solo puedes reservar un equipo a la vez."));
        }

        [Test, Order(5)]
        public void CP17_Reserva_HoraFinAnteriorAInicio_MuestraError()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoDisponibleId}");

            DateTime pasadoManana = DateTime.Today.AddDays(2);
            LlenarYEnviarReserva(pasadoManana, "14:00", "12:00");

            Assert.That(ObtenerTextoPantalla(), Does.Contain("La hora de fin debe ser posterior a la hora de inicio en la misma fecha."));
        }

        [Test, Order(6)]
        public void CP18_Reserva_EnviarSinFechaUHora_MuestraValidacion()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoDisponibleId}");

            var js = (IJavaScriptExecutor)driver;
            // Limpiar cualquier valor por defecto que venga precargado en Fecha/Horas
            js.ExecuteScript("document.getElementById('Fecha').value = '';");
            js.ExecuteScript("document.getElementById('HoraInicio').value = '';");
            js.ExecuteScript("document.getElementById('HoraFin').value = '';");

            driver.FindElement(By.XPath("//button[contains(normalize-space(), 'Enviar solicitud')]")).Click();

            string texto = ObtenerTextoPantalla();
            bool tieneMensajeServidor = texto.Contains("Completa la fecha y las horas de uso.");
            bool tieneErrorCampo = driver.FindElements(By.CssSelector(".field-error, .alert-danger")).Count > 0 && !string.IsNullOrWhiteSpace(texto);

            Assert.That(tieneMensajeServidor || tieneErrorCampo, Is.True);
        }

        [Test, Order(7)]
        public void CP19_Extra_Reserva_FechaPasada_MuestraError()
        {
            driver.Navigate().GoToUrl($"{BaseUrl}/Reservas/Crear?equipoId={EquipoDisponibleId}");

            DateTime ayer = DateTime.Today.AddDays(-1);
            // Quitamos el atributo 'min' del input para probar la validación de negocio en ReservaService
            LlenarYEnviarReserva(ayer, "10:00", "12:00", quitarMinimoHtml: true);

            Assert.That(ObtenerTextoPantalla(), Does.Contain("La reserva debe comenzar en una fecha y hora futuras"));
        }
    }
}