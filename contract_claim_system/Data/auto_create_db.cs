using System;
using System.Data.SqlClient;

namespace contract_claim_system.Data
{
    public class auto_create_db
    {
        private readonly string _connectionString;
        private readonly string _serverConnectionString;

        public auto_create_db()
        {
            //server name
            string serverName = "LabVM1846780\\SQLEXPRESS";

            _serverConnectionString = $"Server={serverName};Integrated Security=true;";
            _connectionString = $"Server={serverName};Database=claims_database;Integrated Security=true;";

            Console.WriteLine($"Using SQL Server: {serverName}");
        }

        public void InitializeSystem()
        {
            try
            {
                Console.WriteLine("=== DATABASE INITIALIZATION STARTED ===");
                Console.WriteLine($"Server: LabVM1846780\\SQLEXPRESS");

                CreateDatabase();
                CreateTables();
                CreateTestUsers();

                Console.WriteLine("=== DATABASE INITIALIZATION COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== DATABASE INITIALIZATION FAILED: {ex.Message} ===");
                // Don't throw - let the app continue
                Console.WriteLine("Application will continue, database operations might fail.");
            }
        }

        private void CreateDatabase()
        {
            try
            {
                Console.WriteLine("Checking if database 'claims_database' exists...");

                using (var con = new SqlConnection(_serverConnectionString))
                {
                    con.Open();

                    // Check if database exists
                    var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM master.dbo.sysdatabases 
                        WHERE name = 'claims_database'", con);

                    var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                    if (!exists)
                    {
                        Console.WriteLine("Creating database 'claims_database'...");
                        var createCmd = new SqlCommand("CREATE DATABASE claims_database", con);
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine("✓ Database 'claims_database' created successfully!");
                    }
                    else
                    {
                        Console.WriteLine("✓ Database 'claims_database' already exists.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating database: {ex.Message}");
                throw;
            }
        }

        private void CreateTables()
        {
            try
            {
                Console.WriteLine("Checking/Creating tables...");

                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Create Users table if not exists

                    // Add to CreateTables method
                    string createApprovalLogsTable = @"
CREATE TABLE ApprovalLogs(
    logID INT PRIMARY KEY IDENTITY(1,1),
    claimID INT,
    approvedBy NVARCHAR(100),
    role NVARCHAR(50),
    action NVARCHAR(200),
    timestamp DATETIME DEFAULT GETDATE()
)";

                    using (var cmd = new SqlCommand(createApprovalLogsTable, con))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("✓ ApprovalLogs table checked/created");
                    }




                    string createUsersTable = @"
    CREATE TABLE Users(
        userID INT PRIMARY KEY IDENTITY(1,1),
        full_names VARCHAR(100),
        surname VARCHAR(100),
        email VARCHAR(100),
        role VARCHAR(100),
        gender VARCHAR(100),
        password VARCHAR(100),
        date DATETIME  -- Changed from DATE to DATETIME
    )";

                    string createClaimsTable = @"
    CREATE TABLE Claims(
        claimID INT PRIMARY KEY IDENTITY(1,1),
        number_of_sessions INT,
        number_of_hours INT,
        amount_of_rate INT,
        module_name VARCHAR(100),
        faculty_name VARCHAR(100),
        supporting_documents VARCHAR(100),
        claim_status VARCHAR(100),
        creating_date DATETIME,  -- Changed from DATE to DATETIME
        lecturerID INT FOREIGN KEY REFERENCES Users(userID)
    )";

                    using (var cmd = new SqlCommand(createUsersTable, con))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("✓ Users table checked/created");
                    }

                    using (var cmd = new SqlCommand(createClaimsTable, con))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("✓ Claims table checked/created");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating tables: {ex.Message}");
                throw;
            }
        }

        private void CreateTestUsers()
        {
            try
            {
                Console.WriteLine("Checking/Creating test users...");

                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Check if any users exist
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users", con);
                    var userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (userCount == 0)
                    {
                        Console.WriteLine("No users found. Creating test users...");

                        string addTestUsers = @"
                            INSERT INTO Users (full_names, surname, email, role, gender, password, date) 
                            VALUES 
                            ('John', 'Doe', 'john.doe@university.com', 'Lecturer', 'Male', 'password123', GETDATE()),
                            ('Sarah', 'Smith', 'sarah.smith@university.com', 'Lecturer', 'Female', 'password123', GETDATE()),
                            ('Mike', 'Johnson', 'mike.johnson@university.com', 'Lecturer', 'Male', 'password123', GETDATE()),
                            ('Admin', 'User', 'admin@university.com', 'Admin', 'Male', 'admin123', GETDATE())";

                        using (var cmd = new SqlCommand(addTestUsers, con))
                        {
                            int rowsAffected = cmd.ExecuteNonQuery();
                            Console.WriteLine($"✓ Created {rowsAffected} test users successfully!");
                        }

                        // Show the created users
                        var displayCmd = new SqlCommand("SELECT userID, full_names, surname FROM Users", con);
                        using (var reader = displayCmd.ExecuteReader())
                        {
                            Console.WriteLine("Available User IDs:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"  - ID: {reader["userID"]}, Name: {reader["full_names"]} {reader["surname"]}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✓ Database already contains {userCount} users.");

                        // Show existing users
                        var displayCmd = new SqlCommand("SELECT userID, full_names, surname FROM Users", con);
                        using (var reader = displayCmd.ExecuteReader())
                        {
                            Console.WriteLine("Existing User IDs:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"  - ID: {reader["userID"]}, Name: {reader["full_names"]} {reader["surname"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating test users: {ex.Message}");
                throw;
            }
        }

        // Utility method to check database status
        public void CheckStatus()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    var usersCmd = new SqlCommand("SELECT COUNT(*) FROM Users", con);
                    var userCount = Convert.ToInt32(usersCmd.ExecuteScalar());

                    var claimsCmd = new SqlCommand("SELECT COUNT(*) FROM Claims", con);
                    var claimCount = Convert.ToInt32(claimsCmd.ExecuteScalar());

                    Console.WriteLine($"Database Status - Users: {userCount}, Claims: {claimCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Status check failed: {ex.Message}");
            }
        }
    }
}