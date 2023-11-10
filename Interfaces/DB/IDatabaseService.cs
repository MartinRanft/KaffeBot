using System.Data;
using MySqlConnector;

namespace KaffeBot.Interfaces.DB
{
    ///<summary>
    /// Schnittstelle für den Datenbankdienst, um gespeicherte Prozeduren und SQL-Abfragen auszuführen.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Führt eine gespeicherte Prozedur in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="procedureName">Der Name der gespeicherten Prozedur.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Prozedurparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Prozedur.</returns>
        DataTable ExecuteStoredProcedure(string procedureName, MySqlParameter[] parameters);

        /// <summary>
        /// Führt eine gespeicherte Funktion in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="functionName">Der Name der gespeicherten Funktion.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Funktionseingabeparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Funktion.</returns>
        DataTable ExecuteFunction(string functionName, MySqlParameter[] parameters);

        /// <summary>
        /// Führt eine gespeicherte Funktion in der Datenbank aus und gibt den zurückgegebenen Skalarwert zurück.
        /// </summary>
        /// <param name="functionName">Der Name der gespeicherten Funktion.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Funktionseingabeparameter.</param>
        /// <returns>Der zurückgegebene Skalarwert der Funktion oder null, wenn kein Wert zurückgegeben wurde.</returns>
        object ExecuteScalarFunction(string functionName, MySqlParameter[] parameters);

        /// <summary>
        /// Führt eine SQL-Abfrage in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="query">Die SQL-Abfrage, die ausgeführt werden soll.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Abfrageparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Abfrage.</returns>
        DataTable ExecuteSqlQuery(string query, MySqlParameter[] parameters);
    }
}
