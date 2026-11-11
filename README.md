# LabTrack

Aplicación de gestión de préstamos de equipos de laboratorio construida con ASP.NET Core MVC sobre .NET 10. Incluye acceso por rol, catálogo, solicitudes de reserva, aprobación y rechazo, devoluciones y penalización por retraso.

## Dependencias

- Entity Framework Core SQL Server 10.0.12.
- ASP.NET Core Identity Entity Framework Core 10.0.12.
- Entity Framework Core Design y Tools 10.0.12, para migraciones desde CLI y Visual Studio.
- Herramienta local `dotnet-ef` 10.0.12, fijada en `dotnet-tools.json`.
- System.Security.Cryptography.Pkcs 10.0.13, referencia explícita para reemplazar la versión indirecta vulnerable incluida por el proveedor SQL.
- Bootstrap, jQuery y validación de formularios ya incluidos en la plantilla MVC.

## Ejecución en desarrollo

Requisitos: SDK .NET 10 y SQL Server LocalDB con la instancia `MSSQLLocalDB`.

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet restore
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project CalidadYPrueba_T3
dotnet run --project CalidadYPrueba_T3 --launch-profile https
```

El perfil HTTPS utiliza `https://localhost:7116`. Las cookies de autenticación requieren HTTPS. Si el certificado de desarrollo no está configurado, ejecutar `dotnet dev-certs https --trust`.

La conexión local está en `CalidadYPrueba_T3/appsettings.Development.json` y usa autenticación integrada de Windows, sin contraseñas. La base se llama `LabTrackDb`. Las migraciones se aplican explícitamente; arrancar la aplicación no altera el esquema automáticamente.

Para otro servidor, configurar `ConnectionStrings__LabTrack` mediante una variable de entorno o un proveedor de secretos. El entorno de producción no utiliza la conexión LocalDB de desarrollo. No guardar credenciales en archivos versionados.

## Organización

- `Models`: entidades persistentes y estados del dominio.
- `Data/LabTrackDbContext.cs`: contexto EF Core integrado con Identity.
- `Data/Configurations`: relaciones, índices, límites y restricciones SQL.
- `Data/Migrations`: esquema versionado y snapshot del modelo.
- `Data/DatosIniciales.cs`: categorías y roles fijos.
- `Security`: nombres de roles compartidos.
- `Controllers` y `Views`: interfaz MVC existente; se ampliará por funcionalidad.

`Services/CatalogoService.cs` concentra las consultas de catálogo y el alta de equipos. Los controladores de reservas, solicitudes y devoluciones delegan las reglas de negocio en sus servicios. Los ViewModels definen los datos de las pantallas.

## Modelo de datos

| Entidad | Relación y propósito |
| --- | --- |
| Usuario | Extiende IdentityUser; `UserName` y su valor normalizado representan el código de acceso único. Incluye nombre completo y estado Activo/Penalizado. |
| Categoría | Agrupa muchos equipos; nombre único. |
| Equipo | Pertenece a una categoría; inventario único; puede tener múltiples reservas en horarios diferentes. |
| Reserva | Pertenece a un estudiante y un equipo; conserva horario, resolución, responsable, rechazo y devolución física. |
| Penalización | Pertenece a una única reserva; el estudiante se obtiene de esa reserva, evitando duplicar la relación. |

Identity agrega sus tablas de usuarios, roles, asignaciones, claims, logins y tokens. El rol del encargado se llama `Encargado` y el del estudiante `Estudiante`.

La migración inicial inserta las categorías **Cómputo**, **Electrónica** y **Redes**, y los dos roles. No crea cuentas con contraseñas predefinidas ni equipos ficticios.

## Integridad y decisiones del modelo

- Las fechas se guardan en UTC con `DateTimeOffset` y se muestran en hora de Lima.
- Estados de reserva: Pendiente, Aprobada, Rechazada y Devuelta.
- SQL exige inicio anterior al fin, estados válidos, motivo de rechazo no vacío y datos de resolución/devolución coherentes con el estado.
- Las eliminaciones de categorías, equipos, estudiantes, encargados o reservas con historial relacionado están restringidas.
- Los índices cubren búsquedas por equipo, estudiante, estado y horario; también la bandeja de pendientes.
- Usuario, Equipo y Reserva tienen columnas `rowversion` para detectar modificaciones concurrentes. Esto no impide por sí solo reservas superpuestas.
- Disponible/Prestado se calculará al consultar el horario y las devoluciones pendientes. No se guarda un estado global que invalide reservas en otros bloques.
- El login invoca Identity con `lockoutOnFailure: true`: la cuenta se bloquea tras tres fallos consecutivos durante 15 minutos, ajustables mediante `LabTrack:BloqueoCuentaMinutos`. Un acceso correcto reinicia el contador de fallos.
- Las rutas `/Cuenta/Login` y `/Cuenta/AccesoDenegado` son públicas. Las demás acciones MVC requieren autenticación por defecto, excepto la página de error. Los paneles exigen además su rol correspondiente.
- El cierre de sesión es un POST con token antifalsificación. Los formularios MVC tienen validación antifalsificación global. Los destinos de retorno del login solo admiten rutas locales.
- La condición Penalizado se muestra al estudiante sin impedir el acceso a su cuenta; el efecto sobre las reservas sigue pendiente de definición.

