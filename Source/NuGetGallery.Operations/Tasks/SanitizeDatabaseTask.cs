﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using AnglicanGeek.DbExecutor;

namespace NuGetGallery.Operations.Tasks
{
    [Command("sanitizedatabase", "Cleans Personally-Identified Information out of a database without destroying data", AltName = "sdb", MinArgs = 0, MaxArgs = 0)]
    public class SanitizeDatabaseTask : DatabaseTask
    {
        private ICollection<string> _unsanitizedUsers = new List<string>();

        private static readonly string[] AllowedPrefixes = new[] {
            "Export_" // Only exports can be sanitized
        };

        [Option("The database name on the server to santize if different from the database identified in the connection string", AltName = "d")]
        public string DatabaseName { get; set; }

        [Option("Semicolon-separated list of users to IGNORE when santizing", AltName = "u")]
        public ICollection<string> UnsanitizedUsers
        {
            get { return _unsanitizedUsers; }
            set { _unsanitizedUsers = value; }
        }

        [Option("Domain name to use for sanitized email addresses, username@[emaildomain]", AltName = "e")]
        public string EmailDomain { get; set; }

        [Option("Forces the command to run, even against a non-backup/export database", AltName = "f")]
        public bool Force { get; set; }

        public override void ValidateArguments()
        {
            base.ValidateArguments();
            EmailDomain = String.IsNullOrEmpty(EmailDomain) ?
                "example.com" :
                EmailDomain;
        }

        public override void ExecuteCommand()
        {
            // Coalesce the database name
            DatabaseName = String.IsNullOrEmpty(DatabaseName) ?
                ConnectionString.InitialCatalog :
                DatabaseName;

            ConnectionString = new SqlConnectionStringBuilder(ConnectionString.ConnectionString)
            {
                InitialCatalog = DatabaseName
            };

            // Verify the name
            if (!Force && !AllowedPrefixes.Any(p => ConnectionString.InitialCatalog.StartsWith(p)))
            {
                Log.Error("Cannot santize {0} without -Force argument", ConnectionString.InitialCatalog);
                return;
            }
            Log.Info("Ready to santize {0} on {1}", ConnectionString.InitialCatalog, Util.GetDatabaseServerName(ConnectionString));

            // Build the IN clause to exclude allowed users. We trust the Unsanitized users data
            string inClause = String.Join(",", UnsanitizedUsers.Select(u => "'" + u + "'"));
            string query = String.Format(@"
                UPDATE Users
                SET    ApiKey = NEWID(),
                       EmailAddress = [Username] + '@{0}',
                       UnconfirmedEmailAddress = NULL,
                       HashedPassword = CAST(NEWID() AS NVARCHAR(MAX)),
                       EmailAllowed = 1,
                       EmailConfirmationToken = NULL,
                       PasswordResetToken = NULL,
                       PasswordResetTokenExpirationDate = NULL,
                       PasswordHashAlgorithm = 'PBKDF2'
               WHERE   Username NOT IN ({1})
            ", EmailDomain, inClause);

            // All we need to sanitize is the user table. Package data is public (EVEN unlisted ones) and not PII
            if (WhatIf)
            {
                Log.Trace("Would execute the following SQL:");
                Log.Trace(query);
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectionString))
                using (SqlExecutor dbExecutor = new SqlExecutor(connection))
                {
                    connection.Open();
                    try
                    {
                        var count = dbExecutor.Execute(query);
                        Log.Info("Sanitization complete. {0} Users affected", count);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
        }
    }
}
