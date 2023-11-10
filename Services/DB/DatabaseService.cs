﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KaffeBot.Interfaces.DB;
using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Services.DB
{
    /// <summary>
    /// Implementierung des Datenbankdiensts, um gespeicherte Prozeduren und SQL-Abfragen auszuführen.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Konstruktor für den DatabaseService, der die Verbindungszeichenfolge zur Datenbank aus der Konfiguration liest.
        /// </summary>
        /// <param name="configuration">Eine IConfiguration-Instanz, die auf die Anwendungs- und Konfigurationsdaten zugreift.</param>
        public DatabaseService(IConfiguration configuration)
        {
            if(configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration is required.");
            }

            _connectionString = configuration.GetConnectionString("MariaDBConnection") ?? throw new ArgumentNullException("", "Connection string is not configured.");
        }

        /// <summary>
        /// Führt eine gespeicherte Prozedur in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="procedureName">Der Name der gespeicherten Prozedur.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Prozedurparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Prozedur.</returns>
        public DataTable ExecuteStoredProcedure(string procedureName, MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using var command = new MySqlCommand(procedureName, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);

            using var adapter = new MySqlDataAdapter(command);
            var result = new DataTable();
            adapter.Fill(result);

            // Überprüfen und behandeln Sie DBNull-Werte in der Ergebnismenge
            for(int rowIndex = 0; rowIndex < result.Rows.Count; rowIndex++)
            {
                for(int colIndex = 0; colIndex < result.Columns.Count; colIndex++)
                {
                    if(result.Rows[rowIndex][colIndex] == DBNull.Value)
                    {
                        // Hier können Sie einen Standardwert setzen oder eine geeignete Aktion ausführen.
                        // Zum Beispiel, um einen leeren String zu verwenden:
                        result.Rows[rowIndex][colIndex] = string.Empty;
                        // Oder um einen Standardwert für numerische Werte zu verwenden:
                        // result.Rows[rowIndex][colIndex] = 0;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Führt eine SQL-Abfrage in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="query">Die SQL-Abfrage, die ausgeführt werden soll.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Abfrageparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Abfrage.</returns>
        public DataTable ExecuteSqlQuery(string query, MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddRange(parameters);

            using var adapter = new MySqlDataAdapter(command);
            DataTable result = new();
            adapter.Fill(result);

            // Überprüfen und behandeln Sie DBNull-Werte in der Ergebnismenge, wie im vorherigen Beispiel gezeigt.

            return result;
        }

        /// <summary>
        /// Führt eine gespeicherte Funktion in der Datenbank aus und gibt die Ergebnisse in Form eines DataTable zurück.
        /// </summary>
        /// <param name="functionName">Der Name der gespeicherten Funktion.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Funktionseingabeparameter.</param>
        /// <returns>Ein DataTable mit den Ergebnissen der Funktion.</returns>
        public DataTable ExecuteFunction(string functionName, MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using var command = new MySqlCommand(functionName, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);

            using var adapter = new MySqlDataAdapter(command);
            var result = new DataTable();
            adapter.Fill(result);

            return result;
        }

        /// <summary>
        /// Führt eine gespeicherte Funktion in der Datenbank aus und gibt den zurückgegebenen Skalarwert zurück.
        /// </summary>
        /// <param name="functionName">Der Name der gespeicherten Funktion.</param>
        /// <param name="parameters">Ein Array von MySqlParameter-Objekten für die Funktionseingabeparameter.</param>
        /// <returns>Der zurückgegebene Skalarwert der Funktion.</returns>
        public object ExecuteScalarFunction(string functionName, MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using var command = new MySqlCommand(functionName, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);

            command.ExecuteScalar();

            object result = command!.Parameters[parameters.Length - 1]!.Value!;

            // Überprüfen, ob das Ergebnis null ist, und in diesem Fall einen Standardwert zurückgeben
            if(result == null || result == DBNull.Value)
            {
                // Hier können Sie einen Standardwert zurückgeben, z.B. 0 oder null, je nach Bedarf
                return 0; // oder return null;
            }

            return result;
        }
    }
}