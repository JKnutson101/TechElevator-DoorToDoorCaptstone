﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DoorToDoorLibrary.Exceptions;
using DoorToDoorLibrary.DatabaseObjects;

namespace DoorToDoorLibrary.DAL
{
    public class DoorToDoorDAL : IDoorToDoorDAL
    {
        #region Properties and Variables

        private string _connectionString;

        private const string _getLastIdSql = "SELECT CAST(SCOPE_IDENTITY() AS int);";

        #endregion

        #region Constructor

        public DoorToDoorDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        #endregion

        #region User Methods

        /// <summary>
        /// Finds a single user in the database using the user's Email Address
        /// </summary>
        /// <param name="emailAddress">The desired user's Email Address</param>
        /// <returns>UserItem containing the user's information</returns>
        public UserItem GetUserItem(string emailAddress)
        {
            UserItem user = null;
            const string sql = "SELECT * From [Users] WHERE emailAddress = @EmailAddress;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EmailAddress", emailAddress.ToLower());

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    user = GetUserItemFromReader(reader);
                }
            }

            if (user == null)
            {
                throw new Exception("User does not exist.");
            }

            return user;
        }

        /// <summary>
        /// Generates a UserItem from the provided Sql Data Reader
        /// </summary>
        /// <param name="reader">The given Sql Data Reader</param>
        /// <returns>UserItem containing the information for a particular user</returns>
        private UserItem GetUserItemFromReader(SqlDataReader reader)
        {
            UserItem item = new UserItem();

            item.Id = Convert.ToInt32(reader["id"]);
            item.FirstName = Convert.ToString(reader["firstName"]);
            item.LastName = Convert.ToString(reader["lastName"]);
            item.EmailAddress = Convert.ToString(reader["emailAddress"]);
            item.Hash = Convert.ToString(reader["hash"]);
            item.Salt = Convert.ToString(reader["salt"]);
            item.RoleId = Convert.ToInt32(reader["roleID"]);
            item.UpdatePassword = Convert.ToBoolean(reader["updatePassword"]);

            return item;
        }

        /// <summary>
        /// Creates a new User in the database
        /// </summary>
        /// <param name="item">The user to be created</param>
        /// <returns>ID of the created User</returns>
        public int RegisterNewUser(UserItem item)
        {
            int ID = 0;

            const string sql = "INSERT INTO [Users] (firstName, lastName, emailAddress, hash, salt, roleID, updatePassword) " +
                               "VALUES (@FirstName, @LastName, @EmailAddress, @Hash, @Salt, @RoleId, 1);";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql + " " + _getLastIdSql, conn);
                cmd.Parameters.AddWithValue("@FirstName",  item.FirstName);
                cmd.Parameters.AddWithValue("@LastName", item.LastName);
                cmd.Parameters.AddWithValue("@EmailAddress", item.EmailAddress.ToLower());
                cmd.Parameters.AddWithValue("@Hash", item.Hash);
                cmd.Parameters.AddWithValue("@Salt", item.Salt);
                cmd.Parameters.AddWithValue("@RoleId", item.RoleId);

                try
                {
                    ID = (int)cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    throw ex;
                }
            }

            return ID;
        }

        /// <summary>
        /// Returns a list of all Manager-type users from the system for Admin use
        /// </summary>
        /// <returns>List of Mangaer users</returns>
        public IList<UserItem> GetAllManagers()
        {
            List<UserItem> output = new List<UserItem>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "SELECT * FROM [Users] WHERE roleID = (SELECT id FROM Roles WHERE [name] = 'Manager');";

                SqlCommand cmd = new SqlCommand(sql, conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    UserItem newuser = GetUserItemFromReader(reader);
                    output.Add(newuser);
                }
            }

            return output;
        }

        /// <summary>
        /// Returns a list of all Salesperson-type users from the system for a particular Manager
        /// </summary>
        /// <param name="managerID">Database ID of the Manager</param>
        /// <returns>List of Salespeople under the given Manager</returns>
        public IList<UserItem> GetMySalespeople(int managerID)
        {
            List<UserItem> output = new List<UserItem>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "SELECT * FROM [Users] WHERE id IN(SELECT salespersonID FROM Manager_Saleperson WHERE managerID = @ManagerID);";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ManagerID", managerID);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    UserItem newuser = GetUserItemFromReader(reader);
                    output.Add(newuser);
                }
            }

            return output;
        }

        /// <summary>
        /// Set's the user's Reset Password flag. Throws error if unsuccessful
        /// </summary>
        /// <param name="userId">User's Database ID</param>
        public void MarkResetPassword(int userId)
        {
            int numRows = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "UPDATE Users SET updatePassword = 1 WHERE id = @UserID;";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                numRows = cmd.ExecuteNonQuery();
            }

            if (numRows != 1)
            {
                throw new MarkResetPasswordFailedException();
            }
        }

        /// <summary>
        /// Reset's the given User's Password values
        /// </summary>
        /// <param name="emailAddress">Email Address of the User</param>
        /// <param name="salt">New Salt value for the User</param>
        /// <param name="hash">New Hash value for the User</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public bool ResetPassword(string emailAddress, string salt, string hash)
        {
            int numRows = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "UPDATE Users SET salt = @Salt, hash = @Hash, updatePassword = 0 WHERE emailAddress = @EmailAddress;";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@EmailAddress", emailAddress);

                numRows = cmd.ExecuteNonQuery();
            }

            return numRows == 1 ? true : false;
        }

        /// <summary>
        /// Pairs the current logged in Manager with the newly created Salesperson
        /// </summary>
        /// <param name="managerID">User ID of the Manager</param>
        /// <param name="SalespersonID">User ID of the Salesperson</param>
        public void PairManagerWithSalesperson(int managerID, int SalespersonID)
        {
            int numRows = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "INSERT INTO [Manager_Saleperson] (managerID, salespersonID) VALUES (@ManagerID, @SalespersonID);";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ManagerID", managerID);
                cmd.Parameters.AddWithValue("@SalespersonID", SalespersonID);

                numRows = cmd.ExecuteNonQuery();
            }

            if (numRows != 1)
            {
                throw new ManagerSalespersonLinkFailedException();
            }
        }

        private bool IsMySalesperson(int salespersonID, int managerID)
        {
            foreach(UserItem user in GetMySalespeople(managerID))
            {
                if(user.Id == salespersonID)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region House Methods

        /// <summary>
        /// Returns a list of Houses associated to the given Manager
        /// </summary>
        /// <param name="managerID">User ID of the Manager</param>
        /// <returns>List of Houses associated to that Manager</returns>
        public IList<HouseItem> GetAllHouses(int managerID)
        {
            List<HouseItem> houseList = new List<HouseItem>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "SELECT h.*, (u.firstName + ' ' + u.lastName) AS salespersonName " +
                    "FROM[Houses] AS h JOIN[Users] AS u ON h.salespersonID = u.id " +
                    "WHERE h.managerID = @ManagerID;";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ManagerID", managerID);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    HouseItem newHouse = GetHouseItemFromReader(reader);
                    houseList.Add(newHouse);
                }
            }

            return houseList;
        }

        /// <summary>
        /// Generates a HouseItem from the provided Sql Data Reader
        /// </summary>
        /// <param name="reader">The given Sql Data Reader</param>
        /// <returns>HouseItem containing the information for a particular hosue</returns>
        private HouseItem GetHouseItemFromReader(SqlDataReader reader)
        {
            HouseItem item = new HouseItem();

            item.Id = Convert.ToInt32(reader["id"]);
            item.Street = Convert.ToString(reader["street"]);
            item.City = Convert.ToString(reader["city"]);
            item.District = Convert.ToString(reader["district"]);
            item.ZipCode = Convert.ToString(reader["zipCode"]);
            item.Country = Convert.ToString(reader["country"]);
            item.ManagerID = Convert.ToInt32(reader["managerID"]);
            item.AssignedSalespersonID = Convert.ToInt32(reader["salespersonID"]);
            item.StatusID = Convert.ToInt32(reader["statusID"]);
            item.AssignedSalesperson = Convert.ToString(reader["salespersonName"]);

            return item;
        }

        /// <summary>
        /// Creates a new House in the database
        /// </summary>
        /// <param name="item">The House to be created</param>
        /// <returns>ID of the created House</returns>
        public int CreateHouse(HouseItem item)
        {
            if (IsMySalesperson(item.AssignedSalespersonID, item.ManagerID))
            {
                int ID = 0;

                const string sql = "INSERT INTO [Houses] (street, city, district, zipCode, country, managerID, salespersonID, statusID) " +
                                   "VALUES (@Street, @City, @District, @ZipCode, @Country, @ManagerID, @SalespersonID, 1);";
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql + " " + _getLastIdSql, conn);
                    cmd.Parameters.AddWithValue("@Street", item.Street);
                    cmd.Parameters.AddWithValue("@City", item.City);
                    cmd.Parameters.AddWithValue("@District", item.District.ToLower());
                    cmd.Parameters.AddWithValue("@ZipCode", item.ZipCode);
                    cmd.Parameters.AddWithValue("@Country", item.Country);
                    cmd.Parameters.AddWithValue("@ManagerID", item.ManagerID);
                    cmd.Parameters.AddWithValue("@SalespersonID", item.AssignedSalespersonID);

                    try
                    {
                        ID = (int)cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                return ID;
            }
            else
            {
                throw new NotMySalespersonException();
            }
        }

        #endregion

        #region SalesTransaction Methods

        /// <summary>
        /// Generates a SalesTransactionItem from the provided Sql Data Reader
        /// </summary>
        /// <param name="reader">The given Sql Data Reader</param>
        /// <returns>SalesTransactionItem containing the information for a particular sale</returns>
        private SalesTransactionItem GetSalesItemFromReader(SqlDataReader reader)
        {
            SalesTransactionItem item = new SalesTransactionItem();

            item.Id = Convert.ToInt32(reader["id"]);
            item.Date = Convert.ToDateTime(reader["date"]);
            item.HouseID = Convert.ToInt32(reader["houseID"]);
            item.ProductID = Convert.ToInt32(reader["productID"]);
            item.SalesmenID = Convert.ToInt32(reader["salespersonID"]);
            item.Amount = Convert.ToDouble(reader["amount"]);
            

            return item;
        }


        /// <summary>
        /// Generates the data of salesman transactions, relevant to the specific Manager.
        /// </summary>
        /// <param name="managerID"></param>
        /// <returns>A list of Sales Transactions from salesmen belonging to the passed in Manager ID</returns>
        public IList<SalesTransactionItem> GetSalesmanTransactionData(int managerID)
        {
            var output = new List<SalesTransactionItem>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = "SELECT u.firstName, u.lastName, COUNT(st.ID) AS numSales " +
                    "FROM Users AS u JOIN Sales_Transactions AS st ON u.id = st.salespersonIDWHERE u.id " +
                    "IN(SELECT ms.salespersonID FROM Manager_Saleperson AS ms WHERE ms.managerID = 1) " +
                    "GROUP BY u.firstName, u.lastName ORDER BY numSales DESC; ";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ManagerID", managerID);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    SalesTransactionItem sale = GetSalesItemFromReader(reader);
                    output.Add(sale);
                }
            }
            return output;
        }

        #endregion
    }
}
