using System.Data.SqlClient;
using contract_claim_system.Models;
using Microsoft.Extensions.Configuration;

namespace contract_claim_system.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public User Authenticate(string email, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT userID, full_names, surname, email, role, gender, password, date 
                    FROM Users 
                    WHERE email = @Email";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var hashedPassword = reader["password"]?.ToString();

                            // Simple password verification (in production, use proper hashing)
                            if (hashedPassword == password) // For demo purposes only
                            {
                                return new User
                                {
                                    userID = Convert.ToInt32(reader["userID"]),
                                    full_names = reader["full_names"]?.ToString(),
                                    surname = reader["surname"]?.ToString(),
                                    email = reader["email"]?.ToString(),
                                    role = reader["role"]?.ToString(),
                                    gender = reader["gender"]?.ToString(),
                                    date = Convert.ToDateTime(reader["date"])
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool Register(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO Users (full_names, surname, email, role, gender, password, date)
                    VALUES (@FullNames, @Surname, @Email, @Role, @Gender, @Password, @Date)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FullNames", user.full_names);
                    command.Parameters.AddWithValue("@Surname", user.surname);
                    command.Parameters.AddWithValue("@Email", user.email);
                    command.Parameters.AddWithValue("@Role", user.role ?? "Lecturer");
                    command.Parameters.AddWithValue("@Gender", user.gender);
                    command.Parameters.AddWithValue("@Password", user.password); // In production, hash this
                    command.Parameters.AddWithValue("@Date", DateTime.Now);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EmailExists(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM Users WHERE email = @Email";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public User GetUserById(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT userID, full_names, surname, email, role, gender, date 
                    FROM Users 
                    WHERE userID = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                userID = Convert.ToInt32(reader["userID"]),
                                full_names = reader["full_names"]?.ToString(),
                                surname = reader["surname"]?.ToString(),
                                email = reader["email"]?.ToString(),
                                role = reader["role"]?.ToString(),
                                gender = reader["gender"]?.ToString(),
                                date = Convert.ToDateTime(reader["date"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool UpdateUser(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE Users 
                    SET full_names = @FullNames, surname = @Surname, email = @Email, 
                        role = @Role, gender = @Gender 
                    WHERE userID = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FullNames", user.full_names);
                    command.Parameters.AddWithValue("@Surname", user.surname);
                    command.Parameters.AddWithValue("@Email", user.email);
                    command.Parameters.AddWithValue("@Role", user.role);
                    command.Parameters.AddWithValue("@Gender", user.gender);
                    command.Parameters.AddWithValue("@UserId", user.userID);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool ChangePassword(int userId, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "UPDATE Users SET password = @Password WHERE userID = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Password", newPassword); // In production, hash this
                    command.Parameters.AddWithValue("@UserId", userId);

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        // Optional: Method to get all users (for admin purposes)
        public List<User> GetAllUsers()
        {
            var users = new List<User>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT userID, full_names, surname, email, role, gender, date FROM Users ORDER BY surname, full_names";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                userID = Convert.ToInt32(reader["userID"]),
                                full_names = reader["full_names"]?.ToString(),
                                surname = reader["surname"]?.ToString(),
                                email = reader["email"]?.ToString(),
                                role = reader["role"]?.ToString(),
                                gender = reader["gender"]?.ToString(),
                                date = Convert.ToDateTime(reader["date"])
                            });
                        }
                    }
                }
            }
            return users;
        }
    }
}