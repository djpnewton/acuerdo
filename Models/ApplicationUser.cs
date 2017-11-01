﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using viafront3.Data;
using MySql.Data.MySqlClient;

namespace viafront3.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public Exchange Exchange{ get; set; }
        public bool EnsureExchangePresent(ApplicationDbContext context)
        {
            context.Entry(this).Reference(u => u.Exchange).Load();
            if (Exchange == null)
            {
                var exch = new Exchange{ ApplicationUserId=Id };
                context.Add(exch);
                return true;
            }
            return false;
        }

        public bool EnsureBackendTablesPresent(ILogger logger, MySqlSettings settings)
        {
            var conn = new MySqlConnection($"host={settings.Host};database={settings.Database};uid={settings.User};password={settings.Password};");
            try
            {
                var sqlCmds = new string[] {
                    $"create table if not exists balance_history_{this.Exchange.Id} like balance_history_example;",
                    $"create table if not exists deal_history_{this.Exchange.Id} like deal_history_example;",
                    $"create table if not exists order_history_{this.Exchange.Id} like order_history_example;",
                    $"create table if not exists order_detail_{this.Exchange.Id} like order_detail_example;",
                    $"create table if not exists user_deal_history_{this.Exchange.Id} like user_deal_history_example;"};
                conn.Open();
                var trans = conn.BeginTransaction();
                foreach (var sqlCmd in sqlCmds)
                {
                    var cmd = new MySqlCommand(sqlCmd, conn, trans);
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                conn.Close();                
            }
            return false;
        }
    }

    public class Exchange
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
    }
}