## Crear las primeras cuentas

El alta se realiza desde la consola del servidor con Identity. No hay contraseñas predeterminadas, registro público ni selección de roles en el navegador. El comando no modifica usuarios existentes y guarda usuario y rol dentro de una transacción.

Desde la raíz, en PowerShell, configura el código, el nombre y el rol. El rol debe ser exactamente `Encargado` o `Estudiante`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AltaUsuario__Codigo = Read-Host 'Código de usuario'
$env:AltaUsuario__Nombre = Read-Host 'Nombre completo'
$env:AltaUsuario__Rol = Read-Host 'Rol (Encargado o Estudiante)'
$labTrackClave = Read-Host 'Contraseña' -AsSecureString
try {
    $env:AltaUsuario__Password = [System.Net.NetworkCredential]::new('', $labTrackClave).Password
    dotnet run --project CalidadYPrueba_T3 --no-launch-profile -- --crear-usuario
} finally {
    Remove-Item Env:AltaUsuario__Password -ErrorAction SilentlyContinue
    Remove-Item Env:AltaUsuario__Codigo -ErrorAction SilentlyContinue
    Remove-Item Env:AltaUsuario__Nombre -ErrorAction SilentlyContinue
    Remove-Item Env:AltaUsuario__Rol -ErrorAction SilentlyContinue
    $labTrackClave.Dispose()
}
```

La contraseña debe cumplir la política predeterminada de Identity: al menos seis caracteres, con mayúscula, minúscula, número y símbolo. Los códigos solo admiten letras ASCII, números y los caracteres `-._@+`, según la configuración predeterminada de Identity. Los nombres completos admiten tildes y espacios. La contraseña se solicita sin mostrarla ni incorporarla al historial de comandos; la variable de entorno se elimina al finalizar.

Ejecuta después el perfil HTTPS e ingresa en `https://localhost:7116/Cuenta/Login`. El estudiante accede a `/Estudiante/Index` y el encargado a `/Encargado/Index`. Los indicadores de cada panel se calculan con los registros reales de la base de datos; el panel de estudiante filtra exclusivamente sus reservas.

## Catálogo e inventario

Ambos roles pueden abrir `/Catalogo` desde la navegación o desde su panel. El catálogo muestra nombre, código de inventario, categoría y estado Disponible/Prestado. El filtro utiliza la categoría exacta seleccionada entre Cómputo, Electrónica y Redes; `Limpiar filtro` devuelve el listado completo. Una categoría vacía muestra el mensaje exigido: `No hay equipos disponibles en esta categoría`.

La disponibilidad corresponde al momento de la consulta. Una reserva aprobada cuyo inicio ya llegó mantiene el equipo Prestado hasta su devolución física, incluso si venció su horario. El sistema comprueba la disponibilidad para el bloque solicitado al crear y aprobar la reserva.

La consulta utiliza la hora UTC de `TimeProvider`, proyecta solo los datos necesarios y obtiene páginas de 12 equipos ordenados por nombre e identificador. Los enlaces de paginación conservan la categoría. Filtros mal formados, categorías inexistentes y páginas menores que 1 reciben una respuesta 400 con un mensaje visible. Una página superior al total se ajusta a la última existente.

El encargado puede abrir `Registrar equipo`, completar nombre, código y categoría y guardar el equipo en la base de datos. El código se normaliza a mayúsculas y se eliminan espacios al inicio y final. El índice único protege también frente a altas simultáneas con el mismo código. El estudiante no puede acceder a esta acción mediante GET ni POST. No se insertan equipos de demostración automáticamente.

## Devoluciones y penalización

El encargado abre `Devoluciones` desde la navegación o el panel. Puede buscar préstamos iniciados por nombre o código del estudiante o del equipo. Al seleccionar `Marcar devolución`, LabTrack guarda la hora del servidor y el encargado responsable y libera el equipo en el catálogo.

