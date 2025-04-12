using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace OnlineBank
{
    class Database
    {
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "database.db");
        public static string connectionString = $"Data Source={dbPath}; Version=3;";

        public static void Initialize()
        {
            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Password TEXT NOT NULL,
                    Balance REAL DEFAULT 0 NOT NULL
                )";
                string createLoansTable = @"
                CREATE TABLE IF NOT EXISTS Loans (
                   ID INTEGER PRIMARY KEY AUTOINCREMENT,
                     UserID INTEGER NOT NULL,
                     AccountID INTEGER NOT NULL,
                     Amount REAL NOT NULL,
                     Term INTEGER NOT NULL,
                     Status TEXT DEFAULT 'Ожидание',
                     FOREIGN KEY(UserID) REFERENCES Users(ID),
                     FOREIGN KEY(AccountID) REFERENCES Accounts(ID)
                )";
                string createTransactionsTable = @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    Type TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Recipient TEXT,
                    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(UserID) REFERENCES Users(ID)
                 )";
                string createDepositsTable = @"
                CREATE TABLE IF NOT EXISTS Deposits (
                   ID INTEGER PRIMARY KEY AUTOINCREMENT,
                   UserId INTEGER NOT NULL,
                   Amount REAL NOT NULL,
                   Term INTEGER NOT NULL,
                   InterestRate REAL NOT NULL,
                   StartDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                   EndDate DATETIME NOT NULL,
                   FOREIGN KEY(UserId) REFERENCES Users(ID)
                )";
                string createAccountsTable = @"
               CREATE TABLE IF NOT EXISTS Accounts (
                   ID INTEGER PRIMARY KEY AUTOINCREMENT,
                   UserID INTEGER NOT NULL,
                   AccountName TEXT NOT NULL,
                   Balance REAL DEFAULT 0 NOT NULL,
                   FOREIGN KEY(UserID) REFERENCES Users(ID)
                )";
                using (var cmd = new SQLiteCommand(createAccountsTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }


                using (var cmd = new SQLiteCommand(createDepositsTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SQLiteCommand(createTransactionsTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SQLiteCommand(createUsersTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SQLiteCommand(createLoansTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                try
                {
                    var cmd = new SQLiteCommand("ALTER TABLE Loans ADD COLUMN AccountID INTEGER", connection);
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    
                }
                // Добавляем колонку AccountID в таблицу Deposits
                try
                {
                    var cmd = new SQLiteCommand("ALTER TABLE Deposits ADD COLUMN AccountID INTEGER", connection);
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    
                }
                try
                {
                    var cmd = new SQLiteCommand("ALTER TABLE Transactions ADD COLUMN AccountID INTEGER", connection);
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException) { 

                }


            }
        }
    
        
        public static List<Tuple<int, string>> GetUserAccounts(string username)
        {
            var accounts = new List<Tuple<int, string>>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string query = @"
            SELECT Accounts.ID, Accounts.AccountName
            FROM Accounts
            JOIN Users ON Accounts.UserID = Users.ID
            WHERE Users.Username = @username";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        accounts.Add(Tuple.Create(reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }
            return accounts;
        }
        public static bool CreateAccount(string username, string accountName)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string getUserId = "SELECT ID FROM Users WHERE Username=@username";
                using (var cmd = new SQLiteCommand(getUserId, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    object result = cmd.ExecuteScalar();
                    if (result == null) return false;

                    int userId = Convert.ToInt32(result);

                    string insert = "INSERT INTO Accounts (UserID, AccountName, Balance) VALUES (@userId, @accountName, 0)";
                    using (var insertCmd = new SQLiteCommand(insert, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", userId);
                        insertCmd.Parameters.AddWithValue("@accountName", accountName);
                        insertCmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
        }
        public static bool UpdateAccountBalance(int accountId, double amount)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Accounts SET Balance = Balance + @amount WHERE ID = @id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@id", accountId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public static double GetAccountBalance(int accountId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Balance FROM Accounts WHERE ID = @id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", accountId);
                    return Convert.ToDouble(cmd.ExecuteScalar());
                }
            }
        }
        public static List<string> GetAccounts(string username)
        {
            var result = new List<string>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = new SQLiteCommand(@"
            SELECT Id, AccountType FROM Accounts 
            WHERE Username = @Username", connection);
                command.Parameters.AddWithValue("@Username", username);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["Id"]);
                        string type = reader["AccountType"].ToString();
                        result.Add($"{type} (ID: {id})");
                    }
                }
            }

            return result;
        }
        public static bool RegisterUser(string username, string password)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username=@username";
                using (var checkCmd = new SQLiteCommand(checkUserQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@username", username);
                    int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (userExists > 0)
                    {
                        return false;
                    }
                }

                string insertQuery = "INSERT INTO Users (Username, Password, Balance) VALUES (@username, @password, 0)";
                using (var cmd = new SQLiteCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public static bool LoginUser(string username, string password)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username=@username AND Password=@password";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public static double GetBalance(string username)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Balance FROM Users WHERE Username = @username";
                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                return Convert.ToDouble(cmd.ExecuteScalar());
            }
        }

        public static bool UpdateBalance(string username, double amount)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE Users SET Balance = Balance + @amount WHERE Username = @username";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@username", username);

                    int rowsAffected = cmd.ExecuteNonQuery(); // Проверяем, были ли изменены строки
                    return rowsAffected > 0;
                }
            }
        }

        public static bool TransferMoney(string sender, string recipient, double amount)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteTransaction transaction = conn.BeginTransaction();

                try
                {
                    string checkBalanceQuery = "SELECT Balance FROM Users WHERE Username = @sender";
                    SQLiteCommand checkBalanceCmd = new SQLiteCommand(checkBalanceQuery, conn);
                    checkBalanceCmd.Parameters.AddWithValue("@sender", sender);
                    double senderBalance = Convert.ToDouble(checkBalanceCmd.ExecuteScalar());

                    if (senderBalance < amount)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    string deductMoneyQuery = "UPDATE Users SET Balance = Balance - @amount WHERE Username = @sender";
                    SQLiteCommand deductMoneyCmd = new SQLiteCommand(deductMoneyQuery, conn);
                    deductMoneyCmd.Parameters.AddWithValue("@amount", amount);
                    deductMoneyCmd.Parameters.AddWithValue("@sender", sender);
                    deductMoneyCmd.ExecuteNonQuery();

                    string checkRecipientQuery = "SELECT COUNT(*) FROM Users WHERE Username = @recipient";
                    SQLiteCommand checkRecipientCmd = new SQLiteCommand(checkRecipientQuery, conn);
                    checkRecipientCmd.Parameters.AddWithValue("@recipient", recipient);
                    int recipientExists = Convert.ToInt32(checkRecipientCmd.ExecuteScalar());

                    if (recipientExists == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    string addMoneyQuery = "UPDATE Users SET Balance = Balance + @amount WHERE Username = @recipient";
                    SQLiteCommand addMoneyCmd = new SQLiteCommand(addMoneyQuery, conn);
                    addMoneyCmd.Parameters.AddWithValue("@amount", amount);
                    addMoneyCmd.Parameters.AddWithValue("@recipient", recipient);
                    addMoneyCmd.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
        public static bool ApplyLoan(string username, int accountId, double amount, int term)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получаем ID пользователя
                string getUserIdQuery = "SELECT ID FROM Users WHERE Username = @username";
                using (var getUserCmd = new SQLiteCommand(getUserIdQuery, connection))
                {
                    getUserCmd.Parameters.AddWithValue("@username", username);
                    object userIdObj = getUserCmd.ExecuteScalar();

                    if (userIdObj == null)
                        return false;

                    int userId = Convert.ToInt32(userIdObj);

                    // Вставляем кредит
                    string insertLoanQuery = @"
                INSERT INTO Loans (UserID, AccountID, Amount, Term)
                VALUES (@userId, @accountId, @amount, @term)";
                    using (var insertCmd = new SQLiteCommand(insertLoanQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", userId);
                        insertCmd.Parameters.AddWithValue("@accountId", accountId);
                        insertCmd.Parameters.AddWithValue("@amount", amount);
                        insertCmd.Parameters.AddWithValue("@term", term);
                        insertCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
        }

        public static List<string> GetLoans(string username)
        {
            List<string> loans = new List<string>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                SELECT Amount, Term, Status, CreatedAt 
                FROM Loans 
                WHERE UserID = (SELECT ID FROM Users WHERE Username = @username)";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            loans.Add($"Сумма: {reader.GetDouble(0)} руб, Срок: {reader.GetInt32(1)} мес, Статус: {reader.GetString(2)}, Дата: {reader.GetString(3)}");
                        }
                    }
                }
            }

            return loans;
        }
        public static void AddTransaction(string username, int accountId, string type, double amount, string recipient = null)

        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string getUserIdQuery = "SELECT ID FROM Users WHERE Username = @username";
                using (var getUserCmd = new SQLiteCommand(getUserIdQuery, connection))
                {
                    getUserCmd.Parameters.AddWithValue("@username", username);
                    int userId = Convert.ToInt32(getUserCmd.ExecuteScalar());

                    string insertTransactionQuery = @"
                INSERT INTO Transactions (UserId, AccountId, Type, Amount, Recipient, Date)
                VALUES (@userId, @accountId, @type, @amount, @recipient, @date)";
                    using (var cmd = new SQLiteCommand(insertTransactionQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@accountId", accountId);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.Parameters.AddWithValue("@amount", amount);
                        cmd.Parameters.AddWithValue("@recipient", recipient);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public static bool TransferToUserAccount(int senderAccountId, string recipientUsername, string recipientAccountName, double amount)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    // Получаем ID счёта получателя по логину и имени счёта
                    var getRecipientCmd = new SQLiteCommand(@"
                SELECT a.ID FROM Accounts a
                JOIN Users u ON a.UserID = u.ID
                WHERE u.Username = @username AND a.AccountName = @accountName
            ", connection);

                    getRecipientCmd.Parameters.AddWithValue("@username", recipientUsername);
                    getRecipientCmd.Parameters.AddWithValue("@accountName", recipientAccountName);

                    object result = getRecipientCmd.ExecuteScalar();
                    if (result == null)
                    {
                        transaction.Rollback();
                        return false; // Получатель не найден
                    }

                    int recipientAccountId = Convert.ToInt32(result);

                    // Проверяем баланс отправителя
                    var getSenderBalance = new SQLiteCommand("SELECT Balance FROM Accounts WHERE ID = @id", connection);
                    getSenderBalance.Parameters.AddWithValue("@id", senderAccountId);
                    double senderBalance = Convert.ToDouble(getSenderBalance.ExecuteScalar());

                    if (senderBalance < amount)
                    {
                        transaction.Rollback();
                        return false; // Недостаточно средств
                    }

                    // Списываем у отправителя
                    var deduct = new SQLiteCommand("UPDATE Accounts SET Balance = Balance - @amount WHERE ID = @id", connection);
                    deduct.Parameters.AddWithValue("@amount", amount);
                    deduct.Parameters.AddWithValue("@id", senderAccountId);
                    deduct.ExecuteNonQuery();

                    // Зачисляем получателю
                    var deposit = new SQLiteCommand("UPDATE Accounts SET Balance = Balance + @amount WHERE ID = @id", connection);
                    deposit.Parameters.AddWithValue("@amount", amount);
                    deposit.Parameters.AddWithValue("@id", recipientAccountId);
                    deposit.ExecuteNonQuery();

                    transaction.Commit();

                    // История операций
                    string senderUsername = GetUsernameByAccountId(senderAccountId);

                    if (!string.IsNullOrEmpty(senderUsername))
                    {
                        // Запись для отправителя
                        AddTransaction(senderUsername, senderAccountId, "Перевод", -amount, $"{recipientUsername}:{recipientAccountName}");

                        // Запись для получателя
                        AddTransaction(recipientUsername, recipientAccountId, "Получено от", amount, senderUsername);
                    }

                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public static string GetUsernameByAccountId(int accountId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var cmd = new SQLiteCommand(@"
            SELECT u.Username
            FROM Users u
            JOIN Accounts a ON a.UserID = u.ID
            WHERE a.ID = @accountId", connection);

                cmd.Parameters.AddWithValue("@accountId", accountId);
                object result = cmd.ExecuteScalar();

                return result?.ToString();
            }
        }


        public static bool TransferToOtherAccount(int fromAccountId, int toAccountId, double amount)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    //хватает ли средств?
                    var checkSender = new SQLiteCommand("SELECT Balance FROM Accounts WHERE ID = @id", connection);
                    checkSender.Parameters.AddWithValue("@id", fromAccountId);
                    double senderBalance = Convert.ToDouble(checkSender.ExecuteScalar());

                    if (senderBalance < amount)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // существует ли счёт-получатель?
                    var checkRecipient = new SQLiteCommand("SELECT COUNT(*) FROM Accounts WHERE ID = @id", connection);
                    checkRecipient.Parameters.AddWithValue("@id", toAccountId);
                    int exists = Convert.ToInt32(checkRecipient.ExecuteScalar());

                    if (exists == 0)
                    {
                        transaction.Rollback();
                        return false; // счёт не найден
                    }

                    // Списываем с отправителя
                    var deduct = new SQLiteCommand("UPDATE Accounts SET Balance = Balance - @amount WHERE ID = @id", connection);
                    deduct.Parameters.AddWithValue("@amount", amount);
                    deduct.Parameters.AddWithValue("@id", fromAccountId);
                    deduct.ExecuteNonQuery();

                    // Зачисляем получателю
                    var add = new SQLiteCommand("UPDATE Accounts SET Balance = Balance + @amount WHERE ID = @id", connection);
                    add.Parameters.AddWithValue("@amount", amount);
                    add.Parameters.AddWithValue("@id", toAccountId);
                    add.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public static bool TransferBetweenAccounts(int fromAccountId, int toAccountId, double amount)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    var getBalance = new SQLiteCommand("SELECT Balance FROM Accounts WHERE ID = @id", connection);
                    getBalance.Parameters.AddWithValue("@id", fromAccountId);
                    double current = Convert.ToDouble(getBalance.ExecuteScalar());

                    if (current < amount)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var withdraw = new SQLiteCommand("UPDATE Accounts SET Balance = Balance - @amount WHERE ID = @id", connection);
                    withdraw.Parameters.AddWithValue("@amount", amount);
                    withdraw.Parameters.AddWithValue("@id", fromAccountId);
                    withdraw.ExecuteNonQuery();

                    var deposit = new SQLiteCommand("UPDATE Accounts SET Balance = Balance + @amount WHERE ID = @id", connection);
                    deposit.Parameters.AddWithValue("@amount", amount);
                    deposit.Parameters.AddWithValue("@id", toAccountId);
                    deposit.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public static List<string> GetTransactions(int accountId)
        {
            var transactions = new List<string>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = new SQLiteCommand(@"
            SELECT Type, Amount, Recipient, Date
            FROM Transactions
            WHERE AccountID = @accountId
            ORDER BY Date DESC", connection);

                command.Parameters.AddWithValue("@accountId", accountId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string type = reader.GetString(0);
                        double amount = reader.GetDouble(1);
                        string recipient = reader.IsDBNull(2) ? "—" : reader.GetString(2);
                        string date = reader.GetString(3);

                        transactions.Add($"{date} | {type} | {amount} руб. | Кому: {recipient}");
                    }
                }
            }

            return transactions;
        }

        public static bool ApplyDeposit(string username, double amount, int term, double interestRate)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получаем ID пользователя
                string getUserIdQuery = "SELECT ID FROM Users WHERE Username = @username";
                using (var getUserCmd = new SQLiteCommand(getUserIdQuery, connection))
                {
                    getUserCmd.Parameters.AddWithValue("@username", username);
                    object userIdObj = getUserCmd.ExecuteScalar();

                    if (userIdObj == null)
                        return false; // Пользователь не найден

                    int userId = Convert.ToInt32(userIdObj);

                    // Рассчитываем дату окончания вклада
                    DateTime endDate = DateTime.Now.AddMonths(term);

                    // Вставляем вклад
                    string insertDepositQuery = "INSERT INTO Deposits (UserId, Amount, Term, InterestRate, EndDate) VALUES (@userId, @amount, @term, @interestRate, @endDate)";
                    using (var insertCmd = new SQLiteCommand(insertDepositQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", userId);
                        insertCmd.Parameters.AddWithValue("@amount", amount);
                        insertCmd.Parameters.AddWithValue("@term", term);
                        insertCmd.Parameters.AddWithValue("@interestRate", interestRate);
                        insertCmd.Parameters.AddWithValue("@endDate", endDate);
                        insertCmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
        }
        public static bool CreateDeposit(string username, int accountId, double amount, int term, double interestRate, DateTime startDate, DateTime endDate)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получаем ID пользователя
                string getUserIdQuery = "SELECT ID FROM Users WHERE Username = @username";
                using (var getUserCmd = new SQLiteCommand(getUserIdQuery, connection))
                {
                    getUserCmd.Parameters.AddWithValue("@username", username);
                    object userIdObj = getUserCmd.ExecuteScalar();

                    if (userIdObj == null) return false;

                    int userId = Convert.ToInt32(userIdObj);

                    // Вставка вклада
                    string insertQuery = @"
                INSERT INTO Deposits (UserId, AccountID, Amount, Term, InterestRate, StartDate, EndDate)
                VALUES (@userId, @accountId, @amount, @term, @interestRate, @startDate, @endDate)";
                    using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", userId);
                        insertCmd.Parameters.AddWithValue("@accountId", accountId);
                        insertCmd.Parameters.AddWithValue("@amount", amount);
                        insertCmd.Parameters.AddWithValue("@term", term);
                        insertCmd.Parameters.AddWithValue("@interestRate", interestRate);
                        insertCmd.Parameters.AddWithValue("@startDate", startDate);
                        insertCmd.Parameters.AddWithValue("@endDate", endDate);

                        insertCmd.ExecuteNonQuery();
                        return true;
                    }
                    }
                }
            }
        

        public static bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Проверяем, совпадает ли старый пароль
                var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @OldPassword", connection);
                checkCommand.Parameters.AddWithValue("@Username", username);
                checkCommand.Parameters.AddWithValue("@OldPassword", oldPassword);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (count == 0)
                {
                    return false; // Неверный текущий пароль
                }

                // Обновляем пароль
                var updateCommand = new SQLiteCommand("UPDATE Users SET Password = @NewPassword WHERE Username = @Username", connection);
                updateCommand.Parameters.AddWithValue("@Username", username);
                updateCommand.Parameters.AddWithValue("@NewPassword", newPassword);

                int rowsAffected = updateCommand.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
    }
}
    

