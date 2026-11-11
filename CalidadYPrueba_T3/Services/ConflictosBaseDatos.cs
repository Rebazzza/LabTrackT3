using Microsoft.Data.SqlClient;

namespace CalidadYPrueba_T3.Services;

internal static class ConflictosBaseDatos
{
    public static bool EsBloqueoTemporal(Exception error)
    {
        // EF puede envolver SqlException en DbUpdateException e InvalidOperationException.
        for (Exception? actual = error; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException { Number: 1205 or 1222 }) return true;
        }
        return false;
    }
}