Cuando la devolución ocurre después del fin acordado, LabTrack registra una penalización para la reserva y cambia el estado del estudiante a Penalizado. La devolución y la penalización se guardan juntas. El estudiante consulta la fecha de devolución y el motivo en `Mis reservas`. El efecto de la penalización sobre futuras solicitudes queda pendiente de definición.

## Solicitudes de reserva

El estudiante selecciona `Solicitar reserva` en un equipo Disponible del catálogo. En `/Reservas/Crear?equipoId=...` indica fecha, hora de inicio y hora de fin, todas en hora de Lima. La aplicación convierte el horario a UTC antes de guardarlo. El formulario muestra hasta diez horarios aprobados próximos; la validación considera todos los registros.

Las solicitudes se guardan como Pendiente y aparecen en `/Reservas` o `Mis reservas`, con páginas de diez registros y las más recientes primero. El listado solo consulta el usuario autenticado, sin aceptar un identificador de estudiante desde el navegador. Incluye estado, horario, motivo de rechazo y fecha de devolución cuando existan esos datos.

Reglas implementadas en `Services/ReservaService.cs`:

- Inicio futuro y fin posterior al inicio, dentro de la misma fecha.
- El equipo debe existir y no tener un préstamo aprobado ya iniciado sin devolución física.
- No puede superponerse con una reserva aprobada del mismo equipo.
- El estudiante no puede tener solicitudes Pendientes ni Aprobadas en intervalos superpuestos, aunque sean equipos distintos.
- Los límites se consideran como intervalos `[inicio, fin)`: se permiten bloques consecutivos.
- La creación usa una transacción Serializable para mantener juntas la comprobación y la inserción. Si SQL Server detecta un interbloqueo o agotamiento del tiempo de bloqueo, se muestra un mensaje para reintentar sin guardar una solicitud parcial.
- El estudiante y el estado inicial se asignan en el servidor. No se aceptan desde el formulario. El encargado no puede enviar solicitudes como estudiante.

Las solicitudes pendientes de estudiantes distintos pueden coincidir sobre un equipo; ninguna garantiza el préstamo hasta su aprobación. El módulo de aprobación revalida esos conflictos. Un equipo prestado debe devolverse antes de poder solicitarlo de nuevo. La condición Penalizado todavía no restringe solicitudes porque su efecto no está definido en los documentos.

## Aprobación y rechazo de solicitudes

El encargado abre `/Solicitudes` desde la navegación o desde `Revisar solicitudes` en su panel. La tabla muestra nombre y código del estudiante, equipo e inventario, fecha y horas de uso y las acciones Aprobar/Rechazar. Se muestran diez solicitudes por página, de la más antigua a la más reciente. Las solicitudes cuyo horario ya finalizó se pueden rechazar, pero no aprobar.

La aprobación se realiza mediante POST con token antifalsificación. `SolicitudService` vuelve a leer la solicitud y comprueba que siga Pendiente y que su versión coincida con la que se mostró al encargado. Dentro de una transacción Serializable valida que el horario no haya finalizado, que el equipo no esté pendiente de devolución, que no haya otra aprobación incompatible para ese equipo y que el estudiante no tenga otro equipo aprobado simultáneamente. Los bloques consecutivos se permiten.

El rechazo abre una pantalla con el detalle y exige un motivo de hasta 500 caracteres. El texto se guarda sin espacios al inicio y al final. Ambas resoluciones registran la cuenta del encargado y la fecha UTC, cambian el estado y retiran la solicitud de la bandeja. El estudiante ve el nuevo estado y, si corresponde, el motivo en `Mis reservas`.

Las decisiones ya guardadas no pueden sobrescribirse desde este módulo. Una pantalla desactualizada o una operación concurrente muestra un aviso sin cambiar el resultado. No se rechazan automáticamente otras solicitudes que queden en conflicto: el encargado puede revisarlas y justificar individualmente su decisión. El estado Disponible/Prestado del catálogo sigue calculándose según el bloque de tiempo y la devolución física; una aprobación futura no marca el equipo como Prestado antes de comenzar.

## Mantenimiento del esquema

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add NombreDelCambio --project CalidadYPrueba_T3 --output-dir Data/Migrations
dotnet ef database update --project CalidadYPrueba_T3
dotnet ef migrations has-pending-model-changes --project CalidadYPrueba_T3
```

Revisar cada migración antes de aplicarla a una base con información real.
